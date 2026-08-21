using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Queue;

namespace Crovus.Gateway;

public sealed class DiscordGatewayClient : IDiscordGateway
{
    private const string LogCategory = "Gateway.Client";
    private const int ReceiveBufferSize = 8192;

    private static readonly TimeSpan BaseReconnectDelay = TimeSpan.FromSeconds(1);

    private readonly GatewayOptions _options;
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private readonly TimeProvider _time;
    private readonly PriorityChannel<GatewayCommand> _outbound;
    private readonly WindowRateLimit _commandLimit;
    private readonly Channel<GatewayEvent> _events;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private CancellationTokenSource? _lifetime;
    private Task? _supervisor;
    private TaskCompletionSource? _ready;
    private Connection? _connection;
    private string? _sessionId;
    private string? _resumeUrl;
    private bool _canResume;
    private bool _disposed;
    private long _epoch;
    private long _lastSequence = -1;
    private long _latencyTicks = -1;
    private volatile GatewayState _state = GatewayState.Disconnected;

    public DiscordGatewayClient(GatewayOptions options, ILogger? logger = null, ITelemetry? telemetry = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;
        _time = timeProvider ?? TimeProvider.System;
        _outbound = new PriorityChannel<GatewayCommand>(options.CommandQueueCapacity);
        _commandLimit = new WindowRateLimit(options.CommandsPerWindow, options.CommandWindow, _time);

        _events = Channel.CreateBounded<GatewayEvent>(new BoundedChannelOptions(options.EventQueueCapacity)
        {
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public DiscordGatewayClient(GatewayOptions options, DiagnosticsHub diagnostics, TimeProvider? timeProvider = null)
        : this(options, diagnostics, diagnostics, timeProvider)
    {
    }

    public GatewayState State => _state;

    public string? SessionId => Volatile.Read(ref _sessionId);

    public int? LastSequence
    {
        get
        {
            var sequence = Interlocked.Read(ref _lastSequence);
            return sequence < 0 ? null : (int)sequence;
        }
    }

    public TimeSpan? Latency
    {
        get
        {
            var ticks = Interlocked.Read(ref _latencyTicks);
            return ticks < 0 ? null : TimeSpan.FromTicks(ticks);
        }
    }

    public int PendingCommands => _outbound.Count;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource ready;

        await _lifecycle.WaitAsync(cancellationToken);

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_supervisor is { IsCompleted: false })
                throw new InvalidOperationException("The gateway is already connected.");

            _lifetime = new CancellationTokenSource();
            _ready = ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var lifetime = _lifetime.Token;
            _supervisor = Task.Run(() => SuperviseAsync(lifetime), CancellationToken.None);

            _logger.LogInformation("Gateway connect requested");
        }
        finally
        {
            _lifecycle.Release();
        }

        await using var registration = cancellationToken.Register(
            static state => ((TaskCompletionSource)state!).TrySetCanceled(), ready);

        try
        {
            await ready.Task;
        }
        catch
        {
            await DisconnectAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        Task? supervisor;

        await _lifecycle.WaitAsync(cancellationToken);

        try
        {
            if (_lifetime is null)
                return;

            _logger.LogInformation("Gateway disconnect requested");

            await _lifetime.CancelAsync();
            _lifetime.Dispose();
            _lifetime = null;

            supervisor = _supervisor;
            _supervisor = null;
        }
        finally
        {
            _lifecycle.Release();
        }

        if (supervisor is not null)
            await Quiet(supervisor);

        _ready?.TrySetCanceled();
        SetState(GatewayState.Disconnected);
    }

    public ValueTask SendAsync(GatewayOpcode opcode, object? payload, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return EnqueueAsync(GatewayCommand.User(opcode, payload), cancellationToken);
    }

    public ValueTask UpdatePresenceAsync(PresenceUpdate presence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presence);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogInformation(presence.Activities.Count == 0
            ? $"Updating presence to {presence.Status}"
            : $"Updating presence to {presence.Status} ({string.Join(", ", presence.Activities.Select(activity => activity.Name))})");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewayPresenceUpdated(presence.Status.ToString(), presence.Activities.Count));

        return SendAsync(GatewayOpcode.PresenceUpdate, PresenceUpdatePayload.From(presence), cancellationToken);
    }

    public ValueTask RequestGuildMembersAsync(GuildMembersRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogInformation(request.IsTargeted
            ? $"Requesting {request.UserIds.Count} members of guild {request.GuildId}"
            : $"Requesting members of guild {request.GuildId} matching '{request.Query}' (limit {request.Limit})");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewayMembersRequested(request.GuildId.Value, request.UserIds.Count, request.Limit,
                request.WithPresences));

        return SendAsync(GatewayOpcode.RequestGuildMembers, RequestGuildMembersPayload.From(request),
            cancellationToken);
    }

    public IAsyncEnumerable<GatewayEvent> ReadEventsAsync(CancellationToken cancellationToken = default) =>
        _events.Reader.ReadAllAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await DisconnectAsync(CancellationToken.None);

        _outbound.Complete();
        _events.Writer.TryComplete();
        _lifecycle.Dispose();
    }

    private async ValueTask EnqueueAsync(GatewayCommand command, CancellationToken cancellationToken)
    {
        if (command.Priority is QueuePriority.High)
            _outbound.TryWrite(command, QueuePriority.High);
        else
            await _outbound.WriteAsync(command, QueuePriority.Normal, cancellationToken);

        if (command.Completion is not { } completion)
            return;

        await using var registration = cancellationToken.Register(
            static state => ((TaskCompletionSource)state!).TrySetCanceled(), completion);

        await completion.Task;
    }

    private async Task SuperviseAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunConnectionAsync(cancellationToken);
                attempt = 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (GatewayFatalException exception)
            {
                _logger.LogCritical($"Gateway closed permanently: {exception.Message}", exception);
                _ready?.TrySetException(exception);
                SetState(GatewayState.Disconnected);
                _events.Writer.TryComplete(exception);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning($"Gateway connection ended: {exception.Message}", exception);
            }

            if (cancellationToken.IsCancellationRequested)
                break;

            attempt++;
            var delay = NextReconnectDelay(attempt);

            SetState(GatewayState.Reconnecting);
            _logger.LogInformation($"Reconnecting in {delay.TotalMilliseconds:F0}ms (attempt {attempt})");

            if (_telemetry.HasSubscribers)
                _telemetry.Emit(new GatewayReconnectScheduled(attempt, delay));

            try
            {
                await Task.Delay(delay, _time, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        SetState(GatewayState.Disconnected);
    }

    private async Task RunConnectionAsync(CancellationToken cancellationToken)
    {
        var epoch = Interlocked.Increment(ref _epoch);
        var resuming = _canResume && _sessionId is not null && LastSequence is not null;
        var uri = _options.BuildUri(resuming ? _resumeUrl : null);

        using var socket = new ClientWebSocket();
        using var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var connection = new Connection(socket, epoch, connectionCts);
        _connection = connection;

        SetState(GatewayState.Connecting);
        _logger.LogInformation($"Connecting to {uri} ({(resuming ? "resume" : "fresh session")})");

        await socket.ConnectAsync(uri, cancellationToken);

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewayConnected(uri.ToString(), resuming));

        var receive = ReceiveLoopAsync(connection, connectionCts.Token);
        var send = SendLoopAsync(connection, connectionCts.Token);

        try
        {
            await await Task.WhenAny(receive, send);
        }
        finally
        {
            await connectionCts.CancelAsync();
            await Task.WhenAll(Quiet(receive), Quiet(send), Quiet(connection.Heartbeat));

            _connection = null;
            await CloseSocketAsync(socket);
        }
    }

    private async Task SendLoopAsync(Connection connection, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var command = await _outbound.ReadAsync(cancellationToken);

            if (command.Epoch != GatewayCommand.AnyEpoch && command.Epoch != connection.Epoch)
            {
                command.Completion?.TrySetCanceled(cancellationToken);
                _logger.LogDebug($"Dropped stale {command.Opcode} queued for connection {command.Epoch}");

                if (_telemetry.HasSubscribers)
                    _telemetry.Emit(new GatewayCommandDropped(command.Opcode.ToString(), "stale connection"));

                continue;
            }

            try
            {
                await ThrottleAsync(command, cancellationToken);

                var frame = JsonSerializer.SerializeToUtf8Bytes(
                    new GatewayOutboundPayload((int)command.Opcode, command.Payload), _json);

                await connection.Socket.SendAsync(frame, WebSocketMessageType.Text, true, cancellationToken);

                command.Completion?.TrySetResult();

                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace($"Sent {command.Opcode} ({frame.Length} bytes)");

                if (_telemetry.HasSubscribers)
                    _telemetry.Emit(new GatewayCommandSent(command.Opcode.ToString(), frame.Length,
                        command.QueueLatency));
            }
            catch (OperationCanceledException)
            {
                command.Completion?.TrySetCanceled(cancellationToken);
                throw;
            }
            catch (Exception exception)
            {
                command.Completion?.TrySetException(exception);
                _logger.LogError($"Failed to send {command.Opcode}", exception);
                throw;
            }
        }
    }

    private async ValueTask ThrottleAsync(GatewayCommand command, CancellationToken cancellationToken)
    {
        if (command.Priority is QueuePriority.High)
        {
            _commandLimit.Consume();
            return;
        }

        var waited = await _commandLimit.WaitAsync(cancellationToken);

        if (waited <= TimeSpan.Zero)
            return;

        _logger.LogDebug($"Throttled {command.Opcode} for {waited.TotalMilliseconds:F0}ms");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewayCommandThrottled(command.Opcode.ToString(), waited));
    }

    private async Task ReceiveLoopAsync(Connection connection, CancellationToken cancellationToken)
    {
        var buffer = new ArrayBufferWriter<byte>(ReceiveBufferSize);

        while (!cancellationToken.IsCancellationRequested)
        {
            buffer.ResetWrittenCount();

            ValueWebSocketReceiveResult result;

            do
            {
                var memory = buffer.GetMemory(ReceiveBufferSize);
                result = await connection.Socket.ReceiveAsync(memory, cancellationToken);

                if (result.MessageType is WebSocketMessageType.Close)
                    throw BuildCloseException((int?)connection.Socket.CloseStatus,
                        connection.Socket.CloseStatusDescription);

                buffer.Advance(result.Count);
            } while (!result.EndOfMessage);

            if (buffer.WrittenCount == 0)
                continue;

            await HandleFrameAsync(connection, buffer.WrittenMemory, cancellationToken);
        }
    }

    private async ValueTask HandleFrameAsync(Connection connection, ReadOnlyMemory<byte> frame,
        CancellationToken cancellationToken)
    {
        GatewayEvent gatewayEvent;

        using (var document = JsonDocument.Parse(frame))
        {
            var root = document.RootElement;

            var opcode = (GatewayOpcode)root.GetProperty("op").GetInt32();

            var sequence = root.TryGetProperty("s", out var rawSequence) &&
                           rawSequence.ValueKind is JsonValueKind.Number
                ? rawSequence.GetInt32()
                : (int?)null;

            var name = root.TryGetProperty("t", out var rawName) && rawName.ValueKind is JsonValueKind.String
                ? rawName.GetString()
                : null;

            var data = root.TryGetProperty("d", out var rawData) && rawData.ValueKind is not JsonValueKind.Null
                ? rawData.Clone()
                : (JsonElement?)null;

            gatewayEvent = new GatewayEvent(opcode, data, sequence, name);
        }

        await DispatchAsync(connection, gatewayEvent, cancellationToken);
    }

    private async ValueTask DispatchAsync(Connection connection, GatewayEvent gatewayEvent,
        CancellationToken cancellationToken)
    {
        switch (gatewayEvent.Opcode)
        {
            case GatewayOpcode.Hello:
                await HandleHelloAsync(connection, gatewayEvent, cancellationToken);
                break;

            case GatewayOpcode.Heartbeat:
                await EnqueueAsync(GatewayCommand.Control(GatewayOpcode.Heartbeat, LastSequence, connection.Epoch),
                    cancellationToken);
                _logger.LogDebug("Server requested a heartbeat");
                break;

            case GatewayOpcode.HeartbeatAck:
                AcknowledgeHeartbeat(connection);
                break;

            case GatewayOpcode.Dispatch:
                HandleDispatch(gatewayEvent);
                break;

            case GatewayOpcode.Reconnect:
                _canResume = true;
                _logger.LogInformation("Server asked us to reconnect");
                throw new GatewayReconnectSignal("The server requested a reconnect.");

            case GatewayOpcode.InvalidSession:
                HandleInvalidSession(gatewayEvent);
                throw new GatewayReconnectSignal("The session was invalidated.");
        }

        await PublishAsync(gatewayEvent, cancellationToken);
    }

    private async ValueTask HandleHelloAsync(Connection connection, GatewayEvent gatewayEvent,
        CancellationToken cancellationToken)
    {
        var interval = gatewayEvent.Data is { } data &&
                       data.TryGetProperty("heartbeat_interval", out var raw)
            ? TimeSpan.FromMilliseconds(raw.GetDouble())
            : TimeSpan.FromSeconds(41.25);

        _logger.LogDebug($"Hello received, heartbeat interval {interval.TotalMilliseconds:F0}ms");

        connection.Heartbeat = HeartbeatLoopAsync(connection, interval, connection.Token);

        if (_canResume && Volatile.Read(ref _sessionId) is { } session && LastSequence is { } sequence)
        {
            SetState(GatewayState.Resuming);
            await EnqueueAsync(
                GatewayCommand.Control(GatewayOpcode.Resume,
                    new ResumePayload(_options.Token, session, sequence), connection.Epoch),
                cancellationToken);
            return;
        }

        SetState(GatewayState.Identifying);

        var identify = new IdentifyPayload
        {
            Token = _options.Token,
            Intents = (int)_options.Intents,
            Properties = new ConnectionProperties(_options.OperatingSystem, _options.Library, _options.Library),
            LargeThreshold = _options.LargeThreshold,
            Shard = _options.BuildShard(),
            Presence = _options.Presence is { } presence ? PresenceUpdatePayload.From(presence) : null
        };

        await EnqueueAsync(GatewayCommand.Control(GatewayOpcode.Identify, identify, connection.Epoch),
            cancellationToken);
    }

    private void HandleDispatch(GatewayEvent gatewayEvent)
    {
        if (gatewayEvent.Sequence is { } sequence)
            Interlocked.Exchange(ref _lastSequence, sequence);

        switch (gatewayEvent.Name)
        {
            case "READY":
                ApplyReady(gatewayEvent);
                break;

            case "RESUMED":
                _canResume = true;
                SetState(GatewayState.Ready);
                _ready?.TrySetResult();
                _logger.LogInformation($"Session {Volatile.Read(ref _sessionId)} resumed");

                if (_telemetry.HasSubscribers)
                    _telemetry.Emit(new GatewaySessionEstablished(Volatile.Read(ref _sessionId) ?? string.Empty, true));

                break;
        }

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewayDispatchReceived(gatewayEvent.Name ?? string.Empty, gatewayEvent.Sequence));
    }

    private void ApplyReady(GatewayEvent gatewayEvent)
    {
        if (gatewayEvent.Data is { } data)
        {
            if (data.TryGetProperty("session_id", out var session))
                Volatile.Write(ref _sessionId, session.GetString());

            if (data.TryGetProperty("resume_gateway_url", out var resumeUrl))
                _resumeUrl = resumeUrl.GetString();
        }

        _canResume = true;
        SetState(GatewayState.Ready);
        _ready?.TrySetResult();

        _logger.LogInformation($"Session {Volatile.Read(ref _sessionId)} is ready");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewaySessionEstablished(Volatile.Read(ref _sessionId) ?? string.Empty, false));
    }

    private void HandleInvalidSession(GatewayEvent gatewayEvent)
    {
        var resumable = gatewayEvent.Data is { ValueKind: JsonValueKind.True };

        _canResume = resumable;

        if (!resumable)
            ClearSession();

        _logger.LogWarning($"Session invalidated (resumable: {resumable})");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewaySessionInvalidated(resumable));
    }

    private async Task HeartbeatLoopAsync(Connection connection, TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(interval * Random.Shared.NextDouble(), _time, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var pending = Interlocked.Read(ref connection.HeartbeatSentAt);

                if (pending != 0)
                {
                    var since = Stopwatch.GetElapsedTime(pending);

                    _logger.LogWarning(
                        $"No heartbeat acknowledgement for {since.TotalMilliseconds:F0}ms, treating the connection as a zombie");

                    if (_telemetry.HasSubscribers)
                        _telemetry.Emit(new GatewayHeartbeatMissed(since));

                    _canResume = true;
                    await connection.AbortAsync();
                    return;
                }

                Interlocked.Exchange(ref connection.HeartbeatSentAt, Stopwatch.GetTimestamp());

                var sequence = LastSequence;
                await EnqueueAsync(GatewayCommand.Control(GatewayOpcode.Heartbeat, sequence, connection.Epoch),
                    cancellationToken);

                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace($"Heartbeat sent at sequence {sequence?.ToString() ?? "none"}");

                if (_telemetry.HasSubscribers)
                    _telemetry.Emit(new GatewayHeartbeatSent(sequence));

                await Task.Delay(interval, _time, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void AcknowledgeHeartbeat(Connection connection)
    {
        var sentAt = Interlocked.Exchange(ref connection.HeartbeatSentAt, 0);

        if (sentAt == 0)
            return;

        var latency = Stopwatch.GetElapsedTime(sentAt);
        Interlocked.Exchange(ref _latencyTicks, latency.Ticks);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace($"Heartbeat acknowledged after {latency.TotalMilliseconds:F0}ms");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewayHeartbeatAcknowledged(latency));
    }

    private async ValueTask PublishAsync(GatewayEvent gatewayEvent, CancellationToken cancellationToken)
    {
        if (_events.Writer.TryWrite(gatewayEvent))
            return;

        _logger.LogWarning(
            $"Event queue is saturated at {_options.EventQueueCapacity} entries, the receive loop is now blocked");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewayEventQueueSaturated(_options.EventQueueCapacity));

        await _events.Writer.WriteAsync(gatewayEvent, cancellationToken);
    }

    private GatewayException BuildCloseException(int? closeCode, string? reason)
    {
        var fatal = closeCode is 4004 or 4010 or 4011 or 4012 or 4013 or 4014;
        var resumable = closeCode is null or 4000 or 4001 or 4002 or 4005 or 4008;

        _canResume = resumable;

        if (!resumable)
            ClearSession();

        _logger.LogWarning($"Gateway closed with code {closeCode?.ToString() ?? "none"}: {reason ?? "no reason"}");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewayDisconnected(closeCode, reason, !fatal));

        var message = $"The gateway closed with code {closeCode?.ToString() ?? "none"}: {reason ?? "no reason"}";

        return fatal
            ? new GatewayFatalException(message, closeCode)
            : new GatewayReconnectSignal(message, closeCode);
    }

    private void ClearSession()
    {
        Volatile.Write(ref _sessionId, null);
        _resumeUrl = null;
        Interlocked.Exchange(ref _lastSequence, -1);
    }

    private void SetState(GatewayState state)
    {
        var previous = _state;

        if (previous == state)
            return;

        _state = state;
        _logger.LogDebug($"State changed from {previous} to {state}");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new GatewayStateChanged(previous.ToString(), state.ToString()));
    }

    private TimeSpan NextReconnectDelay(int attempt)
    {
        var scaled = BaseReconnectDelay * Math.Pow(2, Math.Min(attempt - 1, 6));
        var capped = scaled > _options.MaxReconnectDelay ? _options.MaxReconnectDelay : scaled;

        return capped * (0.8 + Random.Shared.NextDouble() * 0.4);
    }

    private async Task CloseSocketAsync(ClientWebSocket socket)
    {
        if (socket.State is not (WebSocketState.Open or WebSocketState.CloseReceived))
        {
            socket.Abort();
            return;
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token);
        }
        catch (Exception exception)
        {
            _logger.LogDebug($"Graceful close failed: {exception.Message}");
            socket.Abort();
        }
    }

    private static Task Quiet(Task? task) =>
        task is null ? Task.CompletedTask : task.ContinueWith(static _ => { }, CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    private sealed class Connection(ClientWebSocket socket, long epoch, CancellationTokenSource cancellation)
    {
        public long HeartbeatSentAt;

        public ClientWebSocket Socket { get; } = socket;

        public long Epoch { get; } = epoch;

        public CancellationToken Token => cancellation.Token;

        public Task? Heartbeat { get; set; }

        public Task AbortAsync() => cancellation.CancelAsync();
    }
}

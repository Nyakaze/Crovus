using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Crovus.Logs;
using Crovus.Queue;

namespace Crovus.RichPresence;

public sealed class RichPresenceClient : IAsyncDisposable
{
    private const string LogCategory = "RichPresence";

    private readonly RichPresenceClientOptions _options;
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private readonly TimeProvider _time;
    private readonly WindowRateLimit _rateLimit;
    private readonly Channel<bool> _wake = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly ConcurrentDictionary<string, TaskCompletionSource<RichPresenceMessage>> _pending = new();
    private readonly Lock _gate = new();

    private RichPresenceActivity? _desired;
    private long _desiredVersion;
    private long _sentVersion = -1;
    private RichPresenceState _state = RichPresenceState.Idle;
    private RichPresenceFailure? _lastFailure;
    private RichPresenceUser? _user;
    private TaskCompletionSource _connected = NewSignal();
    private CancellationTokenSource? _lifetime;
    private Task? _supervisor;

    public RichPresenceClient(RichPresenceClientOptions options, ILogger? logger = null, ITelemetry? telemetry = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;
        _time = timeProvider ?? TimeProvider.System;
        _rateLimit = new WindowRateLimit(options.UpdatesPerWindow, options.UpdateWindow, _time);
    }

    public event Action<RichPresenceStateChange>? StateChanged;

    public event Action<RichPresenceUser>? Connected;

    public event Action<RichPresenceFailure>? ActivityRejected;

    public event Action<RichPresenceActivity?>? ActivityApplied;

    public RichPresenceClientOptions Options => _options;

    public RichPresenceState State
    {
        get
        {
            lock (_gate)
                return _state;
        }
    }

    public RichPresenceFailure? LastFailure
    {
        get
        {
            lock (_gate)
                return _lastFailure;
        }
    }

    public RichPresenceUser? User
    {
        get
        {
            lock (_gate)
                return _user;
        }
    }

    public RichPresenceActivity? Activity
    {
        get
        {
            lock (_gate)
                return _desired;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_lifetime is not null)
                return Task.CompletedTask;

            _lifetime = new CancellationTokenSource();
            _supervisor = Task.Run(() => SuperviseAsync(_lifetime.Token), CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public async Task WaitUntilConnectedAsync(CancellationToken cancellationToken = default)
    {
        Task signal;

        lock (_gate)
            signal = _connected.Task;

        await signal.WaitAsync(cancellationToken);
    }

    public void SetActivity(RichPresenceActivity? activity)
    {
        activity?.EnsureValid();

        lock (_gate)
        {
            if (Equals(_desired, activity))
                return;

            _desired = activity;
            _desiredVersion++;
        }

        _wake.Writer.TryWrite(true);
    }

    public void ClearActivity() => SetActivity(null);

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? lifetime;
        Task? supervisor;

        lock (_gate)
        {
            lifetime = _lifetime;
            supervisor = _supervisor;
            _lifetime = null;
            _supervisor = null;
        }

        if (lifetime is null || supervisor is null)
            return;

        await lifetime.CancelAsync();

        try
        {
            await supervisor.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            lifetime.Dispose();
        }

        Transition(RichPresenceState.Stopped, null);
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None);

    private async Task SuperviseAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            RichPresenceFailure? failure;
            Transition(RichPresenceState.Connecting, null);

            try
            {
                failure = await RunSessionAsync(cancellationToken);
                attempt = 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (RichPresenceException exception)
            {
                failure = exception.Failure;
            }
            catch (Exception exception)
            {
                failure = Classify(exception);
            }
            finally
            {
                FailPending();
            }

            Transition(RichPresenceState.Disconnected, failure);

            if (!_options.AutoReconnect)
                break;

            attempt++;
            var delay = ReconnectDelay(attempt, failure);
            var reason = failure?.ToString() ?? "connection closed";

            _logger.LogDebug($"Reconnecting in {delay.TotalSeconds:F0}s (attempt {attempt}): {reason}");

            if (_telemetry.HasSubscribers)
                _telemetry.Emit(new RichPresenceReconnectScheduled(attempt, delay, reason));

            try
            {
                await Task.Delay(delay, _time, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<RichPresenceFailure?> RunSessionAsync(CancellationToken cancellationToken)
    {
        await using var connection = await RichPresenceConnection.OpenAsync(_options, cancellationToken);
        var user = await HandshakeAsync(connection, cancellationToken);

        TaskCompletionSource connectedSignal;

        lock (_gate)
        {
            _user = user;
            _sentVersion = _desired is null ? _desiredVersion : -1;
            connectedSignal = _connected;
        }

        _logger.LogInformation($"Connected to Discord as {user.DisplayName} over {connection.PipeName}.");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new RichPresenceConnected(connection.PipeName, user.Id.Value));

        Transition(RichPresenceState.Connected, null);
        connectedSignal.TrySetResult();
        Raise(Connected, user);
        _wake.Writer.TryWrite(true);

        using var session = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var reader = ReadLoopAsync(connection, session.Token);
        var writer = WriteLoopAsync(connection, session.Token);

        var finished = await Task.WhenAny(reader, writer);
        await session.CancelAsync();

        try
        {
            await Task.WhenAll(reader, writer);
        }
        catch when (finished.IsFaulted || cancellationToken.IsCancellationRequested)
        {
        }

        cancellationToken.ThrowIfCancellationRequested();

        return finished.Exception?.InnerException switch
        {
            RichPresenceException exception => exception.Failure,
            { } exception => Classify(exception),
            null => new RichPresenceFailure(RichPresenceFailureKind.ConnectionLost, "The connection to Discord ended.")
        };
    }

    private async Task<RichPresenceUser> HandshakeAsync(RichPresenceConnection connection,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.HandshakeTimeout);

        try
        {
            await connection.SendAsync(RichPresenceOpcode.Handshake,
                RichPresenceJson.Handshake(_options.ApplicationId), timeout.Token);

            while (true)
            {
                var frame = await connection.ReceiveAsync(timeout.Token);

                switch (frame.Opcode)
                {
                    case RichPresenceOpcode.Ping:
                        await connection.SendAsync(RichPresenceOpcode.Pong, frame.Payload, timeout.Token);

                        break;

                    case RichPresenceOpcode.Close:
                        var (code, message) = RichPresenceJson.ParseError(frame.Payload.Span);

                        throw new RichPresenceException(new RichPresenceFailure(
                            RichPresenceFailureKind.HandshakeRejected, DescribeRejection(code, message), code));

                    case RichPresenceOpcode.Frame:
                        var dispatched = RichPresenceJson.ParseMessage(frame.Payload.Span);

                        if (dispatched.IsError)
                        {
                            var (errorCode, errorMessage) = RichPresenceJson.ParseError(dispatched.Data);

                            throw new RichPresenceException(new RichPresenceFailure(
                                RichPresenceFailureKind.HandshakeRejected, DescribeRejection(errorCode, errorMessage),
                                errorCode));
                        }

                        if (dispatched.IsReady)
                            return RichPresenceJson.ParseReadyUser(dispatched.Data) ??
                                   throw new RichPresenceException(new RichPresenceFailure(
                                       RichPresenceFailureKind.ProtocolError,
                                       "Discord's READY event did not contain the user."));

                        break;
                }
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested &&
                                                 !cancellationToken.IsCancellationRequested)
        {
            throw new RichPresenceException(new RichPresenceFailure(RichPresenceFailureKind.HandshakeTimeout,
                $"Discord did not answer the handshake within {_options.HandshakeTimeout.TotalSeconds:F0}s."));
        }
    }

    private async Task ReadLoopAsync(RichPresenceConnection connection, CancellationToken cancellationToken)
    {
        while (true)
        {
            var frame = await connection.ReceiveAsync(cancellationToken);

            switch (frame.Opcode)
            {
                case RichPresenceOpcode.Ping:
                    await connection.SendAsync(RichPresenceOpcode.Pong, frame.Payload, cancellationToken);

                    break;

                case RichPresenceOpcode.Close:
                    var (code, message) = RichPresenceJson.ParseError(frame.Payload.Span);

                    throw new RichPresenceException(new RichPresenceFailure(RichPresenceFailureKind.ConnectionLost,
                        $"Discord closed the connection: {message}", code));

                case RichPresenceOpcode.Frame:
                    var dispatched = RichPresenceJson.ParseMessage(frame.Payload.Span);

                    if (dispatched.Nonce is { } nonce && _pending.TryRemove(nonce, out var waiter))
                        waiter.TrySetResult(dispatched);

                    break;
            }
        }
    }

    private async Task WriteLoopAsync(RichPresenceConnection connection, CancellationToken cancellationToken)
    {
        while (true)
        {
            while (TryTakeChange(out _, out _))
            {
                var throttled = await _rateLimit.WaitAsync(cancellationToken);

                if (throttled > TimeSpan.Zero)
                {
                    _logger.LogDebug($"Activity update throttled for {throttled.TotalMilliseconds:F0}ms.");

                    if (_telemetry.HasSubscribers)
                        _telemetry.Emit(new RichPresenceActivityThrottled(throttled));
                }

                if (!TryTakeChange(out var activity, out var version))
                    break;

                await SendActivityAsync(connection, activity, cancellationToken);

                lock (_gate)
                    _sentVersion = version;
            }

            await WaitForChangeAsync(cancellationToken);
        }
    }

    private async Task WaitForChangeAsync(CancellationToken cancellationToken)
    {
        if (_options.RefreshInterval is not { } refresh)
        {
            await _wake.Reader.ReadAsync(cancellationToken);
            return;
        }

        using var quiet = new CancellationTokenSource(refresh, _time);
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, quiet.Token);

        try
        {
            await _wake.Reader.ReadAsync(wait.Token);
        }
        catch (OperationCanceledException) when (quiet.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            lock (_gate)
            {
                if (_desired is not null && _sentVersion == _desiredVersion)
                    _sentVersion = -1;
            }
        }
    }

    private async Task SendActivityAsync(RichPresenceConnection connection, RichPresenceActivity? activity,
        CancellationToken cancellationToken)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var waiter = new TaskCompletionSource<RichPresenceMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[nonce] = waiter;

        var started = Stopwatch.GetTimestamp();

        try
        {
            await connection.SendAsync(RichPresenceOpcode.Frame,
                RichPresenceJson.SetActivity(_options.ProcessId, activity, nonce), cancellationToken);

            RichPresenceMessage response;

            try
            {
                response = await waiter.Task.WaitAsync(_options.RequestTimeout, _time, cancellationToken);
            }
            catch (TimeoutException)
            {
                throw new RichPresenceException(new RichPresenceFailure(RichPresenceFailureKind.RequestTimeout,
                    $"Discord did not confirm the activity update within {_options.RequestTimeout.TotalSeconds:F0}s."));
            }

            if (response.IsError)
            {
                var (code, message) = RichPresenceJson.ParseError(response.Data);
                var failure = new RichPresenceFailure(RichPresenceFailureKind.ActivityRejected, message, code);

                lock (_gate)
                    _lastFailure = failure;

                _logger.LogWarning($"Discord rejected the activity: {failure}");

                if (_telemetry.HasSubscribers)
                    _telemetry.Emit(new RichPresenceActivityRejected(code, message));

                Raise(ActivityRejected, failure);

                return;
            }

            var latency = Stopwatch.GetElapsedTime(started);
            _logger.LogDebug(activity is null
                ? $"Activity cleared in {latency.TotalMilliseconds:F0}ms."
                : $"Activity set in {latency.TotalMilliseconds:F0}ms.");

            if (_telemetry.HasSubscribers)
                _telemetry.Emit(new RichPresenceActivitySent(activity is null, latency));

            Raise(ActivityApplied, activity);
        }
        finally
        {
            _pending.TryRemove(nonce, out _);
        }
    }

    private bool TryTakeChange(out RichPresenceActivity? activity, out long version)
    {
        lock (_gate)
        {
            activity = _desired;
            version = _desiredVersion;

            return _sentVersion != _desiredVersion;
        }
    }

    private void Transition(RichPresenceState next, RichPresenceFailure? failure)
    {
        RichPresenceState previous;
        bool repeated;

        lock (_gate)
        {
            previous = _state;
            repeated = failure is not null && _lastFailure is { } last && last.Kind == failure.Kind &&
                       last.Message == failure.Message;

            if (failure is not null)
                _lastFailure = failure;
            else if (next is RichPresenceState.Connected)
                _lastFailure = null;

            if (previous == next && failure is null)
                return;

            _state = next;

            if (next is RichPresenceState.Disconnected or RichPresenceState.Stopped)
            {
                _user = null;

                if (_connected.Task.IsCompleted)
                    _connected = NewSignal();
            }
        }

        if (failure is not null)
        {
            if (repeated)
                _logger.LogDebug($"Rich Presence still unavailable: {failure}");
            else
                _logger.LogWarning($"Rich Presence unavailable: {failure}");
        }

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new RichPresenceStateChanged(previous.ToString(), next.ToString(), failure?.ToString()));

        Raise(StateChanged, new RichPresenceStateChange(previous, next, failure));
    }

    private void FailPending()
    {
        foreach (var nonce in _pending.Keys)
        {
            if (_pending.TryRemove(nonce, out var waiter))
                waiter.TrySetCanceled();
        }
    }

    private TimeSpan ReconnectDelay(int attempt, RichPresenceFailure? failure)
    {
        if (failure?.Kind is RichPresenceFailureKind.DiscordNotRunning)
            return _options.NotRunningPollInterval;

        var exponent = Math.Min(attempt - 1, 10);
        var delay = TimeSpan.FromTicks(_options.ReconnectDelay.Ticks * (1L << exponent));

        return delay > _options.MaxReconnectDelay ? _options.MaxReconnectDelay : delay;
    }

    private void Raise<T>(Action<T>? handler, T argument)
    {
        if (handler is null)
            return;

        foreach (var subscriber in handler.GetInvocationList().Cast<Action<T>>())
        {
            try
            {
                subscriber(argument);
            }
            catch (Exception exception)
            {
                _logger.LogError($"A Rich Presence event handler threw: {exception.Message}", exception);
            }
        }
    }

    private static RichPresenceFailure Classify(Exception exception) => exception switch
    {
        EndOfStreamException or IOException or ObjectDisposedException => new RichPresenceFailure(
            RichPresenceFailureKind.ConnectionLost, "The connection to Discord was lost.", Exception: exception),
        InvalidDataException or System.Text.Json.JsonException => new RichPresenceFailure(
            RichPresenceFailureKind.ProtocolError, $"Discord sent data that could not be read: {exception.Message}",
            Exception: exception),
        _ => new RichPresenceFailure(RichPresenceFailureKind.ConnectionLost, exception.Message, Exception: exception)
    };

    private static string DescribeRejection(int? code, string message) => code switch
    {
        4000 => $"Discord rejected the application id: {message}",
        _ => $"Discord rejected the connection: {message}"
    };

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

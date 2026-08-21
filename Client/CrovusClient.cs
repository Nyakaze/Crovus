using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Crovus.Cache;
using Crovus.Events;
using Crovus.Gateway;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;
using Crovus.Services;

namespace Crovus.Client;

public sealed class CrovusClient : IAsyncDisposable
{
    private const string LogCategory = "Client";

    private readonly CrovusClientOptions _options;
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private readonly ConcurrentDictionary<Task, byte> _pending = new();
    private readonly ConcurrentDictionary<Snowflake, DiscordVoiceState> _voiceStates = new();
    private readonly DiscordEventResolver? _resolver;

    private long _dispatched;
    private long _startedAt;
    private int _running;
    private bool _disposed;

    public CrovusClient(CrovusClientOptions options, DiagnosticsHub? diagnostics = null,
        HttpClient? httpClient = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _options = options;

        Diagnostics = diagnostics ?? new DiagnosticsHub(options.MinimumLogLevel, timeProvider);
        _logger = Diagnostics.ForCategory(LogCategory);
        _telemetry = Diagnostics;

        Cache = options.EnableCache
            ? new DiscordCache(options.Cache, Diagnostics, timeProvider: timeProvider)
            : NullDiscordCache.Instance;

        IDiscordRest rest = new DiscordRestClient(options.BuildRest(), Diagnostics, httpClient,
            timeProvider: timeProvider);

        if (options.EnableRestLogging)
            rest = new LoggingDiscordRest(rest, Diagnostics);

        if (options.EnableCache)
            rest = new CachedDiscordRest(rest, Cache, Diagnostics);

        Rest = rest;

        IDiscordGateway gateway = new DiscordGatewayClient(options.BuildGateway(), Diagnostics, timeProvider);

        if (options.EnableCache)
            gateway = new CachedDiscordGateway(gateway, Cache, Diagnostics);

        Gateway = gateway;
        Events = new DiscordEventDispatcher(Diagnostics);
        Presences = new PresenceTracker(Diagnostics, options.PresenceCapacity);
        Services = new DiscordServices(Rest, Diagnostics);
        _resolver = options.ResolveEntities ? new DiscordEventResolver(Cache, Diagnostics) : null;
    }

    public CrovusClient(string token, GatewayIntents intents)
        : this(CrovusClientOptions.For(token, intents))
    {
    }

    public CrovusClient(CrovusClientOptions options, IDiscordRest rest, IDiscordGateway gateway,
        IDiscordCache? cache = null, DiagnosticsHub? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(rest);
        ArgumentNullException.ThrowIfNull(gateway);

        options.Validate();

        _options = options;

        Diagnostics = diagnostics ?? new DiagnosticsHub(options.MinimumLogLevel);
        _logger = Diagnostics.ForCategory(LogCategory);
        _telemetry = Diagnostics;

        Cache = cache ?? NullDiscordCache.Instance;
        Rest = rest;
        Gateway = gateway;
        Events = new DiscordEventDispatcher(Diagnostics);
        Presences = new PresenceTracker(Diagnostics, options.PresenceCapacity);
        Services = new DiscordServices(Rest, Diagnostics);
        _resolver = options.ResolveEntities ? new DiscordEventResolver(Cache, Diagnostics) : null;
    }

    public DiagnosticsHub Diagnostics { get; }

    public IDiscordRest Rest { get; }

    public IDiscordGateway Gateway { get; }

    public IDiscordCache Cache { get; }

    public DiscordEventDispatcher Events { get; }

    public PresenceTracker Presences { get; }

    public DiscordServices Services { get; }

    public MessageService Messages => Services.Messages;

    public EmbedService Embeds => Services.Embeds;

    public ChannelService Channels => Services.Channels;

    public ThreadService Threads => Services.Threads;

    public WebhookService Webhooks => Services.Webhooks;

    public ReactionService Reactions => Services.Reactions;

    public EmojiService Emojis => Services.Emojis;

    public CommandService Commands => Services.Commands;

    public InteractionService Interactions => Services.Interactions;

    public GuildService Guilds => Services.Guilds;

    public MemberService Members => Services.Members;

    public RoleService Roles => Services.Roles;

    public DiscordUser? CurrentUser { get; private set; }

    public Snowflake? ApplicationId { get; private set; }

    public GatewayState State => Gateway.State;

    public TimeSpan? Latency => Gateway.Latency;

    public string? SessionId => Gateway.SessionId;

    public bool IsConnected => State is GatewayState.Ready;

    public long DispatchedEvents => Interlocked.Read(ref _dispatched);

    public IDisposable On<TEvent>(Func<TEvent, Task> handler) where TEvent : DiscordEvent =>
        Events.Subscribe(handler);

    public IDisposable On<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : DiscordEvent =>
        Events.Subscribe(handler);

    public IDisposable OnAny(Func<DiscordEvent, CancellationToken, Task> handler) => Events.SubscribeAll(handler);

    public IDisposable OnPresenceUpdate(Func<PresenceUpdatedEvent, Task> handler) => Presences.OnUpdate(handler);

    public IDisposable OnPresenceUpdate(Snowflake userId, Func<PresenceUpdatedEvent, Task> handler) =>
        Presences.OnUser(userId, handler);

    public DiscordPresence? PresenceOf(Snowflake userId) => Presences.Get(userId);

    public UserStatus StatusOf(Snowflake userId) => Presences.StatusOf(userId);

    public DiscordActivity? ActivityOf(Snowflake userId) => Presences.ActivityOf(userId);

    public DiscordVoiceState? VoiceStateOf(Snowflake userId) =>
        _voiceStates.TryGetValue(userId, out var state) ? state : null;

    public Snowflake? VoiceChannelOf(Snowflake userId) => VoiceStateOf(userId)?.ChannelId;

    public IReadOnlyList<DiscordVoiceState> VoiceStatesIn(Snowflake channelId) =>
        _voiceStates.Values.Where(state => state.ChannelId == channelId).ToArray();

    public ValueTask RequestMembersAsync(GuildMembersRequest request, CancellationToken cancellationToken = default) =>
        Gateway.RequestGuildMembersAsync(request, cancellationToken);

    public ValueTask RequestMembersAsync(Snowflake guildId, bool withPresences = false,
        CancellationToken cancellationToken = default) =>
        Gateway.RequestGuildMembersAsync(GuildMembersRequest.All(guildId, withPresences), cancellationToken);

    public ValueTask SetPresenceAsync(PresenceUpdate presence, CancellationToken cancellationToken = default) =>
        Gateway.UpdatePresenceAsync(presence, cancellationToken);

    public ValueTask SetStatusAsync(UserStatus status, CancellationToken cancellationToken = default) =>
        Gateway.UpdatePresenceAsync(new PresenceUpdate { Status = status }, cancellationToken);

    public ValueTask SetActivityAsync(PresenceActivity activity, UserStatus status = UserStatus.Online,
        CancellationToken cancellationToken = default) =>
        Gateway.UpdatePresenceAsync(PresenceUpdate.With(activity, status), cancellationToken);

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await StartAsync(cancellationToken);

        try
        {
            await PumpAsync(cancellationToken);
        }
        finally
        {
            await StopAsync(CancellationToken.None);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (Interlocked.Exchange(ref _running, 1) == 1)
            throw new InvalidOperationException("The client is already running.");

        _startedAt = Stopwatch.GetTimestamp();

        _logger.LogInformation($"Starting with intents {_options.Intents}");

        await Gateway.ConnectAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _running, 0) == 0)
            return;

        await Gateway.DisconnectAsync(cancellationToken);
        await DrainAsync();

        var uptime = Stopwatch.GetElapsedTime(_startedAt);

        _logger.LogInformation(
            $"Stopped after {uptime.TotalSeconds:F0}s and {DispatchedEvents} events");

        _telemetry.Emit(new ClientDisconnected(uptime, DispatchedEvents));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await StopAsync(CancellationToken.None);
        await Services.DisposeAsync();
        await Gateway.DisposeAsync();
    }

    private async Task PumpAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var gatewayEvent in Gateway.ReadEventsAsync(cancellationToken))
            {
                if (gatewayEvent is not { IsDispatch: true, Name: { } name, Data: not null })
                    continue;

                if (!TryDecode(gatewayEvent, name, out var decoded))
                    continue;

                decoded = Capture(decoded);

                if (_resolver is { } resolver)
                    decoded = await resolver.ResolveAsync(decoded, cancellationToken);

                Interlocked.Increment(ref _dispatched);

                if (_options.SequentialDispatch)
                    await DispatchAsync(decoded, cancellationToken);
                else
                    Detach(decoded, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private bool TryDecode(GatewayEvent gatewayEvent, string name, out DiscordEvent decoded)
    {
        try
        {
            decoded = DiscordEventFactory.Create(gatewayEvent);

            return true;
        }
        catch (Exception exception)
            when (exception is JsonException or FormatException or KeyNotFoundException or InvalidOperationException)
        {
            _logger.LogWarning($"Could not decode the {name} dispatch", exception);
            _telemetry.Emit(new EventDecodeFailed(name, exception.GetType().Name));

            decoded = null!;

            return false;
        }
    }

    private DiscordEvent Capture(DiscordEvent decoded)
    {
        switch (decoded)
        {
            case PresenceUpdatedEvent presence:
                return Presences.Record(presence);

            case VoiceStateUpdatedEvent voice:
                return TrackVoiceState(voice);

            case GuildAvailableEvent guild:
                if (guild.Presences.Count > 0)
                    Presences.Seed(guild.Presences);

                foreach (var state in guild.VoiceStates)
                    if (state.IsConnected)
                        _voiceStates[state.UserId] = state;

                return decoded;

            case InteractionCreatedEvent created:
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug(
                        $"Received {created.InteractionType} interaction {created.Interaction.Id} ({created.CommandPath})");

                _telemetry.Emit(new InteractionReceived(created.Interaction.Id, created.InteractionType.ToString(),
                    created.CommandPath, created.GuildId?.Value));

                return decoded;

            case ReadyEvent ready:
                CurrentUser = ready.User;
                ApplicationId = ready.ApplicationId;

                _logger.LogInformation(
                    $"Ready as {ready.User.Username} ({ready.User.Id}) on session {ready.SessionId}");

                _telemetry.Emit(new ClientConnected(ready.User.Id.Value, ready.ApplicationId?.Value,
                    ready.SessionId));

                return decoded;

            default:
                return decoded;
        }
    }

    private VoiceStateUpdatedEvent TrackVoiceState(VoiceStateUpdatedEvent updated)
    {
        var state = updated.VoiceState;

        _voiceStates.TryGetValue(state.UserId, out var previous);

        if (state.IsConnected)
            _voiceStates[state.UserId] = state;
        else
            _voiceStates.TryRemove(state.UserId, out _);

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new VoiceStateChanged(state.UserId.Value, state.GuildId?.Value, state.ChannelId?.Value,
                previous?.ChannelId?.Value));

        return updated with { Previous = previous };
    }

    private async Task DispatchAsync(DiscordEvent decoded, CancellationToken cancellationToken)
    {
        await Events.DispatchAsync(decoded, cancellationToken);

        if (decoded is PresenceUpdatedEvent presence)
            await Presences.PublishAsync(presence, cancellationToken);
    }

    private void Detach(DiscordEvent decoded, CancellationToken cancellationToken)
    {
        var task = Task.Run(() => DispatchAsync(decoded, cancellationToken), CancellationToken.None);

        _pending[task] = 0;

        _ = task.ContinueWith(static (completed, state) =>
                ((ConcurrentDictionary<Task, byte>)state!).TryRemove(completed, out _),
            _pending, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private async Task DrainAsync()
    {
        while (!_pending.IsEmpty)
        {
            var outstanding = _pending.Keys.ToArray();

            if (outstanding.Length == 0)
                break;

            await Task.WhenAll(outstanding);
        }
    }
}

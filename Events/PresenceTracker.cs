using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Crovus.Logs;
using Crovus.Models;

namespace Crovus.Events;

public sealed class PresenceTracker
{
    private const string LogCategory = "Client.Presences";

    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private readonly int _capacity;
    private readonly ConcurrentDictionary<Snowflake, DiscordPresence> _presences = new();
    private readonly ConcurrentDictionary<Snowflake, Subscription[]> _byUser = new();
    private readonly ConcurrentDictionary<Snowflake, Subscription[]> _byGuild = new();
    private readonly Lock _gate = new();

    private Subscription[] _global = [];

    public PresenceTracker(ILogger? logger = null, ITelemetry? telemetry = null, int capacity = 25_000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;
        _capacity = capacity;
    }

    public PresenceTracker(DiagnosticsHub diagnostics, int capacity = 25_000)
        : this(diagnostics, diagnostics, capacity)
    {
    }

    public int Count => _presences.Count;

    public int SubscriberCount =>
        Volatile.Read(ref _global).Length +
        _byUser.Values.Sum(subscriptions => subscriptions.Length) +
        _byGuild.Values.Sum(subscriptions => subscriptions.Length);

    public IReadOnlyCollection<DiscordPresence> Tracked => _presences.Values.ToArray();

    public IReadOnlyCollection<Snowflake> WatchedUsers => _byUser.Keys.ToArray();

    public DiscordPresence? Get(Snowflake userId) =>
        _presences.TryGetValue(userId, out var presence) ? presence : null;

    public UserStatus StatusOf(Snowflake userId) => Get(userId)?.Status ?? UserStatus.Offline;

    public bool IsOnline(Snowflake userId) => Get(userId) is { IsOnline: true };

    public DiscordActivity? ActivityOf(Snowflake userId) => Get(userId)?.Primary;

    public IReadOnlyList<DiscordActivity> ActivitiesOf(Snowflake userId) => Get(userId)?.Activities ?? [];

    public IReadOnlyCollection<DiscordPresence> InGuild(Snowflake guildId) =>
        _presences.Values.Where(presence => presence.GuildId == guildId).ToArray();

    public IReadOnlyCollection<DiscordPresence> WithStatus(UserStatus status) =>
        _presences.Values.Where(presence => presence.Status == status).ToArray();

    public IReadOnlyCollection<DiscordPresence> WithActivity(ActivityTypes types) =>
        _presences.Values.Where(presence => presence.Has(types)).ToArray();

    public IReadOnlyCollection<DiscordPresence> WithActivity(ActivityType type, string name) =>
        _presences.Values.Where(presence => presence.Has(type, name)).ToArray();

    public IReadOnlyList<DiscordActivity> ActivitiesOf(Snowflake userId, ActivityTypes types) =>
        Get(userId)?.ActivitiesOf(types) ?? [];

    public DiscordActivity? ActivityOf(Snowflake userId, ActivityTypes types) => Get(userId)?.Find(types);

    public IDisposable OnUpdate(Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnUpdate(Wrap(handler), name ?? Describe(handler));

    public IDisposable OnUpdate(Func<PresenceUpdatedEvent, CancellationToken, Task> handler, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.Global, default, handler, null, name ?? Describe(handler));
    }

    public IDisposable OnUser(Snowflake userId, Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnUser(userId, Wrap(handler), name ?? Describe(handler));

    public IDisposable OnUser(Snowflake userId, Func<PresenceUpdatedEvent, CancellationToken, Task> handler,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.User, userId, handler, null, name ?? Describe(handler));
    }

    public IDisposable OnUsers(IEnumerable<Snowflake> userIds, Func<PresenceUpdatedEvent, Task> handler,
        string? name = null) => OnUsers(userIds, Wrap(handler), name ?? Describe(handler));

    public IDisposable OnUsers(IEnumerable<Snowflake> userIds,
        Func<PresenceUpdatedEvent, CancellationToken, Task> handler, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentNullException.ThrowIfNull(handler);

        var label = name ?? Describe(handler);

        return new CompositeSubscription(userIds
            .Distinct()
            .Select(userId => Register(Scope.User, userId, handler, null, label))
            .ToArray());
    }

    public IDisposable OnGuild(Snowflake guildId, Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnGuild(guildId, Wrap(handler), name ?? Describe(handler));

    public IDisposable OnGuild(Snowflake guildId, Func<PresenceUpdatedEvent, CancellationToken, Task> handler,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.Guild, guildId, handler, null, name ?? Describe(handler));
    }

    public IDisposable OnStatusChanged(Func<PresenceUpdatedEvent, Task> handler, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.Global, default, Wrap(handler), static updated => updated.StatusChanged,
            name ?? Describe(handler));
    }

    public IDisposable OnUserStatusChanged(Snowflake userId, Func<PresenceUpdatedEvent, Task> handler,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.User, userId, Wrap(handler), static updated => updated.StatusChanged,
            name ?? Describe(handler));
    }

    public IDisposable OnActivityChanged(Func<PresenceUpdatedEvent, Task> handler, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.Global, default, Wrap(handler), static updated => updated.ActivitiesChanged,
            name ?? Describe(handler));
    }

    public IDisposable OnUserActivityChanged(Snowflake userId, Func<PresenceUpdatedEvent, Task> handler,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.User, userId, Wrap(handler), static updated => updated.ActivitiesChanged,
            name ?? Describe(handler));
    }

    public IDisposable OnActivity(ActivityTypes types, Func<PresenceUpdatedEvent, Task> handler,
        string? name = null) => OnActivity(types, Wrap(handler), name ?? Describe(handler));

    public IDisposable OnActivity(ActivityTypes types, Func<PresenceUpdatedEvent, CancellationToken, Task> handler,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.Global, default, handler, updated => updated.Changed(types),
            name ?? Describe(handler));
    }

    public IDisposable OnUserActivity(Snowflake userId, ActivityTypes types,
        Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnUserActivity(userId, types, Wrap(handler), name ?? Describe(handler));

    public IDisposable OnUserActivity(Snowflake userId, ActivityTypes types,
        Func<PresenceUpdatedEvent, CancellationToken, Task> handler, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.User, userId, handler, updated => updated.Changed(types), name ?? Describe(handler));
    }

    public IDisposable OnGuildActivity(Snowflake guildId, ActivityTypes types,
        Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnGuildActivity(guildId, types, Wrap(handler), name ?? Describe(handler));

    public IDisposable OnGuildActivity(Snowflake guildId, ActivityTypes types,
        Func<PresenceUpdatedEvent, CancellationToken, Task> handler, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.Guild, guildId, handler, updated => updated.Changed(types), name ?? Describe(handler));
    }

    public IDisposable OnPlaying(Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnActivity(ActivityTypes.Playing, handler, name ?? Describe(handler));

    public IDisposable OnStreaming(Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnActivity(ActivityTypes.Streaming, handler, name ?? Describe(handler));

    public IDisposable OnListening(Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnActivity(ActivityTypes.Listening, handler, name ?? Describe(handler));

    public IDisposable OnListeningTo(Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnListening(handler, name);

    public IDisposable OnWatching(Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnActivity(ActivityTypes.Watching, handler, name ?? Describe(handler));

    public IDisposable OnCompeting(Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnActivity(ActivityTypes.Competing, handler, name ?? Describe(handler));

    public IDisposable OnCustomStatus(Func<PresenceUpdatedEvent, Task> handler, string? name = null) =>
        OnActivity(ActivityTypes.Custom, handler, name ?? Describe(handler));

    public IDisposable OnOnline(Func<PresenceUpdatedEvent, Task> handler, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.Global, default, Wrap(handler), static updated => updated.CameOnline,
            name ?? Describe(handler));
    }

    public IDisposable OnOffline(Func<PresenceUpdatedEvent, Task> handler, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(Scope.Global, default, Wrap(handler), static updated => updated.WentOffline,
            name ?? Describe(handler));
    }

    public async Task<PresenceUpdatedEvent> NextAsync(Snowflake userId, CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<PresenceUpdatedEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = OnUser(userId, updated =>
        {
            completion.TrySetResult(updated);
            return Task.CompletedTask;
        }, $"NextAsync({userId})");

        await using var registration = cancellationToken.Register(
            static state => ((TaskCompletionSource<PresenceUpdatedEvent>)state!).TrySetCanceled(), completion);

        return await completion.Task;
    }

    public IAsyncEnumerable<PresenceUpdatedEvent> WatchAsync(ActivityTypes types, Snowflake? userId = null,
        int capacity = 64, CancellationToken cancellationToken = default) =>
        WatchAsync(userId, capacity, types, cancellationToken);

    public async IAsyncEnumerable<PresenceUpdatedEvent> WatchAsync(Snowflake? userId = null, int capacity = 64,
        ActivityTypes? types = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<PresenceUpdatedEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

        Task Publish(PresenceUpdatedEvent updated)
        {
            channel.Writer.TryWrite(updated);
            return Task.CompletedTask;
        }

        using var subscription = (userId, types) switch
        {
            ({ } id, { } filter) => OnUserActivity(id, filter, Publish, "WatchAsync"),
            ({ } id, null) => OnUser(id, Publish, "WatchAsync"),
            (null, { } filter) => OnActivity(filter, Publish, "WatchAsync"),
            _ => OnUpdate(Publish, "WatchAsync")
        };

        await foreach (var updated in channel.Reader.ReadAllAsync(cancellationToken))
            yield return updated;
    }

    public bool Forget(Snowflake userId) => _presences.TryRemove(userId, out _);

    public void Clear() => _presences.Clear();

    public void Seed(IEnumerable<DiscordPresence> presences)
    {
        ArgumentNullException.ThrowIfNull(presences);

        var seeded = 0;

        foreach (var presence in presences)
        {
            if (_presences.Count >= _capacity && !_presences.ContainsKey(presence.UserId))
                continue;

            _presences[presence.UserId] = presence;
            seeded++;
        }

        if (seeded > 0 && _logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug($"Seeded {seeded} presences, now tracking {_presences.Count}");
    }

    public async Task<PresenceUpdatedEvent> ApplyAsync(PresenceUpdatedEvent updated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updated);

        var recorded = Record(updated);

        await PublishAsync(recorded, cancellationToken);

        return recorded;
    }

    public Task<PresenceUpdatedEvent> ApplyAsync(DiscordPresence presence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presence);

        return ApplyAsync(new PresenceUpdatedEvent(presence, null) { Name = "PRESENCE_UPDATE" }, cancellationToken);
    }

    internal PresenceUpdatedEvent Record(PresenceUpdatedEvent updated)
    {
        var presence = updated.Presence;

        DiscordPresence? previous = null;

        if (_presences.Count >= _capacity && !_presences.ContainsKey(presence.UserId))
        {
            _presences.TryGetValue(presence.UserId, out previous);
        }
        else
        {
            _presences.AddOrUpdate(presence.UserId, presence, (_, existing) =>
            {
                previous = existing;
                return presence;
            });
        }

        return updated with { Previous = previous };
    }

    internal async Task PublishAsync(PresenceUpdatedEvent updated, CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        var invoked = 0;

        invoked += await InvokeAllAsync(Volatile.Read(ref _global), updated, cancellationToken);

        if (_byUser.TryGetValue(updated.UserId, out var userSubscriptions))
            invoked += await InvokeAllAsync(userSubscriptions, updated, cancellationToken);

        if (updated.GuildId is { } guildId && _byGuild.TryGetValue(guildId, out var guildSubscriptions))
            invoked += await InvokeAllAsync(guildSubscriptions, updated, cancellationToken);

        if (invoked == 0)
            return;

        var duration = Stopwatch.GetElapsedTime(start);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace(
                $"Published presence for {updated.UserId} to {invoked} handlers in {duration.TotalMilliseconds:F0}ms");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new PresencePublished(updated.UserId.Value, updated.Status.ToString(), invoked, duration));
    }

    private async Task<int> InvokeAllAsync(Subscription[] subscriptions, PresenceUpdatedEvent updated,
        CancellationToken cancellationToken)
    {
        var invoked = 0;

        foreach (var subscription in subscriptions)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (!subscription.Accepts(updated))
                continue;

            invoked++;

            try
            {
                await subscription.Invoke(updated, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError($"Presence handler {subscription.Name} failed for {updated.UserId}", exception);

                if (_telemetry.HasSubscribers)
                    _telemetry.Emit(new PresenceHandlerFailed(updated.UserId.Value, subscription.Name,
                        exception.GetType().Name));
            }
        }

        return invoked;
    }

    private Subscription Register(Scope scope, Snowflake key,
        Func<PresenceUpdatedEvent, CancellationToken, Task> invoke, Func<PresenceUpdatedEvent, bool>? filter,
        string name)
    {
        var subscription = new Subscription(this, scope, key, invoke, filter, name);

        lock (_gate)
        {
            switch (scope)
            {
                case Scope.User:
                    _byUser[key] = Append(_byUser.GetValueOrDefault(key), subscription);
                    break;

                case Scope.Guild:
                    _byGuild[key] = Append(_byGuild.GetValueOrDefault(key), subscription);
                    break;

                default:
                    _global = Append(_global, subscription);
                    break;
            }
        }

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug(scope is Scope.Global
                ? $"Subscribed {name} to all presence updates"
                : $"Subscribed {name} to presence updates for {scope.ToString().ToLowerInvariant()} {key}");

        return subscription;
    }

    private void Remove(Subscription subscription)
    {
        lock (_gate)
        {
            switch (subscription.Scope)
            {
                case Scope.User:
                    RemoveFrom(_byUser, subscription.Key, subscription);
                    break;

                case Scope.Guild:
                    RemoveFrom(_byGuild, subscription.Key, subscription);
                    break;

                default:
                    _global = [.. _global.Where(existing => existing != subscription)];
                    break;
            }
        }

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug($"Unsubscribed {subscription.Name} from presence updates");
    }

    private static void RemoveFrom(ConcurrentDictionary<Snowflake, Subscription[]> source, Snowflake key,
        Subscription subscription)
    {
        if (!source.TryGetValue(key, out var existing))
            return;

        var remaining = existing.Where(candidate => candidate != subscription).ToArray();

        if (remaining.Length == 0)
            source.TryRemove(key, out _);
        else
            source[key] = remaining;
    }

    private static Subscription[] Append(Subscription[]? existing, Subscription subscription) =>
        existing is null ? [subscription] : [.. existing, subscription];

    private static Func<PresenceUpdatedEvent, CancellationToken, Task> Wrap(Func<PresenceUpdatedEvent, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return (updated, _) => handler(updated);
    }

    private static string Describe(Delegate handler) =>
        handler.Method.DeclaringType is { } owner
            ? $"{owner.Name}.{handler.Method.Name}"
            : handler.Method.Name;

    private enum Scope
    {
        Global,
        User,
        Guild
    }

    private sealed class Subscription(PresenceTracker owner, Scope scope, Snowflake key,
        Func<PresenceUpdatedEvent, CancellationToken, Task> invoke, Func<PresenceUpdatedEvent, bool>? filter,
        string name) : IDisposable
    {
        private volatile bool _disposed;

        public Scope Scope { get; } = scope;

        public Snowflake Key { get; } = key;

        public string Name { get; } = name;

        public bool Accepts(PresenceUpdatedEvent updated) => !_disposed && (filter is null || filter(updated));

        public Task Invoke(PresenceUpdatedEvent updated, CancellationToken cancellationToken) =>
            _disposed ? Task.CompletedTask : invoke(updated, cancellationToken);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            owner.Remove(this);
        }
    }

    private sealed class CompositeSubscription(IReadOnlyList<IDisposable> subscriptions) : IDisposable
    {
        public void Dispose()
        {
            foreach (var subscription in subscriptions)
                subscription.Dispose();
        }
    }
}

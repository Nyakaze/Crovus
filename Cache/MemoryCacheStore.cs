using Crovus.Logs;

namespace Crovus.Cache;

public sealed class MemoryCacheStore<TKey, TValue> : ICacheStore<TKey, TValue> where TKey : notnull
{
    private const string LogCategory = "Cache.Memory";

    private readonly string _name;
    private readonly CachePolicy _policy;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private readonly Lock _gate = new();
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _index;
    private readonly LinkedList<Entry> _order = new();

    public MemoryCacheStore(string name, CachePolicy policy, ILogger? logger = null, ITelemetry? telemetry = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _name = name;
        _policy = policy;
        _time = timeProvider ?? TimeProvider.System;
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;
        _index = new Dictionary<TKey, LinkedListNode<Entry>>(Math.Min(policy.Capacity, 64));
    }

    public int Count
    {
        get
        {
            lock (_gate)
                return _index.Count;
        }
    }

    public ValueTask<TValue?> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        if (!_policy.Enabled)
            return ValueTask.FromResult<TValue?>(default);

        lock (_gate)
        {
            if (!_index.TryGetValue(key, out var node))
                return ValueTask.FromResult<TValue?>(default);

            if (node.Value.ExpiresAt is { } expiry && _time.GetUtcNow() >= expiry)
            {
                Evict(node, "expired");
                return ValueTask.FromResult<TValue?>(default);
            }

            _order.Remove(node);
            _order.AddFirst(node);

            return ValueTask.FromResult<TValue?>(node.Value.Value);
        }
    }

    public ValueTask SetAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        if (!_policy.Enabled)
            return ValueTask.CompletedTask;

        lock (_gate)
        {
            var expiresAt = _policy.Lifetime is { } lifetime ? _time.GetUtcNow() + lifetime : (DateTimeOffset?)null;

            if (_index.TryGetValue(key, out var existing))
            {
                existing.Value.Value = value;
                existing.Value.ExpiresAt = expiresAt;
                _order.Remove(existing);
                _order.AddFirst(existing);
                return ValueTask.CompletedTask;
            }

            var node = _order.AddFirst(new Entry(key, value, expiresAt));
            _index[key] = node;

            while (_index.Count > _policy.Capacity && _order.Last is { } last)
                Evict(last, "capacity");

            return ValueTask.CompletedTask;
        }
    }

    public ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_index.TryGetValue(key, out var node))
                return ValueTask.FromResult(false);

            _index.Remove(key);
            _order.Remove(node);

            return ValueTask.FromResult(true);
        }
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var removed = _index.Count;
            _index.Clear();
            _order.Clear();

            if (removed > 0)
                _logger.LogDebug($"Cleared {removed} entries from {_name}");
        }

        return ValueTask.CompletedTask;
    }

    private void Evict(LinkedListNode<Entry> node, string reason)
    {
        _index.Remove(node.Value.Key);
        _order.Remove(node);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace($"Evicted an entry from {_name} ({reason})");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new CacheEntryEvicted(_name, reason));
    }

    private sealed class Entry(TKey key, TValue value, DateTimeOffset? expiresAt)
    {
        public TKey Key { get; } = key;

        public TValue Value { get; set; } = value;

        public DateTimeOffset? ExpiresAt { get; set; } = expiresAt;
    }
}

public sealed class MemoryCacheStoreFactory(ILogger? logger = null, ITelemetry? telemetry = null,
    TimeProvider? timeProvider = null) : ICacheStoreFactory
{
    public ICacheStore<TKey, TValue> Create<TKey, TValue>(string name, CachePolicy policy) where TKey : notnull =>
        new MemoryCacheStore<TKey, TValue>(name, policy, logger, telemetry, timeProvider);
}

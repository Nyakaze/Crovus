namespace Crovus.Cache;

public interface ICacheStore<in TKey, TValue> where TKey : notnull
{
    int Count { get; }

    ValueTask<TValue?> GetAsync(TKey key, CancellationToken cancellationToken = default);

    ValueTask SetAsync(TKey key, TValue value, CancellationToken cancellationToken = default);

    ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}

public interface ICacheStoreFactory
{
    ICacheStore<TKey, TValue> Create<TKey, TValue>(string name, CachePolicy policy) where TKey : notnull;
}

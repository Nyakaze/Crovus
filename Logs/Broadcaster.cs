using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Crovus.Logs;

internal sealed class Broadcaster<T>
{
    private readonly Lock _gate = new();

    private Subscription[] _subscriptions = [];

    public bool HasSubscribers => Volatile.Read(ref _subscriptions).Length > 0;

    public IDisposable Subscribe(Action<T> handler)
    {
        var subscription = new Subscription(this, handler);

        lock (_gate)
            _subscriptions = [.. _subscriptions, subscription];

        return subscription;
    }

    public async IAsyncEnumerable<T> ReadAsync(int capacity,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

        using var subscription = Subscribe(item => channel.Writer.TryWrite(item));

        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            yield return item;
    }

    public void Publish(T item)
    {
        foreach (var subscription in Volatile.Read(ref _subscriptions))
            subscription.Invoke(item);
    }

    private void Remove(Subscription subscription)
    {
        lock (_gate)
            _subscriptions = [.. _subscriptions.Where(existing => existing != subscription)];
    }

    private sealed class Subscription(Broadcaster<T> owner, Action<T> handler) : IDisposable
    {
        private volatile bool _disposed;

        public void Invoke(T item)
        {
            if (_disposed)
                return;

            try
            {
                handler(item);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            owner.Remove(this);
        }
    }
}

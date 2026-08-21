using System.Threading.Channels;

namespace Crovus.Queue;

public sealed class PriorityChannel<T>
{
    private readonly Channel<T> _high;
    private readonly Channel<T> _normal;

    public PriorityChannel(int normalCapacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(normalCapacity, 1);

        _high = Channel.CreateUnbounded<T>();

        _normal = Channel.CreateBounded<T>(new BoundedChannelOptions(normalCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        Capacity = normalCapacity;
    }

    public int Capacity { get; }

    public int Count => _high.Reader.Count + _normal.Reader.Count;

    public bool TryWrite(T item, QueuePriority priority = QueuePriority.Normal) =>
        priority is QueuePriority.High ? _high.Writer.TryWrite(item) : _normal.Writer.TryWrite(item);

    public ValueTask WriteAsync(T item, QueuePriority priority = QueuePriority.Normal,
        CancellationToken cancellationToken = default) =>
        priority is QueuePriority.High
            ? _high.Writer.WriteAsync(item, cancellationToken)
            : _normal.Writer.WriteAsync(item, cancellationToken);

    public async ValueTask<T> ReadAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (_high.Reader.TryRead(out var item) || _normal.Reader.TryRead(out item))
                return item;

            var high = _high.Reader.WaitToReadAsync(cancellationToken).AsTask();
            var normal = _normal.Reader.WaitToReadAsync(cancellationToken).AsTask();
            var ready = await Task.WhenAny(high, normal);

            if (await ready)
                continue;

            if (!await (ready == high ? normal : high))
                throw new ChannelClosedException();
        }
    }

    public void Complete(Exception? error = null)
    {
        _high.Writer.TryComplete(error);
        _normal.Writer.TryComplete(error);
    }
}

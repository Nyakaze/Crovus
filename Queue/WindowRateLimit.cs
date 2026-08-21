namespace Crovus.Queue;

public sealed class WindowRateLimit
{
    private readonly int _permits;
    private readonly TimeSpan _window;
    private readonly TimeProvider _time;
    private readonly DateTimeOffset[] _slots;
    private readonly Lock _gate = new();

    private int _head;
    private int _count;

    public WindowRateLimit(int permits, TimeSpan window, TimeProvider? timeProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(permits, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);

        _permits = permits;
        _window = window;
        _time = timeProvider ?? TimeProvider.System;
        _slots = new DateTimeOffset[permits];
    }

    public int Available
    {
        get
        {
            lock (_gate)
            {
                Trim(_time.GetUtcNow());
                return _permits - _count;
            }
        }
    }

    public TimeSpan TryAcquire()
    {
        lock (_gate)
        {
            var now = _time.GetUtcNow();
            Trim(now);

            if (_count == _permits)
                return _slots[_head] + _window - now;

            Append(now);
            return TimeSpan.Zero;
        }
    }

    public void Consume()
    {
        lock (_gate)
        {
            var now = _time.GetUtcNow();
            Trim(now);
            Append(now);
        }
    }

    public async ValueTask<TimeSpan> WaitAsync(CancellationToken cancellationToken = default)
    {
        var waited = TimeSpan.Zero;

        while (true)
        {
            var delay = TryAcquire();

            if (delay <= TimeSpan.Zero)
                return waited;

            waited += delay;
            await Task.Delay(delay, _time, cancellationToken);
        }
    }

    private void Trim(DateTimeOffset now)
    {
        while (_count > 0 && now - _slots[_head] >= _window)
        {
            _head = (_head + 1) % _permits;
            _count--;
        }
    }

    private void Append(DateTimeOffset now)
    {
        if (_count == _permits)
        {
            _slots[_head] = now;
            _head = (_head + 1) % _permits;
            return;
        }

        _slots[(_head + _count) % _permits] = now;
        _count++;
    }
}

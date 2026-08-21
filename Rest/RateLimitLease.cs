namespace Crovus.Rest;

public sealed class RateLimitLease : IDisposable
{
    private readonly RateLimiter _limiter;
    private readonly RouteKey _route;
    private readonly RateLimiter.Bucket _bucket;
    private bool _released;

    internal RateLimitLease(RateLimiter limiter, RouteKey route, RateLimiter.Bucket bucket)
    {
        _limiter = limiter;
        _route = route;
        _bucket = bucket;
    }

    public void Complete(HttpResponseMessage response)
    {
        ObjectDisposedException.ThrowIf(_released, this);
        _limiter.Complete(_route, _bucket, response);
    }

    public void Dispose()
    {
        if (_released)
            return;

        _released = true;
        RateLimiter.Release(_bucket);
    }
}

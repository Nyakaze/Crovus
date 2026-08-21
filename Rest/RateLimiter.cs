using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using Crovus.Logs;

namespace Crovus.Rest;

public sealed class RateLimiter
{
    private const int GlobalRequestsPerSecond = 50;
    private const string LogCategory = "Rest.RateLimiter";

    private readonly TimeProvider _time;
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private readonly ConcurrentDictionary<string, string> _hashesByRoute = new();
    private readonly ConcurrentDictionary<string, Bucket> _buckets = new();
    private readonly Lock _globalLock = new();

    private int _globalRemaining = GlobalRequestsPerSecond;
    private DateTimeOffset _globalWindowEndsAt;
    private DateTimeOffset _globalRetryAt;

    public RateLimiter(TimeProvider? timeProvider = null, ILogger? logger = null, ITelemetry? telemetry = null)
    {
        _time = timeProvider ?? TimeProvider.System;
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;
    }

    public RateLimiter(DiagnosticsHub diagnostics, TimeProvider? timeProvider = null)
        : this(timeProvider, diagnostics, diagnostics)
    {
    }

    public async ValueTask<RateLimitLease> AcquireAsync(RouteKey route, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var bucket = ResolveBucket(route);
            await bucket.Gate.WaitAsync(cancellationToken);

            var now = _time.GetUtcNow();
            var delay = bucket.TryConsume(now);
            var global = false;

            if (delay <= TimeSpan.Zero)
            {
                delay = TryConsumeGlobal(now);
                global = delay > TimeSpan.Zero;

                if (!global)
                    return new RateLimitLease(this, route, bucket);

                bucket.Refund();
            }

            bucket.Gate.Release();
            ReportDelay(route, bucket, delay, global);

            await Task.Delay(delay, _time, cancellationToken);
        }
    }

    public static TimeSpan? GetRetryDelay(HttpResponseMessage response)
    {
        if (response.StatusCode is not HttpStatusCode.TooManyRequests)
            return null;

        if (TryReadHeader(response, "X-RateLimit-Reset-After") is { } resetAfter)
            return TimeSpan.FromSeconds(resetAfter);

        if (response.Headers.RetryAfter?.Delta is { } delta)
            return delta;

        return TimeSpan.FromSeconds(1);
    }

    internal void Complete(RouteKey route, Bucket bucket, HttpResponseMessage response)
    {
        var now = _time.GetUtcNow();

        if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var hashes))
        {
            var hash = hashes.FirstOrDefault();
            if (!string.IsNullOrEmpty(hash) && bucket.Hash != hash)
            {
                bucket.Hash = hash;
                _hashesByRoute[route.ToString()] = hash;
                _buckets.TryAdd($"{hash}:{route.MajorParameter}", bucket);

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug($"Mapped {route} to bucket {hash}");
            }
        }

        if (response.StatusCode is HttpStatusCode.TooManyRequests)
        {
            var retryAfter = GetRetryDelay(response) ?? TimeSpan.FromSeconds(1);
            var global = IsGlobalLimit(response);

            if (global)
                ApplyGlobalRetry(now + retryAfter);
            else
                bucket.Exhaust(now + retryAfter);

            _logger.LogWarning(
                $"Rate limited on {route} ({(global ? "global" : $"bucket {bucket.Hash ?? "unknown"}")}), retrying in {retryAfter.TotalMilliseconds:F0}ms");

            if (_telemetry.HasSubscribers)
                _telemetry.Emit(new RateLimitHit(route.ToString(), bucket.Hash, retryAfter, global));

            return;
        }

        var limit = TryReadHeader(response, "X-RateLimit-Limit");
        var remaining = TryReadHeader(response, "X-RateLimit-Remaining");
        var reset = TryReadHeader(response, "X-RateLimit-Reset-After");

        if (limit is null || remaining is null || reset is null)
            return;

        var resetAfter = TimeSpan.FromSeconds(reset.Value);
        bucket.Update((int)limit.Value, (int)remaining.Value, resetAfter, now);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace($"{route} has {(int)remaining.Value}/{(int)limit.Value} left, resets in {resetAfter.TotalMilliseconds:F0}ms");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new RateLimitBucketUpdated(route.ToString(), bucket.Hash, (int)limit.Value,
                (int)remaining.Value, resetAfter));
    }

    internal static void Release(Bucket bucket) => bucket.Gate.Release();

    private static bool IsGlobalLimit(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-RateLimit-Scope", out var scopes))
            return string.Equals(scopes.FirstOrDefault(), "global", StringComparison.OrdinalIgnoreCase);

        return response.Headers.TryGetValues("X-RateLimit-Global", out var global)
               && bool.TryParse(global.FirstOrDefault(), out var isGlobal)
               && isGlobal;
    }

    private static double? TryReadHeader(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues(name, out var values))
            return null;

        var raw = values.FirstOrDefault();
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private void ReportDelay(RouteKey route, Bucket bucket, TimeSpan delay, bool global)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug(
                $"Delaying {route} by {delay.TotalMilliseconds:F0}ms ({(global ? "global limit" : $"bucket {bucket.Hash ?? "unknown"}")})");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new RateLimitDelayed(route.ToString(), delay, global));
    }

    private Bucket ResolveBucket(RouteKey route)
    {
        var routeKey = route.ToString();
        var key = _hashesByRoute.TryGetValue(routeKey, out var hash)
            ? $"{hash}:{route.MajorParameter}"
            : routeKey;

        return _buckets.GetOrAdd(key, _ => new Bucket());
    }

    private void ApplyGlobalRetry(DateTimeOffset retryAt)
    {
        lock (_globalLock)
        {
            if (retryAt > _globalRetryAt)
                _globalRetryAt = retryAt;
        }
    }

    private TimeSpan TryConsumeGlobal(DateTimeOffset now)
    {
        lock (_globalLock)
        {
            if (now < _globalRetryAt)
                return _globalRetryAt - now;

            if (now >= _globalWindowEndsAt)
            {
                _globalWindowEndsAt = now + TimeSpan.FromSeconds(1);
                _globalRemaining = GlobalRequestsPerSecond;
            }

            if (_globalRemaining <= 0)
                return _globalWindowEndsAt - now;

            _globalRemaining--;
            return TimeSpan.Zero;
        }
    }

    internal sealed class Bucket
    {
        public readonly SemaphoreSlim Gate = new(1, 1);

        public string? Hash;

        private int _limit = 1;
        private int _remaining = 1;
        private DateTimeOffset _resetAt;

        public TimeSpan TryConsume(DateTimeOffset now)
        {
            if (now >= _resetAt)
                _remaining = _limit;

            if (_remaining <= 0)
                return _resetAt - now;

            _remaining--;
            return TimeSpan.Zero;
        }

        public void Refund() => _remaining++;

        public void Update(int limit, int remaining, TimeSpan resetAfter, DateTimeOffset now)
        {
            _limit = limit > 0 ? limit : 1;
            _remaining = remaining;
            _resetAt = now + resetAfter;
        }

        public void Exhaust(DateTimeOffset until)
        {
            _remaining = 0;
            _resetAt = until;
        }
    }
}

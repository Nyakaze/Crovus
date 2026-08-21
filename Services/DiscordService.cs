using System.Diagnostics;
using Crovus.Logs;
using Crovus.Rest;

namespace Crovus.Services;

public abstract class DiscordService
{
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;

    protected DiscordService(IDiscordRest rest, string name, ILogger? logger = null, ITelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(rest);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Rest = rest;
        Name = name;
        _logger = (logger ?? NullLogger.Instance).ForCategory($"Service.{name}");
        _telemetry = telemetry ?? NullTelemetry.Instance;
    }

    public string Name { get; }

    protected IDiscordRest Rest { get; }

    protected async Task<TResult> TrackAsync<TResult>(string operation, string context, Func<Task<TResult>> action,
        Func<TResult, string> describe, LogLevel level = LogLevel.Information)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var result = await action();
            Succeeded(operation, start, level, describe(result));
            return result;
        }
        catch (Exception exception)
        {
            Failed(operation, start, exception, context);
            throw;
        }
    }

    protected async Task TrackAsync(string operation, string context, Func<Task> action, string description,
        LogLevel level = LogLevel.Information)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await action();
            Succeeded(operation, start, level, description);
        }
        catch (Exception exception)
        {
            Failed(operation, start, exception, context);
            throw;
        }
    }

    protected void Emit(TelemetryEvent telemetryEvent)
    {
        if (_telemetry.HasSubscribers)
            _telemetry.Emit(telemetryEvent);
    }

    protected void Warn(string message, Exception? exception = null) => _logger.LogWarning(message, exception);

    protected static string Because(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? string.Empty : $" (reason: {reason})";

    private void Succeeded(string operation, long start, LogLevel level, string message)
    {
        var duration = Stopwatch.GetElapsedTime(start);

        if (_logger.IsEnabled(level))
            _logger.Log(level, $"{message} in {duration.TotalMilliseconds:F0}ms");

        Emit(new ServiceOperationCompleted(Name, operation, duration));
    }

    private void Failed(string operation, long start, Exception exception, string context)
    {
        var duration = Stopwatch.GetElapsedTime(start);

        if (exception is OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"{operation} canceled for {context} after {duration.TotalMilliseconds:F0}ms");
        }
        else
        {
            _logger.LogError($"{operation} failed for {context} after {duration.TotalMilliseconds:F0}ms", exception);
        }

        Emit(new ServiceOperationFailed(Name, operation, exception.GetType().Name, duration));
    }
}

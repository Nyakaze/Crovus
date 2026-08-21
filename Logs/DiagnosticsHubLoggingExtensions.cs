using System.Collections.Concurrent;
using MsLogging = Microsoft.Extensions.Logging;

namespace Crovus.Logs;

public static class DiagnosticsHubLoggingExtensions
{
    private const string TelemetryCategory = "Crovus.Telemetry";

    public static IDisposable ForwardLogsTo(this DiagnosticsHub hub, MsLogging.ILoggerFactory factory)
    {
        var loggers = new ConcurrentDictionary<string, MsLogging.ILogger>();

        return hub.SubscribeLogs(entry =>
        {
            var category = entry.Category.Length == 0 ? "Crovus" : $"Crovus.{entry.Category}";
            var logger = loggers.GetOrAdd(category, factory.CreateLogger);

            MicrosoftLoggerAdapter.Write(logger, entry.Level, entry.Message, entry.Exception);
        });
    }

    public static IDisposable ForwardTelemetryTo(this DiagnosticsHub hub, MsLogging.ILoggerFactory factory,
        LogLevel level = LogLevel.Debug)
    {
        var logger = factory.CreateLogger(TelemetryCategory);
        var microsoftLevel = MicrosoftLoggerAdapter.ToMicrosoft(level);

        return hub.SubscribeTelemetry(telemetryEvent =>
        {
            if (!logger.IsEnabled(microsoftLevel))
                return;

            logger.Log(microsoftLevel, default, telemetryEvent, null,
                static (state, _) => $"{state.GetType().Name} {state}");
        });
    }
}

namespace Crovus.Logs;

public sealed record LogEntry(DateTimeOffset Timestamp, LogLevel Level, string Category, string Message,
    Exception? Exception = null);

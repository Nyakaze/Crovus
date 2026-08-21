namespace Crovus.Logs;

public sealed class ConsoleLogWriter : IDisposable
{
    private readonly IDisposable _subscription;
    private readonly TextWriter _output;
    private readonly Lock _gate = new();

    private ConsoleLogWriter(DiagnosticsHub hub, LogLevel minimumLevel, TextWriter output)
    {
        _output = output;
        _subscription = hub.SubscribeLogs(minimumLevel, Write);
    }

    public static ConsoleLogWriter Attach(DiagnosticsHub hub, LogLevel minimumLevel = LogLevel.Information,
        TextWriter? output = null) => new(hub, minimumLevel, output ?? Console.Out);

    public static string Format(LogEntry entry)
    {
        var category = entry.Category.Length == 0 ? "Crovus" : entry.Category;
        var line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Abbreviate(entry.Level)} {category}: {entry.Message}";

        return entry.Exception is null ? line : $"{line}{Environment.NewLine}{entry.Exception}";
    }

    public void Dispose() => _subscription.Dispose();

    private static string Abbreviate(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "___"
    };

    private void Write(LogEntry entry)
    {
        lock (_gate)
            _output.WriteLine(Format(entry));
    }
}

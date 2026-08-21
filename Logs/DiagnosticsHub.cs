namespace Crovus.Logs;

public sealed class DiagnosticsHub : ILogger, ITelemetry
{
    private const int DefaultStreamCapacity = 1024;

    private readonly Broadcaster<LogEntry> _logs = new();
    private readonly Broadcaster<TelemetryEvent> _telemetry = new();
    private readonly TimeProvider _time;

    public DiagnosticsHub(LogLevel minimumLevel = LogLevel.Information, TimeProvider? timeProvider = null)
    {
        MinimumLevel = minimumLevel;
        _time = timeProvider ?? TimeProvider.System;
    }

    public LogLevel MinimumLevel { get; set; }

    public bool HasSubscribers => _telemetry.HasSubscribers;

    public bool IsEnabled(LogLevel level) =>
        level is not LogLevel.None && level >= MinimumLevel && _logs.HasSubscribers;

    public void Log(LogLevel level, string message, Exception? exception = null) =>
        Write(level, string.Empty, message, exception);

    public ILogger ForCategory(string category) => new CategoryLogger(this, category);

    public void Emit(TelemetryEvent telemetryEvent)
    {
        if (telemetryEvent.Timestamp == default)
            telemetryEvent = telemetryEvent with { Timestamp = _time.GetUtcNow() };

        _telemetry.Publish(telemetryEvent);
    }

    public IDisposable SubscribeLogs(Action<LogEntry> handler) => _logs.Subscribe(handler);

    public IDisposable SubscribeLogs(LogLevel minimumLevel, Action<LogEntry> handler) =>
        _logs.Subscribe(entry =>
        {
            if (entry.Level >= minimumLevel)
                handler(entry);
        });

    public IDisposable SubscribeTelemetry(Action<TelemetryEvent> handler) => _telemetry.Subscribe(handler);

    public IDisposable SubscribeTelemetry<TEvent>(Action<TEvent> handler) where TEvent : TelemetryEvent =>
        _telemetry.Subscribe(telemetryEvent =>
        {
            if (telemetryEvent is TEvent typed)
                handler(typed);
        });

    public IAsyncEnumerable<LogEntry> ReadLogsAsync(CancellationToken cancellationToken = default) =>
        _logs.ReadAsync(DefaultStreamCapacity, cancellationToken);

    public IAsyncEnumerable<TelemetryEvent> ReadTelemetryAsync(CancellationToken cancellationToken = default) =>
        _telemetry.ReadAsync(DefaultStreamCapacity, cancellationToken);

    private void Write(LogLevel level, string category, string message, Exception? exception)
    {
        if (level is LogLevel.None || level < MinimumLevel)
            return;

        _logs.Publish(new LogEntry(_time.GetUtcNow(), level, category, message, exception));
    }

    private sealed class CategoryLogger(DiagnosticsHub hub, string category) : ILogger
    {
        public bool IsEnabled(LogLevel level) => hub.IsEnabled(level);

        public void Log(LogLevel level, string message, Exception? exception = null) =>
            hub.Write(level, category, message, exception);

        public ILogger ForCategory(string childCategory) =>
            new CategoryLogger(hub, category.Length == 0 ? childCategory : $"{category}.{childCategory}");
    }
}

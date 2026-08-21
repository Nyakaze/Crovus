using MsLogging = Microsoft.Extensions.Logging;

namespace Crovus.Logs;

public sealed class MicrosoftLoggerAdapter : ILogger
{
    private const string RootCategory = "Crovus";

    private readonly MsLogging.ILogger _logger;
    private readonly MsLogging.ILoggerFactory? _factory;
    private readonly string _category;

    public MicrosoftLoggerAdapter(MsLogging.ILoggerFactory factory, string category = RootCategory)
    {
        _factory = factory;
        _category = category;
        _logger = factory.CreateLogger(category);
    }

    public MicrosoftLoggerAdapter(MsLogging.ILogger logger)
    {
        _logger = logger;
        _category = RootCategory;
    }

    public bool IsEnabled(LogLevel level) => level is not LogLevel.None && _logger.IsEnabled(ToMicrosoft(level));

    public void Log(LogLevel level, string message, Exception? exception = null) =>
        Write(_logger, level, message, exception);

    public ILogger ForCategory(string category) => _factory is null
        ? this
        : new MicrosoftLoggerAdapter(_factory, $"{_category}.{category}");

    public static MsLogging.LogLevel ToMicrosoft(LogLevel level) => level switch
    {
        LogLevel.Trace => MsLogging.LogLevel.Trace,
        LogLevel.Debug => MsLogging.LogLevel.Debug,
        LogLevel.Information => MsLogging.LogLevel.Information,
        LogLevel.Warning => MsLogging.LogLevel.Warning,
        LogLevel.Error => MsLogging.LogLevel.Error,
        LogLevel.Critical => MsLogging.LogLevel.Critical,
        _ => MsLogging.LogLevel.None
    };

    public static LogLevel FromMicrosoft(MsLogging.LogLevel level) => level switch
    {
        MsLogging.LogLevel.Trace => LogLevel.Trace,
        MsLogging.LogLevel.Debug => LogLevel.Debug,
        MsLogging.LogLevel.Information => LogLevel.Information,
        MsLogging.LogLevel.Warning => LogLevel.Warning,
        MsLogging.LogLevel.Error => LogLevel.Error,
        MsLogging.LogLevel.Critical => LogLevel.Critical,
        _ => LogLevel.None
    };

    internal static void Write(MsLogging.ILogger logger, LogLevel level, string message, Exception? exception) =>
        logger.Log(ToMicrosoft(level), default, message, exception, static (state, _) => state);
}

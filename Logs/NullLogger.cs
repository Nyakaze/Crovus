namespace Crovus.Logs;

public sealed class NullLogger : ILogger
{
    public static readonly NullLogger Instance = new();

    private NullLogger()
    {
    }

    public bool IsEnabled(LogLevel level) => false;

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
    }

    public ILogger ForCategory(string category) => this;
}

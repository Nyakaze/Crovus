namespace Crovus.Logs;

public interface ILogger
{
    bool IsEnabled(LogLevel level);

    void Log(LogLevel level, string message, Exception? exception = null);

    ILogger ForCategory(string category);
}

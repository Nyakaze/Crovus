using Crovus.Models;

namespace Crovus.RichPresence;

public sealed record RichPresenceClientOptions
{
    public static IReadOnlyList<string> DefaultPipeNames { get; } =
        Enumerable.Range(0, 10).Select(index => $"discord-ipc-{index}").ToArray();

    public required Snowflake ApplicationId { get; init; }

    public IReadOnlyList<string> PipeNames { get; init; } = DefaultPipeNames;

    public int ProcessId { get; init; } = Environment.ProcessId;

    public bool AutoReconnect { get; init; } = true;

    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan MaxReconnectDelay { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan NotRunningPollInterval { get; init; } = TimeSpan.FromSeconds(5);

    public int UpdatesPerWindow { get; init; } = RichPresenceLimits.UpdatesPerWindow;

    public TimeSpan UpdateWindow { get; init; } = RichPresenceLimits.UpdateWindow;

    public TimeSpan? RefreshInterval { get; init; }

    public static RichPresenceClientOptions For(Snowflake applicationId) => new() { ApplicationId = applicationId };
}

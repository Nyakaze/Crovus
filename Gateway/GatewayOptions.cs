using Crovus.Models;

namespace Crovus.Gateway;

public sealed record GatewayOptions
{
    public required string Token { get; init; }

    public required GatewayIntents Intents { get; init; }

    public string Url { get; init; } = "wss://gateway.discord.gg";

    public int ApiVersion { get; init; } = 10;

    public int LargeThreshold { get; init; } = 50;

    public int? ShardId { get; init; }

    public int? ShardCount { get; init; }

    public PresenceUpdate? Presence { get; init; }

    public string OperatingSystem { get; init; } = Environment.OSVersion.Platform.ToString();

    public string Library { get; init; } = "Crovus";

    public int EventQueueCapacity { get; init; } = 1024;

    public int CommandQueueCapacity { get; init; } = 256;

    public int CommandsPerWindow { get; init; } = 120;

    public TimeSpan CommandWindow { get; init; } = TimeSpan.FromSeconds(60);

    public TimeSpan MaxReconnectDelay { get; init; } = TimeSpan.FromSeconds(60);

    public Uri BuildUri(string? resumeUrl)
    {
        var basis = string.IsNullOrWhiteSpace(resumeUrl) ? Url : resumeUrl;
        return new Uri($"{basis.TrimEnd('/')}/?v={ApiVersion}&encoding=json");
    }

    internal int[]? BuildShard() =>
        ShardId is { } id && ShardCount is { } count ? [id, count] : null;
}

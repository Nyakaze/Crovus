using Crovus.Cache;
using Crovus.Gateway;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Client;

public sealed record CrovusClientOptions
{
    public required string Token { get; init; }

    public required GatewayIntents Intents { get; init; }

    public string TokenType { get; init; } = "Bot";

    public bool EnableCache { get; init; } = true;

    public bool EnableRestLogging { get; init; } = true;

    public bool SequentialDispatch { get; init; }

    public LogLevel MinimumLogLevel { get; init; } = LogLevel.Information;

    public int? ShardId { get; init; }

    public int? ShardCount { get; init; }

    public PresenceUpdate? Presence { get; init; }

    public int PresenceCapacity { get; init; } = 25_000;

    public CacheOptions Cache { get; init; } = new();

    public Func<DiscordRestOptions, DiscordRestOptions>? ConfigureRest { get; init; }

    public Func<GatewayOptions, GatewayOptions>? ConfigureGateway { get; init; }

    public static CrovusClientOptions For(string token, GatewayIntents intents) =>
        new() { Token = token, Intents = intents };

    public DiscordRestOptions BuildRest()
    {
        var options = new DiscordRestOptions { Token = Token, TokenType = TokenType };

        return ConfigureRest?.Invoke(options) ?? options;
    }

    public GatewayOptions BuildGateway()
    {
        var options = new GatewayOptions
        {
            Token = Token,
            Intents = Intents,
            ShardId = ShardId,
            ShardCount = ShardCount,
            Presence = Presence
        };

        return ConfigureGateway?.Invoke(options) ?? options;
    }

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Token);
        ArgumentException.ThrowIfNullOrWhiteSpace(TokenType);

        if (ShardId is { } id && ShardCount is { } count && (id < 0 || id >= count))
            throw new ArgumentOutOfRangeException(nameof(ShardId),
                $"A shard id must be between 0 and {count - 1} but was {id}.");

        if (ShardCount is { } total && total < 1)
            throw new ArgumentOutOfRangeException(nameof(ShardCount),
                $"A shard count must be at least 1 but was {total}.");

        if (PresenceCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(PresenceCapacity),
                $"A presence capacity must be at least 1 but was {PresenceCapacity}.");
    }
}

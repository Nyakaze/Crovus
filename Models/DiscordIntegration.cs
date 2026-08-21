using System.Text.Json.Serialization;

namespace Crovus.Models;

public enum IntegrationExpireBehavior
{
    RemoveRole = 0,
    Kick = 1
}

public sealed record DiscordIntegrationAccount(string Id, string Name);

public sealed record DiscordIntegration
{
    public required Snowflake Id { get; init; }

    public Snowflake? GuildId { get; init; }

    public required string Name { get; init; }

    public string Type { get; init; } = string.Empty;

    public bool Enabled { get; init; }

    public bool Syncing { get; init; }

    public bool Revoked { get; init; }

    public bool EnableEmoticons { get; init; }

    public Snowflake? RoleId { get; init; }

    public IntegrationExpireBehavior ExpireBehavior { get; init; }

    public int? ExpireGracePeriod { get; init; }

    public DiscordUser? User { get; init; }

    public DiscordIntegrationAccount? Account { get; init; }

    public DateTimeOffset? SyncedAt { get; init; }

    public int? SubscriberCount { get; init; }

    public Snowflake? ApplicationId { get; init; }

    public IReadOnlyList<string> Scopes { get; init; } = [];

    [JsonIgnore]
    public bool IsBot => ApplicationId is not null;

    [JsonIgnore]
    public bool IsTwitch => string.Equals(Type, "twitch", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsYouTube => string.Equals(Type, "youtube", StringComparison.OrdinalIgnoreCase);

    public DiscordIntegration In(Snowflake guildId) => GuildId is null ? this with { GuildId = guildId } : this;

    public override string ToString() => $"{Name} ({Type})";
}

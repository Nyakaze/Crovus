using System.Text.Json.Serialization;

namespace Crovus.Models;

public enum ScheduledEventStatus
{
    Scheduled = 1,
    Active = 2,
    Completed = 3,
    Canceled = 4
}

public enum ScheduledEventEntityType
{
    StageInstance = 1,
    Voice = 2,
    External = 3
}

public enum ScheduledEventPrivacyLevel
{
    GuildOnly = 2
}

public sealed record DiscordScheduledEvent
{
    public required Snowflake Id { get; init; }

    public Snowflake? GuildId { get; init; }

    [JsonIgnore]
    public bool IsPartial { get; init; }

    public static DiscordScheduledEvent Partial(Snowflake id, Snowflake? guildId = null) =>
        new() { Id = id, GuildId = guildId, Name = string.Empty, IsPartial = true };

    public Snowflake? ChannelId { get; init; }

    public Snowflake? CreatorId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Image { get; init; }

    public DateTimeOffset StartsAt { get; init; }

    public DateTimeOffset? EndsAt { get; init; }

    public ScheduledEventStatus Status { get; init; } = ScheduledEventStatus.Scheduled;

    public ScheduledEventEntityType EntityType { get; init; } = ScheduledEventEntityType.External;

    public ScheduledEventPrivacyLevel PrivacyLevel { get; init; } = ScheduledEventPrivacyLevel.GuildOnly;

    public Snowflake? EntityId { get; init; }

    public string? Location { get; init; }

    public DiscordUser? Creator { get; init; }

    public int? UserCount { get; init; }

    [JsonIgnore]
    public bool IsActive => Status is ScheduledEventStatus.Active;

    [JsonIgnore]
    public bool IsExternal => EntityType is ScheduledEventEntityType.External;

    [JsonIgnore]
    public TimeSpan? Duration => EndsAt - StartsAt;

    [JsonIgnore]
    public TimeSpan? StartsIn => StartsAt > DateTimeOffset.UtcNow ? StartsAt - DateTimeOffset.UtcNow : null;

    [JsonIgnore]
    public string? Url => GuildId is { } guildId ? $"https://discord.com/events/{guildId}/{Id}" : null;

    [JsonIgnore]
    public string? ImageUrl => Image is null
        ? null
        : $"https://cdn.discordapp.com/guild-events/{Id}/{Image}.png";

    public DiscordScheduledEvent In(Snowflake guildId) => GuildId is null ? this with { GuildId = guildId } : this;

    public override string ToString() => Name;
}

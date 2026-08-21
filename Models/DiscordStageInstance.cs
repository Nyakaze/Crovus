using System.Text.Json.Serialization;

namespace Crovus.Models;

public enum StagePrivacyLevel
{
    Public = 1,
    GuildOnly = 2
}

public sealed record DiscordStageInstance
{
    public required Snowflake Id { get; init; }

    [JsonIgnore]
    public bool IsPartial { get; init; }

    public static DiscordStageInstance Partial(Snowflake id, Snowflake channelId, Snowflake? guildId = null) =>
        new() { Id = id, ChannelId = channelId, GuildId = guildId, IsPartial = true };

    public Snowflake? GuildId { get; init; }

    public required Snowflake ChannelId { get; init; }

    public string Topic { get; init; } = string.Empty;

    public StagePrivacyLevel PrivacyLevel { get; init; } = StagePrivacyLevel.GuildOnly;

    public Snowflake? ScheduledEventId { get; init; }

    [JsonIgnore]
    public bool IsPublic => PrivacyLevel is StagePrivacyLevel.Public;

    public DiscordStageInstance In(Snowflake guildId) => GuildId is null ? this with { GuildId = guildId } : this;

    public override string ToString() => Topic;
}

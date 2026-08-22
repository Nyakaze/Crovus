using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public enum StagePrivacyLevel
{
    Public = 1,
    GuildOnly = 2
}

public sealed record DiscordStageInstance : IBoundEntity
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

    private EntityBinding _binding;

    public DiscordStageInstance Bind(ICrovusContext context)
    {
        var bound = this with { };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}

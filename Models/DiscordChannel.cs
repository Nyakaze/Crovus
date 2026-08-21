using System.Text.Json.Serialization;

namespace Crovus.Models;

public sealed record DiscordChannel(Snowflake Id, Snowflake? GuildId, ChannelType Type, string Name,
    Snowflake? ParentId, bool IsThread
    )
{
    [JsonIgnore]
    public bool IsPartial { get; init; }

    public static DiscordChannel Partial(Snowflake id, Snowflake? guildId = null, Snowflake? parentId = null,
        bool isThread = false) =>
        new(id, guildId, isThread ? ChannelType.PublicThread : ChannelType.Unknown, string.Empty, parentId, isThread)
        {
            IsPartial = true
        };

    public DiscordChannel In(Snowflake guildId) => GuildId is null ? this with { GuildId = guildId } : this;

    public Snowflake WebhookChannelId => IsThread ? ParentId ?? Id : Id;

    public Snowflake? ThreadId => IsThread ? Id : null;

    public string Mention => $"<#{Id.Value}>";

    public bool SupportsWebhooks => GuildId is not null && (IsThread || Type is
        ChannelType.GuildText or ChannelType.GuildVoice or ChannelType.GuildAnnouncement
        or ChannelType.GuildForum or ChannelType.GuildMedia or ChannelType.GuildStageVoice);
}

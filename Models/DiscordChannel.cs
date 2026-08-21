namespace Crovus.Models;

public sealed record DiscordChannel(Snowflake Id, Snowflake? GuildId, ChannelType Type, string Name,
    Snowflake? ParentId, bool IsThread
    )
{
    public Snowflake WebhookChannelId => IsThread ? ParentId ?? Id : Id;

    public Snowflake? ThreadId => IsThread ? Id : null;

    public bool SupportsWebhooks => GuildId is not null && (IsThread || Type is
        ChannelType.GuildText or ChannelType.GuildVoice or ChannelType.GuildAnnouncement
        or ChannelType.GuildForum or ChannelType.GuildMedia or ChannelType.GuildStageVoice);
}

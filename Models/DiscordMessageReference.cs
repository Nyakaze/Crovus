namespace Crovus.Models;

public sealed record DiscordMessageReference(Snowflake? MessageId, Snowflake? ChannelId, Snowflake? GuildId,
    MessageReferenceType Type = MessageReferenceType.Default, bool FailIfNotExists = true);

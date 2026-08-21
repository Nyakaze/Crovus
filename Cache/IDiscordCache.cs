using Crovus.Models;

namespace Crovus.Cache;

public readonly record struct ReactionKey(Snowflake MessageId, string Emoji)
{
    public static ReactionKey For(Snowflake messageId, DiscordEmoji emoji) =>
        new(messageId, emoji.Id is { } id ? id.Value.ToString() : emoji.Name);
}

public readonly record struct MemberKey(Snowflake GuildId, Snowflake UserId)
{
    public override string ToString() => $"{GuildId}/{UserId}";
}

public sealed record CacheStatistics(long Hits, long Misses, long Writes, long Invalidations)
{
    public long Lookups => Hits + Misses;

    public double HitRate => Lookups == 0 ? 0 : (double)Hits / Lookups;
}

public interface IDiscordCache
{
    CacheStatistics Statistics { get; }

    ValueTask<DiscordChannel?> GetChannelAsync(Snowflake channelId, CancellationToken cancellationToken = default);

    ValueTask SetChannelAsync(DiscordChannel channel, CancellationToken cancellationToken = default);

    ValueTask RemoveChannelAsync(Snowflake channelId, CancellationToken cancellationToken = default);

    ValueTask<DiscordMessage?> GetMessageAsync(Snowflake messageId, CancellationToken cancellationToken = default);

    ValueTask SetMessageAsync(DiscordMessage message, CancellationToken cancellationToken = default);

    ValueTask RemoveMessageAsync(Snowflake messageId, CancellationToken cancellationToken = default);

    ValueTask<DiscordUser?> GetUserAsync(Snowflake userId, CancellationToken cancellationToken = default);

    ValueTask SetUserAsync(DiscordUser user, CancellationToken cancellationToken = default);

    ValueTask<DiscordWebhook?> GetWebhookAsync(Snowflake webhookId, CancellationToken cancellationToken = default);

    ValueTask SetWebhookAsync(DiscordWebhook webhook, CancellationToken cancellationToken = default);

    ValueTask RemoveWebhookAsync(Snowflake webhookId, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<DiscordWebhook>?> GetChannelWebhooksAsync(Snowflake channelId,
        CancellationToken cancellationToken = default);

    ValueTask SetChannelWebhooksAsync(Snowflake channelId, IReadOnlyList<DiscordWebhook> webhooks,
        CancellationToken cancellationToken = default);

    ValueTask RemoveChannelWebhooksAsync(Snowflake channelId, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlySet<Snowflake>?> GetReactionsAsync(Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default);

    ValueTask AddReactionAsync(Snowflake messageId, DiscordEmoji emoji, Snowflake userId,
        CancellationToken cancellationToken = default);

    ValueTask RemoveReactionAsync(Snowflake messageId, DiscordEmoji emoji, Snowflake userId,
        CancellationToken cancellationToken = default);

    ValueTask RemoveReactionsAsync(Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default);

    ValueTask ClearReactionsAsync(Snowflake messageId, CancellationToken cancellationToken = default);

    ValueTask<DiscordGuild?> GetGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default);

    ValueTask SetGuildAsync(DiscordGuild guild, CancellationToken cancellationToken = default);

    ValueTask RemoveGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default);

    ValueTask<DiscordMember?> GetMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default);

    ValueTask SetMemberAsync(Snowflake guildId, DiscordMember member, CancellationToken cancellationToken = default);

    ValueTask RemoveMemberAsync(Snowflake guildId, Snowflake userId, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<DiscordRole>?> GetGuildRolesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default);

    ValueTask SetGuildRolesAsync(Snowflake guildId, IReadOnlyList<DiscordRole> roles,
        CancellationToken cancellationToken = default);

    ValueTask RemoveGuildRolesAsync(Snowflake guildId, CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}

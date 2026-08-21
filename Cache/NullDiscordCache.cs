using Crovus.Models;

namespace Crovus.Cache;

public sealed class NullDiscordCache : IDiscordCache
{
    public static NullDiscordCache Instance { get; } = new();

    private NullDiscordCache()
    {
    }

    public CacheStatistics Statistics { get; } = new(0, 0, 0, 0);

    public ValueTask<DiscordChannel?> GetChannelAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) => default;

    public ValueTask SetChannelAsync(DiscordChannel channel, CancellationToken cancellationToken = default) => default;

    public ValueTask RemoveChannelAsync(Snowflake channelId, CancellationToken cancellationToken = default) => default;

    public ValueTask<DiscordMessage?> GetMessageAsync(Snowflake messageId,
        CancellationToken cancellationToken = default) => default;

    public ValueTask SetMessageAsync(DiscordMessage message, CancellationToken cancellationToken = default) => default;

    public ValueTask RemoveMessageAsync(Snowflake messageId, CancellationToken cancellationToken = default) => default;

    public ValueTask<DiscordUser?> GetUserAsync(Snowflake userId, CancellationToken cancellationToken = default) =>
        default;

    public ValueTask SetUserAsync(DiscordUser user, CancellationToken cancellationToken = default) => default;

    public ValueTask<DiscordWebhook?> GetWebhookAsync(Snowflake webhookId,
        CancellationToken cancellationToken = default) => default;

    public ValueTask SetWebhookAsync(DiscordWebhook webhook, CancellationToken cancellationToken = default) => default;

    public ValueTask RemoveWebhookAsync(Snowflake webhookId, CancellationToken cancellationToken = default) => default;

    public ValueTask<IReadOnlyList<DiscordWebhook>?> GetChannelWebhooksAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) => default;

    public ValueTask SetChannelWebhooksAsync(Snowflake channelId, IReadOnlyList<DiscordWebhook> webhooks,
        CancellationToken cancellationToken = default) => default;

    public ValueTask RemoveChannelWebhooksAsync(Snowflake channelId, CancellationToken cancellationToken = default) =>
        default;

    public ValueTask<IReadOnlySet<Snowflake>?> GetReactionsAsync(Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default) => default;

    public ValueTask AddReactionAsync(Snowflake messageId, DiscordEmoji emoji, Snowflake userId,
        CancellationToken cancellationToken = default) => default;

    public ValueTask RemoveReactionAsync(Snowflake messageId, DiscordEmoji emoji, Snowflake userId,
        CancellationToken cancellationToken = default) => default;

    public ValueTask RemoveReactionsAsync(Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default) => default;

    public ValueTask ClearReactionsAsync(Snowflake messageId, CancellationToken cancellationToken = default) =>
        default;

    public ValueTask<DiscordGuild?> GetGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default) =>
        default;

    public ValueTask SetGuildAsync(DiscordGuild guild, CancellationToken cancellationToken = default) => default;

    public ValueTask RemoveGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default) => default;

    public ValueTask<DiscordMember?> GetMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default) => default;

    public ValueTask SetMemberAsync(Snowflake guildId, DiscordMember member,
        CancellationToken cancellationToken = default) => default;

    public ValueTask RemoveMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default) => default;

    public ValueTask<IReadOnlyList<DiscordRole>?> GetGuildRolesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) => default;

    public ValueTask SetGuildRolesAsync(Snowflake guildId, IReadOnlyList<DiscordRole> roles,
        CancellationToken cancellationToken = default) => default;

    public ValueTask RemoveGuildRolesAsync(Snowflake guildId, CancellationToken cancellationToken = default) =>
        default;

    public ValueTask ClearAsync(CancellationToken cancellationToken = default) => default;
}

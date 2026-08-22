using Crovus.Factory;

namespace Crovus.Models;

public static class MessageFluent
{
    public static Task<DiscordMessage> ReplyAsync(this DiscordMessage message, string content,
        bool failIfNotExists = false, CancellationToken cancellationToken = default) =>
        message.Services().Messages.ReplyAsync(message, content, failIfNotExists, cancellationToken);

    public static Task<DiscordMessage> ReplyAsync(this DiscordMessage message, Action<MessageFactory> configure,
        bool failIfNotExists = false, CancellationToken cancellationToken = default) =>
        message.Services().Messages.ReplyAsync(message, configure, failIfNotExists, cancellationToken);

    public static Task<DiscordMessage> EditAsync(this DiscordMessage message, Action<MessageFactory> configure,
        CancellationToken cancellationToken = default) =>
        message.Services().Messages.EditAsync(message, configure, cancellationToken);

    public static Task<DiscordMessage> EditAsync(this DiscordMessage message, string content,
        CancellationToken cancellationToken = default) =>
        message.Services().Messages.EditAsync(message, edit => edit.WithContent(content), cancellationToken);

    public static Task<DiscordMessage> EditAsync(this DiscordMessage message, MessageEditRequest request,
        CancellationToken cancellationToken = default) =>
        message.Services().Messages.EditAsync(message.ChannelId, message.Id, request, cancellationToken);

    public static Task DeleteAsync(this DiscordMessage message, string? reason = null,
        CancellationToken cancellationToken = default) =>
        message.Services().Messages.DeleteAsync(message, reason, cancellationToken);

    public static Task<DiscordMessage> ForwardAsync(this DiscordMessage message, Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        message.Services().Messages.ForwardAsync(message, channelId, cancellationToken);

    public static Task<DiscordMessage> ForwardAsync(this DiscordMessage message, DiscordChannel channel,
        CancellationToken cancellationToken = default) =>
        message.Services().Messages.ForwardAsync(message, channel.Id, cancellationToken);

    public static Task ReactAsync(this DiscordMessage message, string emoji,
        CancellationToken cancellationToken = default) =>
        message.Services().Reactions.AddAsync(message, emoji, cancellationToken);

    public static Task ReactAsync(this DiscordMessage message, DiscordEmoji emoji,
        CancellationToken cancellationToken = default) =>
        message.Services().Reactions.AddAsync(message.ChannelId, message.Id, emoji, cancellationToken);

    public static Task ReactAsync(this DiscordMessage message, params string[] emojis) =>
        message.Services().Reactions.ApplyAsync(message, emojis);

    public static Task UnreactAsync(this DiscordMessage message, string emoji,
        CancellationToken cancellationToken = default) =>
        message.Services().Reactions.RemoveAsync(message, emoji, cancellationToken);

    public static Task UnreactAsync(this DiscordMessage message, DiscordEmoji emoji,
        CancellationToken cancellationToken = default) =>
        message.Services().Reactions.RemoveAsync(message.ChannelId, message.Id, emoji, cancellationToken);

    public static Task UnreactAsync(this DiscordMessage message, DiscordEmoji emoji, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        message.Rest().DeleteUserReactionAsync(message.ChannelId, message.Id, emoji, userId, cancellationToken);

    public static Task ClearReactionsAsync(this DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.Rest().DeleteAllReactionsAsync(message.ChannelId, message.Id, cancellationToken);

    public static Task ClearReactionsAsync(this DiscordMessage message, DiscordEmoji emoji,
        CancellationToken cancellationToken = default) =>
        message.Rest().DeleteEmojiReactionsAsync(message.ChannelId, message.Id, emoji, cancellationToken);

    public static Task<IReadOnlyList<DiscordUser>> GetReactorsAsync(this DiscordMessage message,
        DiscordEmoji emoji, ReactionQuery? query = null, CancellationToken cancellationToken = default) =>
        message.Rest().GetReactionsAsync(message.ChannelId, message.Id, emoji, query, cancellationToken);

    public static Task<IReadOnlyList<DiscordUser>> GetReactorsAsync(this DiscordMessage message, string emoji,
        ReactionQuery? query = null, CancellationToken cancellationToken = default) =>
        message.GetReactorsAsync(EmojiParser.Parse(emoji), query, cancellationToken);

    public static Task PinAsync(this DiscordMessage message, string? reason = null,
        CancellationToken cancellationToken = default) =>
        message.Rest().PinMessageAsync(message.ChannelId, message.Id, reason, cancellationToken);

    public static Task UnpinAsync(this DiscordMessage message, string? reason = null,
        CancellationToken cancellationToken = default) =>
        message.Rest().UnpinMessageAsync(message.ChannelId, message.Id, reason, cancellationToken);

    public static Task<DiscordMessage> CrosspostAsync(this DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.Rest().CrosspostMessageAsync(message.ChannelId, message.Id, cancellationToken);

    public static Task<DiscordChannel> StartThreadAsync(this DiscordMessage message, string name,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        message.Services().Threads.StartFromMessageAsync(message, name, configure, reason, cancellationToken);

    public static Task<DiscordMessage> RefreshAsync(this DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.Services().Messages.GetAsync(message.ChannelId, message.Id, cancellationToken);

    public static Task<DiscordChannel> GetChannelAsync(this DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.Rest().GetChannelAsync(message.ChannelId, cancellationToken);

    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.GuildId is { } guildId
            ? await message.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;

    public static async Task<DiscordMember?> GetAuthorMemberAsync(this DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.GuildId is { } guildId
            ? await message.Services().Members.GetAsync(guildId, message.Author.Id, cancellationToken)
            : null;
}

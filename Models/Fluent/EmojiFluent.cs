using Crovus.Factory;

namespace Crovus.Models;

public static class EmojiFluent
{
    public static Task<DiscordGuildEmoji> ModifyAsync(this DiscordGuildEmoji emoji, Snowflake guildId,
        EmojiModifyRequest request, string? reason = null, CancellationToken cancellationToken = default) =>
        emoji.Services().Emojis.ModifyAsync(guildId, emoji.Id, request, reason, cancellationToken);

    public static Task<DiscordGuildEmoji> ModifyAsync(this DiscordGuildEmoji emoji, Snowflake guildId,
        Action<EmojiFactory> configure, string? reason = null, CancellationToken cancellationToken = default) =>
        emoji.Services().Emojis.ModifyAsync(guildId, emoji.Id, configure, reason, cancellationToken);

    public static Task<DiscordGuildEmoji> RenameAsync(this DiscordGuildEmoji emoji, Snowflake guildId, string name,
        string? reason = null, CancellationToken cancellationToken = default) =>
        emoji.Services().Emojis.RenameAsync(guildId, emoji.Id, name, reason, cancellationToken);

    public static Task<DiscordGuildEmoji> RestrictAsync(this DiscordGuildEmoji emoji, Snowflake guildId,
        IEnumerable<Snowflake> roleIds, string? reason = null, CancellationToken cancellationToken = default) =>
        emoji.Services().Emojis.RestrictAsync(guildId, emoji.Id, roleIds, reason, cancellationToken);

    public static Task<DiscordGuildEmoji> UnrestrictAsync(this DiscordGuildEmoji emoji, Snowflake guildId,
        string? reason = null, CancellationToken cancellationToken = default) =>
        emoji.Services().Emojis.UnrestrictAsync(guildId, emoji.Id, reason, cancellationToken);

    public static Task DeleteAsync(this DiscordGuildEmoji emoji, Snowflake guildId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        emoji.Services().Emojis.DeleteAsync(guildId, emoji.Id, reason, cancellationToken);

    public static Task<DiscordGuildEmoji> RefreshAsync(this DiscordGuildEmoji emoji, Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        emoji.Services().Emojis.GetAsync(guildId, emoji.Id, cancellationToken);

    public static Task<DiscordGuild> GetGuildAsync(this DiscordGuildEmoji emoji, Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        emoji.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken);

    public static Task AddToAsync(this DiscordEmoji emoji, DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.ReactAsync(emoji, cancellationToken);

    public static Task AddToAsync(this DiscordGuildEmoji emoji, DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.ReactAsync(emoji.AsReaction(), cancellationToken);

    public static Task RemoveFromAsync(this DiscordEmoji emoji, DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.UnreactAsync(emoji, cancellationToken);

    public static Task RemoveFromAsync(this DiscordGuildEmoji emoji, DiscordMessage message,
        CancellationToken cancellationToken = default) =>
        message.UnreactAsync(emoji.AsReaction(), cancellationToken);

    public static Task<IReadOnlyList<DiscordUser>> GetReactorsAsync(this DiscordEmoji emoji,
        DiscordMessage message, ReactionQuery? query = null, CancellationToken cancellationToken = default) =>
        message.GetReactorsAsync(emoji, query, cancellationToken);
}

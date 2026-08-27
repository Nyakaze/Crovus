using Crovus.Factory;

namespace Crovus.Models;

public static class UserFluent
{
    public static Task<DiscordChannel> GetDirectChannelAsync(this DiscordUser user,
        CancellationToken cancellationToken = default) =>
        user.Rest().CreateDirectMessageChannelAsync(user.Id, cancellationToken);

    public static async Task<DiscordMessage> SendAsync(this DiscordUser user, string content,
        CancellationToken cancellationToken = default)
    {
        var channel = await user.GetDirectChannelAsync(cancellationToken);

        return await user.Services().Messages.SendAsync(channel.Id, content, cancellationToken);
    }

    public static async Task<DiscordMessage> SendAsync(this DiscordUser user, Action<MessageFactory> configure,
        CancellationToken cancellationToken = default)
    {
        var channel = await user.GetDirectChannelAsync(cancellationToken);

        return await user.Services().Messages.SendAsync(channel.Id, configure, cancellationToken);
    }

    public static async Task<DiscordMessage> SendAsync(this DiscordUser user, DiscordEmbed embed,
        string? content = null, CancellationToken cancellationToken = default)
    {
        var channel = await user.GetDirectChannelAsync(cancellationToken);

        return await user.Services().Messages.SendAsync(channel.Id,
            message => message.WithContent(content).AddEmbed(embed), cancellationToken);
    }

    public static Task<DiscordMember> AsMemberAsync(this DiscordUser user, Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        user.Services().Members.GetAsync(guildId, user.Id, cancellationToken);

    public static Task<DiscordMember> AsMemberAsync(this DiscordUser user, DiscordGuild guild,
        CancellationToken cancellationToken = default) =>
        user.Services().Members.GetAsync(guild.Id, user.Id, cancellationToken);

    public static Task<DiscordUser> RefreshAsync(this DiscordUser user,
        CancellationToken cancellationToken = default) =>
        user.Rest().GetUserAsync(user.Id, cancellationToken);

    public static DiscordPresence? GetPresence(this DiscordUser user) =>
        user.Context().Presences?.Get(user.Id);

    public static UserStatus GetStatus(this DiscordUser user) =>
        user.Context().Presences?.StatusOf(user.Id) ?? UserStatus.Offline;

    public static DiscordActivity? GetActivity(this DiscordUser user) =>
        user.Context().Presences?.ActivityOf(user.Id);

    public static DiscordActivity? GetActivity(this DiscordUser user, ActivityTypes types) =>
        user.Context().Presences?.ActivityOf(user.Id, types);

    public static IReadOnlyList<DiscordActivity> GetActivities(this DiscordUser user,
        ActivityTypes types = ActivityTypes.All) =>
        user.Context().Presences?.ActivitiesOf(user.Id, types) ?? [];

    public static bool IsOnline(this DiscordUser user) =>
        user.Context().Presences?.IsOnline(user.Id) ?? false;

    public static bool IsDoing(this DiscordUser user, ActivityTypes types) =>
        user.GetPresence() is { } presence && presence.Has(types);
}

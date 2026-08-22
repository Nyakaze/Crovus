namespace Crovus.Models;

public static class PresenceFluent
{
    public static Task<DiscordUser> GetUserAsync(this DiscordPresence presence,
        CancellationToken cancellationToken = default) =>
        presence.Rest().GetUserAsync(presence.UserId, cancellationToken);

    public static async Task<DiscordMember?> GetMemberAsync(this DiscordPresence presence,
        CancellationToken cancellationToken = default) =>
        presence.GuildId is { } guildId
            ? await presence.Services().Members.GetAsync(guildId, presence.UserId, cancellationToken)
            : null;

    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordPresence presence,
        CancellationToken cancellationToken = default) =>
        presence.GuildId is { } guildId
            ? await presence.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;
}

public static class InviteFluent
{
    public static Task DeleteAsync(this DiscordInvite invite, string? reason = null,
        CancellationToken cancellationToken = default) =>
        invite.Rest().DeleteInviteAsync(invite.Code, reason, cancellationToken);

    public static Task<DiscordInvite> RefreshAsync(this DiscordInvite invite, bool withCounts = false,
        CancellationToken cancellationToken = default) =>
        invite.Rest().GetInviteAsync(invite.Code, withCounts, cancellationToken);

    public static async Task<DiscordChannel?> GetChannelAsync(this DiscordInvite invite,
        CancellationToken cancellationToken = default) =>
        invite.ChannelId is { } channelId
            ? await invite.Rest().GetChannelAsync(channelId, cancellationToken)
            : null;

    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordInvite invite,
        CancellationToken cancellationToken = default) =>
        invite.GuildId is { } guildId
            ? await invite.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;
}

public static class BanFluent
{
    public static Task LiftAsync(this DiscordBan ban, Snowflake guildId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ban.Services().Members.UnbanAsync(guildId, ban.User.Id, reason, cancellationToken);

    public static Task<DiscordUser> GetUserAsync(this DiscordBan ban,
        CancellationToken cancellationToken = default) =>
        ban.Rest().GetUserAsync(ban.User.Id, cancellationToken);
}

public static class ThreadMemberFluent
{
    public static async Task<DiscordChannel?> GetThreadAsync(this DiscordThreadMember member,
        CancellationToken cancellationToken = default) =>
        member.ThreadId is { } threadId
            ? await member.Rest().GetChannelAsync(threadId, cancellationToken)
            : null;

    public static async Task<DiscordUser?> GetUserAsync(this DiscordThreadMember member,
        CancellationToken cancellationToken = default) =>
        member.UserId is { } userId
            ? await member.Rest().GetUserAsync(userId, cancellationToken)
            : null;

    public static async Task<DiscordMember?> GetGuildMemberAsync(this DiscordThreadMember member,
        CancellationToken cancellationToken = default) =>
        member is { GuildId: { } guildId, UserId: { } userId }
            ? await member.Services().Members.GetAsync(guildId, userId, cancellationToken)
            : null;

    public static Task RemoveAsync(this DiscordThreadMember member,
        CancellationToken cancellationToken = default)
    {
        if (member is not { ThreadId: { } threadId, UserId: { } userId })
            throw new InvalidOperationException(
                "This thread member carries neither a thread id nor a user id, so it cannot be removed.");

        return member.Rest().RemoveThreadMemberAsync(threadId, userId, cancellationToken);
    }
}

public static class VoiceStateFluent
{
    public static async Task<DiscordChannel?> GetChannelAsync(this DiscordVoiceState state,
        CancellationToken cancellationToken = default) =>
        state.ChannelId is { } channelId
            ? await state.Rest().GetChannelAsync(channelId, cancellationToken)
            : null;

    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordVoiceState state,
        CancellationToken cancellationToken = default) =>
        state.GuildId is { } guildId
            ? await state.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;

    public static async Task<DiscordMember?> GetMemberAsync(this DiscordVoiceState state,
        CancellationToken cancellationToken = default) =>
        state.GuildId is { } guildId
            ? await state.Services().Members.GetAsync(guildId, state.UserId, cancellationToken)
            : null;

    public static Task<DiscordUser> GetUserAsync(this DiscordVoiceState state,
        CancellationToken cancellationToken = default) =>
        state.Rest().GetUserAsync(state.UserId, cancellationToken);
}

public static class ScheduledEventFluent
{
    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordScheduledEvent scheduled,
        CancellationToken cancellationToken = default) =>
        scheduled.GuildId is { } guildId
            ? await scheduled.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;

    public static async Task<DiscordChannel?> GetChannelAsync(this DiscordScheduledEvent scheduled,
        CancellationToken cancellationToken = default) =>
        scheduled.ChannelId is { } channelId
            ? await scheduled.Rest().GetChannelAsync(channelId, cancellationToken)
            : null;

    public static async Task<DiscordUser?> GetCreatorAsync(this DiscordScheduledEvent scheduled,
        CancellationToken cancellationToken = default) =>
        scheduled.CreatorId is { } creatorId
            ? await scheduled.Rest().GetUserAsync(creatorId, cancellationToken)
            : scheduled.Creator;
}

public static class StageInstanceFluent
{
    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordStageInstance instance,
        CancellationToken cancellationToken = default) =>
        instance.GuildId is { } guildId
            ? await instance.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;

    public static Task<DiscordChannel> GetChannelAsync(this DiscordStageInstance instance,
        CancellationToken cancellationToken = default) =>
        instance.Rest().GetChannelAsync(instance.ChannelId, cancellationToken);
}

public static class IntegrationFluent
{
    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordIntegration integration,
        CancellationToken cancellationToken = default) =>
        integration.GuildId is { } guildId
            ? await integration.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;

    public static async Task<DiscordRole?> GetRoleAsync(this DiscordIntegration integration,
        CancellationToken cancellationToken = default) =>
        integration is { GuildId: { } guildId, RoleId: { } roleId }
            ? await integration.Services().Roles.GetAsync(guildId, roleId, cancellationToken)
            : null;

    public static async Task<DiscordUser?> GetUserAsync(this DiscordIntegration integration,
        CancellationToken cancellationToken = default) =>
        integration.User is { } user
            ? await integration.Rest().GetUserAsync(user.Id, cancellationToken)
            : null;
}

public static class AutoModerationFluent
{
    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordAutoModerationRule rule,
        CancellationToken cancellationToken = default) =>
        rule.GuildId is { } guildId
            ? await rule.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;

    public static async Task<DiscordUser?> GetCreatorAsync(this DiscordAutoModerationRule rule,
        CancellationToken cancellationToken = default) =>
        rule.CreatorId is { } creatorId
            ? await rule.Rest().GetUserAsync(creatorId, cancellationToken)
            : null;

    public static async Task<IReadOnlyList<DiscordRole>> GetExemptRolesAsync(this DiscordAutoModerationRule rule,
        CancellationToken cancellationToken = default)
    {
        if (rule.GuildId is not { } guildId || rule.ExemptRoles.Count == 0)
            return [];

        var roles = await rule.Services().Roles.GetAllAsync(guildId, cancellationToken);

        return [.. roles.Where(role => rule.ExemptRoles.Contains(role.Id))];
    }
}

public static class EntitlementFluent
{
    public static async Task<DiscordUser?> GetUserAsync(this DiscordEntitlement entitlement,
        CancellationToken cancellationToken = default) =>
        entitlement.UserId is { } userId
            ? await entitlement.Rest().GetUserAsync(userId, cancellationToken)
            : null;

    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordEntitlement entitlement,
        CancellationToken cancellationToken = default) =>
        entitlement.GuildId is { } guildId
            ? await entitlement.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;
}

public static class StickerFluent
{
    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordSticker sticker,
        CancellationToken cancellationToken = default) =>
        sticker.GuildId is { } guildId
            ? await sticker.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;

    public static async Task<DiscordUser?> GetAuthorAsync(this DiscordSticker sticker,
        CancellationToken cancellationToken = default) =>
        sticker.Author is { } author
            ? await sticker.Rest().GetUserAsync(author.Id, cancellationToken)
            : null;
}

public static class AuditLogEntryFluent
{
    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordAuditLogEntry entry,
        CancellationToken cancellationToken = default) =>
        entry.GuildId is { } guildId
            ? await entry.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;

    public static async Task<DiscordUser?> GetUserAsync(this DiscordAuditLogEntry entry,
        CancellationToken cancellationToken = default) =>
        entry.UserId is { } userId
            ? await entry.Rest().GetUserAsync(userId, cancellationToken)
            : null;
}

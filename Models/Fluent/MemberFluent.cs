using Crovus.Factory;

namespace Crovus.Models;

public static class MemberFluent
{
    public static Task<DiscordMember> ModifyAsync(this DiscordMember member, MemberModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        member.Services().Members.ModifyAsync(member.RequireGuildId(), member.User.Id, request, reason,
            cancellationToken);

    public static Task<DiscordMember> SetNicknameAsync(this DiscordMember member, string? nickname,
        string? reason = null, CancellationToken cancellationToken = default) =>
        member.Services().Members.SetNicknameAsync(member.RequireGuildId(), member.User.Id, nickname, reason,
            cancellationToken);

    public static Task AddRoleAsync(this DiscordMember member, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        member.Services().Members.GrantRoleAsync(member.RequireGuildId(), member.User.Id, roleId, reason,
            cancellationToken);

    public static Task AddRoleAsync(this DiscordMember member, DiscordRole role, string? reason = null,
        CancellationToken cancellationToken = default) =>
        member.AddRoleAsync(role.Id, reason, cancellationToken);

    public static Task RemoveRoleAsync(this DiscordMember member, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        member.Services().Members.RevokeRoleAsync(member.RequireGuildId(), member.User.Id, roleId, reason,
            cancellationToken);

    public static Task RemoveRoleAsync(this DiscordMember member, DiscordRole role, string? reason = null,
        CancellationToken cancellationToken = default) =>
        member.RemoveRoleAsync(role.Id, reason, cancellationToken);

    public static Task<DiscordMember> SetRolesAsync(this DiscordMember member, IReadOnlyList<Snowflake> roles,
        string? reason = null, CancellationToken cancellationToken = default) =>
        member.Services().Members.SetRolesAsync(member.RequireGuildId(), member.User.Id, roles, reason,
            cancellationToken);

    public static Task<DiscordMember> TimeoutAsync(this DiscordMember member, TimeSpan duration,
        string? reason = null, CancellationToken cancellationToken = default) =>
        member.Services().Members.TimeoutAsync(member.RequireGuildId(), member.User.Id, duration, reason,
            cancellationToken);

    public static Task<DiscordMember> ClearTimeoutAsync(this DiscordMember member, string? reason = null,
        CancellationToken cancellationToken = default) =>
        member.Services().Members.ClearTimeoutAsync(member.RequireGuildId(), member.User.Id, reason,
            cancellationToken);

    public static Task KickAsync(this DiscordMember member, string? reason = null,
        CancellationToken cancellationToken = default) =>
        member.Services().Members.KickAsync(member.RequireGuildId(), member.User.Id, reason, cancellationToken);

    public static Task BanAsync(this DiscordMember member, TimeSpan deleteMessageHistory = default,
        string? reason = null, CancellationToken cancellationToken = default) =>
        member.Services().Members.BanAsync(member.RequireGuildId(), member.User.Id, deleteMessageHistory, reason,
            cancellationToken);

    public static Task<DiscordMessage> SendAsync(this DiscordMember member, string content,
        CancellationToken cancellationToken = default) =>
        member.User.SendAsync(content, cancellationToken);

    public static Task<DiscordMessage> SendAsync(this DiscordMember member, Action<MessageFactory> configure,
        CancellationToken cancellationToken = default) =>
        member.User.SendAsync(configure, cancellationToken);

    public static Task<DiscordGuild> GetGuildAsync(this DiscordMember member,
        CancellationToken cancellationToken = default) =>
        member.Services().Guilds.GetAsync(member.RequireGuildId(), cancellationToken: cancellationToken);

    public static async Task<IReadOnlyList<DiscordRole>> GetRolesAsync(this DiscordMember member,
        CancellationToken cancellationToken = default)
    {
        var roles = await member.Services().Roles.GetAllAsync(member.RequireGuildId(), cancellationToken);

        return [.. roles.Where(role => member.Roles.Contains(role.Id))];
    }

    public static Task<DiscordMember> RefreshAsync(this DiscordMember member,
        CancellationToken cancellationToken = default) =>
        member.Services().Members.GetAsync(member.RequireGuildId(), member.User.Id, cancellationToken);

    public static Task<DiscordPermissions> GetPermissionsAsync(this DiscordMember member,
        CancellationToken cancellationToken = default) =>
        member.Services().Guilds.PermissionsOfAsync(member.RequireGuildId(), member.User.Id, cancellationToken);

    internal static Snowflake RequireGuildId(this DiscordMember member) =>
        member.GuildId ?? throw new InvalidOperationException(
            $"Member {member.User.Id} has no guild id, so this call cannot be routed. " +
            "Load the member through the client first, or use the guild-scoped service method.");
}

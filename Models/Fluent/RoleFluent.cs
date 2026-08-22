namespace Crovus.Models;

public static class RoleFluent
{
    public static Task<DiscordRole> ModifyAsync(this DiscordRole role, RoleModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        role.Services().Roles.ModifyAsync(role.RequireGuildId(), role.Id, request, reason, cancellationToken);

    public static Task<DiscordRole> RenameAsync(this DiscordRole role, string name, string? reason = null,
        CancellationToken cancellationToken = default) =>
        role.Services().Roles.RenameAsync(role.RequireGuildId(), role.Id, name, reason, cancellationToken);

    public static Task<DiscordRole> SetColorAsync(this DiscordRole role, int color, string? reason = null,
        CancellationToken cancellationToken = default) =>
        role.Services().Roles.SetColorAsync(role.RequireGuildId(), role.Id, color, reason, cancellationToken);

    public static Task<DiscordRole> SetPermissionsAsync(this DiscordRole role, DiscordPermissions permissions,
        string? reason = null, CancellationToken cancellationToken = default) =>
        role.Services().Roles.SetPermissionsAsync(role.RequireGuildId(), role.Id, permissions, reason,
            cancellationToken);

    public static Task DeleteAsync(this DiscordRole role, string? reason = null,
        CancellationToken cancellationToken = default) =>
        role.Services().Roles.DeleteAsync(role.RequireGuildId(), role.Id, reason, cancellationToken);

    public static Task GrantToAsync(this DiscordRole role, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        role.Services().Members.GrantRoleAsync(role.RequireGuildId(), userId, role.Id, reason, cancellationToken);

    public static Task GrantToAsync(this DiscordRole role, DiscordMember member, string? reason = null,
        CancellationToken cancellationToken = default) =>
        role.GrantToAsync(member.User.Id, reason, cancellationToken);

    public static Task RevokeFromAsync(this DiscordRole role, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        role.Services().Members.RevokeRoleAsync(role.RequireGuildId(), userId, role.Id, reason, cancellationToken);

    public static Task RevokeFromAsync(this DiscordRole role, DiscordMember member, string? reason = null,
        CancellationToken cancellationToken = default) =>
        role.RevokeFromAsync(member.User.Id, reason, cancellationToken);

    public static Task<DiscordGuild> GetGuildAsync(this DiscordRole role,
        CancellationToken cancellationToken = default) =>
        role.Services().Guilds.GetAsync(role.RequireGuildId(), cancellationToken: cancellationToken);

    public static async Task<DiscordRole?> RefreshAsync(this DiscordRole role,
        CancellationToken cancellationToken = default) =>
        await role.Services().Roles.GetAsync(role.RequireGuildId(), role.Id, cancellationToken);

    internal static Snowflake RequireGuildId(this DiscordRole role) =>
        role.GuildId ?? throw new InvalidOperationException(
            $"Role {role.Id} has no guild id, so this call cannot be routed. " +
            "Load the role through the client first, or use the guild-scoped service method.");
}

using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class RoleService : DiscordService
{
    public RoleService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Role", logger, telemetry)
    {
    }

    public RoleService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task<IReadOnlyList<DiscordRole>> GetAllAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetAllAsync), $"roles of guild {guildId}",
            () => Rest.GetGuildRolesAsync(guildId, cancellationToken),
            roles => $"Loaded {roles.Count} roles of guild {guildId}", LogLevel.Debug);

    public async Task<DiscordRole?> GetAsync(Snowflake guildId, Snowflake roleId,
        CancellationToken cancellationToken = default)
    {
        var roles = await GetAllAsync(guildId, cancellationToken);

        return roles.FirstOrDefault(role => role.Id == roleId);
    }

    public async Task<DiscordRole?> FindAsync(Snowflake guildId, string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var roles = await GetAllAsync(guildId, cancellationToken);

        return roles.FirstOrDefault(role => string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public Task<DiscordRole> CreateAsync(Snowflake guildId, RoleCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Limit.Required(request.Name, DiscordLimits.RoleName, nameof(request.Name));

        return TrackAsync(nameof(CreateAsync), $"role in guild {guildId}",
            () => Rest.CreateGuildRoleAsync(guildId, request, reason, cancellationToken),
            role => $"Created role {role.Name} ({role.Id}) in guild {guildId}{Because(reason)}");
    }

    public Task<DiscordRole> CreateAsync(Snowflake guildId, string name,
        DiscordPermissions permissions = DiscordPermissions.None, int color = 0, bool hoist = false,
        bool mentionable = false, string? reason = null, CancellationToken cancellationToken = default) =>
        CreateAsync(guildId,
            new RoleCreateRequest(name)
            {
                Permissions = permissions,
                Color = color,
                Hoist = hoist,
                Mentionable = mentionable
            }, reason, cancellationToken);

    public Task<DiscordRole> ModifyAsync(Snowflake guildId, Snowflake roleId, RoleModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsEmpty)
            Warn($"Modifying role {roleId} in guild {guildId} was requested without any change");

        if (request.Name is { } name)
            Limit.Required(name, DiscordLimits.RoleName, nameof(request.Name));

        return TrackAsync(nameof(ModifyAsync), $"role {roleId} in guild {guildId}",
            () => Rest.ModifyGuildRoleAsync(guildId, roleId, request, reason, cancellationToken),
            role => $"Modified role {role.Name} ({roleId}) in guild {guildId}{Because(reason)}");
    }

    public Task<DiscordRole> RenameAsync(Snowflake guildId, Snowflake roleId, string name, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(guildId, roleId, new RoleModifyRequest { Name = name }, reason, cancellationToken);

    public Task<DiscordRole> SetPermissionsAsync(Snowflake guildId, Snowflake roleId,
        DiscordPermissions permissions, string? reason = null, CancellationToken cancellationToken = default) =>
        ModifyAsync(guildId, roleId, new RoleModifyRequest { Permissions = permissions }, reason, cancellationToken);

    public Task<DiscordRole> SetColorAsync(Snowflake guildId, Snowflake roleId, int color, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(guildId, roleId, new RoleModifyRequest { Color = color }, reason, cancellationToken);

    public Task DeleteAsync(Snowflake guildId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteAsync), $"role {roleId} in guild {guildId}",
            () => Rest.DeleteGuildRoleAsync(guildId, roleId, reason, cancellationToken),
            $"Deleted role {roleId} in guild {guildId}{Because(reason)}", LogLevel.Warning);

    public async Task<DiscordRole> EnsureAsync(Snowflake guildId, RoleCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await FindAsync(guildId, request.Name, cancellationToken) is { } existing)
        {
            Emit(new RoleResolved(guildId, existing.Id, false));

            return existing;
        }

        var created = await CreateAsync(guildId, request, reason, cancellationToken);

        Emit(new RoleResolved(guildId, created.Id, true));

        return created;
    }

    public Task<DiscordRole> EnsureAsync(Snowflake guildId, string name,
        DiscordPermissions permissions = DiscordPermissions.None, string? reason = null,
        CancellationToken cancellationToken = default) =>
        EnsureAsync(guildId, new RoleCreateRequest(name) { Permissions = permissions }, reason, cancellationToken);
}

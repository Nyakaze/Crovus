using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class MemberService : DiscordService
{
    public MemberService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Member", logger, telemetry)
    {
    }

    public MemberService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task<DiscordMember> GetAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetAsync), Describe(guildId, userId),
            () => Rest.GetGuildMemberAsync(guildId, userId, cancellationToken),
            member => $"Loaded {member.DisplayName} ({member.User.Id}) of guild {guildId}", LogLevel.Debug);

    public Task<IReadOnlyList<DiscordMember>> GetPageAsync(Snowflake guildId, int limit = 100,
        Snowflake? after = null, CancellationToken cancellationToken = default)
    {
        Limit.Range(limit, 1, DiscordLimits.MembersPerPage, nameof(limit));

        return TrackAsync(nameof(GetPageAsync), $"members of guild {guildId}",
            () => Rest.GetGuildMembersAsync(guildId, new MemberQuery { Limit = limit, After = after },
                cancellationToken),
            members => $"Loaded {members.Count} members of guild {guildId}", LogLevel.Debug);
    }

    public async IAsyncEnumerable<DiscordMember> GetAllAsync(Snowflake guildId, int pageSize = 1000,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        Limit.Range(pageSize, 1, DiscordLimits.MembersPerPage, nameof(pageSize));

        Snowflake? after = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var page = await GetPageAsync(guildId, pageSize, after, cancellationToken);

            if (page.Count == 0)
                yield break;

            foreach (var member in page)
                yield return member;

            if (page.Count < pageSize)
                yield break;

            after = page[^1].User.Id;
        }
    }

    public Task<IReadOnlyList<DiscordMember>> SearchAsync(Snowflake guildId, string query, int limit = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        Limit.Range(limit, 1, DiscordLimits.MemberSearchLimit, nameof(limit));

        return TrackAsync(nameof(SearchAsync), $"member search in guild {guildId}",
            () => Rest.SearchGuildMembersAsync(guildId, query, limit, cancellationToken),
            members => $"Found {members.Count} members matching '{query}' in guild {guildId}", LogLevel.Debug);
    }

    public async Task<DiscordMember?> FindAsync(Snowflake guildId, string name,
        CancellationToken cancellationToken = default)
    {
        var matches = await SearchAsync(guildId, name, 10, cancellationToken);

        return matches.FirstOrDefault(member =>
                   string.Equals(member.DisplayName, name, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(member.User.Username, name, StringComparison.OrdinalIgnoreCase)) ??
               matches.FirstOrDefault();
    }

    public Task<DiscordMember> ModifyAsync(Snowflake guildId, Snowflake userId, MemberModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsEmpty)
            Warn($"Modifying {Describe(guildId, userId)} was requested without any change");

        if (request.Nickname is { } nickname)
            Limit.Text(nickname, DiscordLimits.Nickname, nameof(request.Nickname));

        return TrackAsync(nameof(ModifyAsync), Describe(guildId, userId),
            () => Rest.ModifyGuildMemberAsync(guildId, userId, request, reason, cancellationToken),
            member => $"Modified {member.DisplayName} ({userId}) of guild {guildId}{Because(reason)}");
    }

    public Task<DiscordMember> SetNicknameAsync(Snowflake guildId, Snowflake userId, string? nickname,
        string? reason = null, CancellationToken cancellationToken = default) =>
        ModifyAsync(guildId, userId, MemberModifyRequest.Rename(nickname), reason, cancellationToken);

    public Task<DiscordMember> SetRolesAsync(Snowflake guildId, Snowflake userId, IReadOnlyList<Snowflake> roles,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roles);

        return ModifyAsync(guildId, userId, new MemberModifyRequest { Roles = roles }, reason, cancellationToken);
    }

    public Task GrantRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GrantRoleAsync), Describe(guildId, userId),
            () => Rest.AddGuildMemberRoleAsync(guildId, userId, roleId, reason, cancellationToken),
            $"Granted role {roleId} to {Describe(guildId, userId)}{Because(reason)}");

    public Task RevokeRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(RevokeRoleAsync), Describe(guildId, userId),
            () => Rest.RemoveGuildMemberRoleAsync(guildId, userId, roleId, reason, cancellationToken),
            $"Revoked role {roleId} from {Describe(guildId, userId)}{Because(reason)}");

    public async Task<DiscordMember> TimeoutAsync(Snowflake guildId, Snowflake userId, TimeSpan duration,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "A timeout must be positive.");

        if (duration > DiscordMember.MaxTimeout)
            throw new ArgumentOutOfRangeException(nameof(duration), duration,
                $"Discord caps timeouts at {DiscordMember.MaxTimeout.TotalDays:F0} days.");

        var member = await ModifyAsync(guildId, userId,
            MemberModifyRequest.Timeout(DateTimeOffset.UtcNow + duration), reason, cancellationToken);

        Emit(new MemberTimedOut(guildId, userId, duration));

        return member;
    }

    public Task<DiscordMember> ClearTimeoutAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(guildId, userId, MemberModifyRequest.RemoveTimeout(), reason, cancellationToken);

    public Task KickAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(KickAsync), Describe(guildId, userId),
            () => Rest.RemoveGuildMemberAsync(guildId, userId, reason, cancellationToken),
            $"Kicked {Describe(guildId, userId)}{Because(reason)}", LogLevel.Warning);

    public Task BanAsync(Snowflake guildId, Snowflake userId, TimeSpan deleteMessageHistory = default,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        if (deleteMessageHistory < TimeSpan.Zero ||
            deleteMessageHistory.TotalSeconds > DiscordLimits.MaxBanDeleteSeconds)
            throw new ArgumentOutOfRangeException(nameof(deleteMessageHistory), deleteMessageHistory,
                "Discord deletes at most seven days of message history.");

        return TrackAsync(nameof(BanAsync), Describe(guildId, userId),
            () => Rest.CreateGuildBanAsync(guildId, userId, new BanCreateRequest(deleteMessageHistory), reason,
                cancellationToken),
            $"Banned {Describe(guildId, userId)}{Because(reason)}", LogLevel.Warning);
    }

    public Task UnbanAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(UnbanAsync), Describe(guildId, userId),
            () => Rest.RemoveGuildBanAsync(guildId, userId, reason, cancellationToken),
            $"Unbanned {Describe(guildId, userId)}{Because(reason)}");

    public Task<DiscordBan?> GetBanAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetBanAsync), Describe(guildId, userId),
            () => Rest.GetGuildBanAsync(guildId, userId, cancellationToken),
            ban => ban is null
                ? $"{Describe(guildId, userId)} is not banned"
                : $"Loaded the ban of {Describe(guildId, userId)}", LogLevel.Debug);

    public async Task<bool> IsBannedAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        await GetBanAsync(guildId, userId, cancellationToken) is not null;

    public Task<IReadOnlyList<DiscordBan>> GetBansAsync(Snowflake guildId, int limit = 1000, Snowflake? after = null,
        CancellationToken cancellationToken = default)
    {
        Limit.Range(limit, 1, DiscordLimits.BansPerPage, nameof(limit));

        return TrackAsync(nameof(GetBansAsync), $"bans of guild {guildId}",
            () => Rest.GetGuildBansAsync(guildId, new BanQuery { Limit = limit, After = after }, cancellationToken),
            bans => $"Loaded {bans.Count} bans of guild {guildId}", LogLevel.Debug);
    }

    private static string Describe(Snowflake guildId, Snowflake userId) => $"member {userId} of guild {guildId}";
}

namespace Crovus.Models;

public static class ThreadFluent
{
    public static Task JoinAsync(this DiscordChannel thread, CancellationToken cancellationToken = default) =>
        thread.Rest().JoinThreadAsync(thread.Id, cancellationToken);

    public static Task LeaveAsync(this DiscordChannel thread, CancellationToken cancellationToken = default) =>
        thread.Rest().LeaveThreadAsync(thread.Id, cancellationToken);

    public static Task<DiscordChannel> ArchiveAsync(this DiscordChannel thread, bool archived = true,
        string? reason = null, CancellationToken cancellationToken = default) =>
        thread.Services().Threads.ArchiveAsync(thread.Id, archived, reason, cancellationToken);

    public static Task<DiscordChannel> UnarchiveAsync(this DiscordChannel thread, string? reason = null,
        CancellationToken cancellationToken = default) =>
        thread.Services().Threads.UnarchiveAsync(thread.Id, reason, cancellationToken);

    public static Task<DiscordChannel> LockAsync(this DiscordChannel thread, bool locked = true,
        string? reason = null, CancellationToken cancellationToken = default) =>
        thread.Services().Threads.LockAsync(thread.Id, locked, reason, cancellationToken);

    public static Task<DiscordChannel> UnlockAsync(this DiscordChannel thread, string? reason = null,
        CancellationToken cancellationToken = default) =>
        thread.Services().Threads.UnlockAsync(thread.Id, reason, cancellationToken);

    public static Task<DiscordChannel> SetArchiveDurationAsync(this DiscordChannel thread,
        ThreadArchiveDuration duration, string? reason = null, CancellationToken cancellationToken = default) =>
        thread.Services().Threads.SetArchiveDurationAsync(thread.Id, duration, reason, cancellationToken);

    public static Task<DiscordChannel> SetTagsAsync(this DiscordChannel thread, IEnumerable<Snowflake> tagIds,
        string? reason = null, CancellationToken cancellationToken = default) =>
        thread.Services().Threads.SetTagsAsync(thread.Id, tagIds, reason, cancellationToken);

    public static Task AddMemberAsync(this DiscordChannel thread, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        thread.Rest().AddThreadMemberAsync(thread.Id, userId, cancellationToken);

    public static Task AddMemberAsync(this DiscordChannel thread, DiscordUser user,
        CancellationToken cancellationToken = default) =>
        thread.Rest().AddThreadMemberAsync(thread.Id, user.Id, cancellationToken);

    public static Task RemoveMemberAsync(this DiscordChannel thread, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        thread.Rest().RemoveThreadMemberAsync(thread.Id, userId, cancellationToken);

    public static Task RemoveMemberAsync(this DiscordChannel thread, DiscordUser user,
        CancellationToken cancellationToken = default) =>
        thread.Rest().RemoveThreadMemberAsync(thread.Id, user.Id, cancellationToken);

    public static Task<DiscordThreadMember> GetMemberAsync(this DiscordChannel thread, Snowflake userId,
        bool withMember = false, CancellationToken cancellationToken = default) =>
        thread.Rest().GetThreadMemberAsync(thread.Id, userId, withMember, cancellationToken);

    public static Task<IReadOnlyList<DiscordThreadMember>> GetMembersAsync(this DiscordChannel thread,
        bool withMember = false, CancellationToken cancellationToken = default) =>
        thread.Rest().GetThreadMembersAsync(thread.Id, withMember, cancellationToken);
}

using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class ThreadService : DiscordService
{
    public ThreadService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Thread", logger, telemetry)
    {
    }

    public ThreadService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task<DiscordChannel> StartAsync(Snowflake channelId, ThreadCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(StartAsync), $"channel {channelId}",
            () => Rest.StartThreadAsync(channelId, request, reason, cancellationToken),
            thread => $"Started {thread.Type} {thread.Name} ({thread.Id}) in channel {channelId}{Because(reason)}");
    }

    public Task<DiscordChannel> StartAsync(Snowflake channelId, ThreadFactory thread, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(thread);

        return StartAsync(channelId, thread.Build(), reason, cancellationToken);
    }

    public Task<DiscordChannel> StartPublicAsync(Snowflake channelId, string name,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        StartAsync(channelId, Compose(ThreadFactory.Public(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> StartPrivateAsync(Snowflake channelId, string name,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        StartAsync(channelId, Compose(ThreadFactory.Private(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> StartAnnouncementAsync(Snowflake channelId, string name,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        StartAsync(channelId, Compose(ThreadFactory.Announcement(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> CreatePostAsync(Snowflake channelId, string name, Action<MessageFactory> content,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var thread = ThreadFactory.ForumPost(name).WithStarterMessage(content);
        configure?.Invoke(thread);

        return StartAsync(channelId, thread.Build(), reason, cancellationToken);
    }

    public Task<DiscordChannel> CreatePostAsync(Snowflake channelId, string name, string content,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        CreatePostAsync(channelId, name, message => message.WithContent(content), configure, reason,
            cancellationToken);

    public Task<DiscordChannel> StartFromMessageAsync(Snowflake channelId, Snowflake messageId,
        ThreadFromMessageRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(StartFromMessageAsync), $"message {messageId} in channel {channelId}",
            () => Rest.StartThreadFromMessageAsync(channelId, messageId, request, reason, cancellationToken),
            thread => $"Started {thread.Name} ({thread.Id}) from message {messageId}{Because(reason)}");
    }

    public Task<DiscordChannel> StartFromMessageAsync(Snowflake channelId, Snowflake messageId, string name,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var thread = ThreadFactory.Public(name);
        configure?.Invoke(thread);

        return StartFromMessageAsync(channelId, messageId, thread.BuildFromMessage(), reason, cancellationToken);
    }

    public Task<DiscordChannel> StartFromMessageAsync(DiscordMessage message, string name,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return StartFromMessageAsync(message.ChannelId, message.Id, name, configure, reason, cancellationToken);
    }

    public Task<ThreadListing> GetActiveAsync(Snowflake guildId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetActiveAsync), $"guild {guildId}",
            () => Rest.GetActiveThreadsAsync(guildId, cancellationToken),
            listing => $"Loaded {listing.Count} active threads in guild {guildId}", LogLevel.Debug);

    public async Task<ThreadListing> GetActiveAsync(Snowflake guildId, Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        await TrackAsync(nameof(GetActiveAsync), $"channel {channelId} in guild {guildId}",
            async () => (await Rest.GetActiveThreadsAsync(guildId, cancellationToken)).Under(channelId),
            listing => $"Loaded {listing.Count} active threads in channel {channelId}", LogLevel.Debug);

    public Task<ThreadListing> GetPublicArchivedAsync(Snowflake channelId, ArchivedThreadQuery? query = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetPublicArchivedAsync), $"channel {channelId}",
            () => Rest.GetPublicArchivedThreadsAsync(channelId, query, cancellationToken),
            listing => $"Loaded {listing.Count} public archived threads in channel {channelId}" +
                       More(listing), LogLevel.Debug);

    public Task<ThreadListing> GetPrivateArchivedAsync(Snowflake channelId, ArchivedThreadQuery? query = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetPrivateArchivedAsync), $"channel {channelId}",
            () => Rest.GetPrivateArchivedThreadsAsync(channelId, query, cancellationToken),
            listing => $"Loaded {listing.Count} private archived threads in channel {channelId}" +
                       More(listing), LogLevel.Debug);

    public Task<ThreadListing> GetJoinedPrivateArchivedAsync(Snowflake channelId, ArchivedThreadQuery? query = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetJoinedPrivateArchivedAsync), $"channel {channelId}",
            () => Rest.GetJoinedPrivateArchivedThreadsAsync(channelId, query, cancellationToken),
            listing => $"Loaded {listing.Count} joined private archived threads in channel {channelId}" +
                       More(listing), LogLevel.Debug);

    public async Task<IReadOnlyList<DiscordChannel>> GetPostsAsync(Snowflake guildId, Snowflake forumId,
        bool includeArchived = true, int? archivedLimit = null, CancellationToken cancellationToken = default) =>
        await TrackAsync(nameof(GetPostsAsync), $"forum {forumId} in guild {guildId}",
            async () =>
            {
                var active = await Rest.GetActiveThreadsAsync(guildId, cancellationToken);
                var posts = active.Under(forumId).Threads.ToList();

                if (!includeArchived)
                    return (IReadOnlyList<DiscordChannel>)posts;

                var query = archivedLimit is null ? null : new ArchivedThreadQuery { Limit = archivedLimit };
                var archived = await Rest.GetPublicArchivedThreadsAsync(forumId, query, cancellationToken);
                var seen = posts.Select(post => post.Id).ToHashSet();

                posts.AddRange(archived.Threads.Where(post => seen.Add(post.Id)));

                return (IReadOnlyList<DiscordChannel>)posts;
            },
            posts => $"Loaded {posts.Count} posts from forum {forumId}", LogLevel.Debug);

    public async Task<DiscordChannel> ArchiveAsync(Snowflake threadId, bool archived = true, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var thread = await TrackAsync(nameof(ArchiveAsync), $"thread {threadId}",
            () => Rest.ModifyChannelAsync(threadId, new ChannelModifyRequest { Archived = archived }, reason,
                cancellationToken),
            updated => $"{(archived ? "Archived" : "Unarchived")} thread {updated.Name} ({updated.Id})" +
                       Because(reason));

        Emit(new ThreadArchiveToggled(threadId.Value, archived));

        return thread;
    }

    public Task<DiscordChannel> UnarchiveAsync(Snowflake threadId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ArchiveAsync(threadId, false, reason, cancellationToken);

    public async Task<DiscordChannel> LockAsync(Snowflake threadId, bool locked = true, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var thread = await TrackAsync(nameof(LockAsync), $"thread {threadId}",
            () => Rest.ModifyChannelAsync(threadId, new ChannelModifyRequest { Locked = locked }, reason,
                cancellationToken),
            updated => $"{(locked ? "Locked" : "Unlocked")} thread {updated.Name} ({updated.Id}){Because(reason)}");

        Emit(new ThreadLockToggled(threadId.Value, locked));

        return thread;
    }

    public Task<DiscordChannel> UnlockAsync(Snowflake threadId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        LockAsync(threadId, false, reason, cancellationToken);

    public Task<DiscordChannel> SetSlowmodeAsync(Snowflake threadId, TimeSpan slowmode, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var seconds = (int)slowmode.TotalSeconds;

        if (seconds is < 0 or > DiscordLimits.MaxSlowmodeSeconds)
            throw new ArgumentOutOfRangeException(nameof(slowmode),
                $"Slowmode must be between 0 and {DiscordLimits.MaxSlowmodeSeconds} seconds but was {seconds}.");

        return TrackAsync(nameof(SetSlowmodeAsync), $"thread {threadId}",
            () => Rest.ModifyChannelAsync(threadId, new ChannelModifyRequest { RateLimitPerUser = seconds }, reason,
                cancellationToken),
            thread => $"Set slowmode of thread {thread.Name} ({thread.Id}) to {seconds}s{Because(reason)}");
    }

    public Task<DiscordChannel> SetArchiveDurationAsync(Snowflake threadId, ThreadArchiveDuration duration,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(SetArchiveDurationAsync), $"thread {threadId}",
            () => Rest.ModifyChannelAsync(threadId, new ChannelModifyRequest { AutoArchiveDuration = duration },
                reason, cancellationToken),
            thread => $"Set auto archive of thread {thread.Name} ({thread.Id}) to {duration}{Because(reason)}");

    public Task<DiscordChannel> SetTagsAsync(Snowflake threadId, IEnumerable<Snowflake> tagIds, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tagIds);

        var tags = tagIds.Distinct().ToArray();

        if (tags.Length > DiscordLimits.ForumAppliedTags)
            throw new ArgumentException(
                $"A post carries at most {DiscordLimits.ForumAppliedTags} tags but {tags.Length} were given.",
                nameof(tagIds));

        return TrackAsync(nameof(SetTagsAsync), $"thread {threadId}",
            () => Rest.ModifyChannelAsync(threadId, new ChannelModifyRequest { AppliedTags = tags }, reason,
                cancellationToken),
            thread => $"Applied {tags.Length} tags to thread {thread.Name} ({thread.Id}){Because(reason)}");
    }

    public Task DeleteAsync(Snowflake threadId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteAsync), $"thread {threadId}",
            () => Rest.DeleteChannelAsync(threadId, reason, cancellationToken),
            $"Deleted thread {threadId}{Because(reason)}");

    private static string More(ThreadListing listing) => listing.HasMore ? " (more available)" : string.Empty;

    private static ThreadCreateRequest Compose(ThreadFactory factory, Action<ThreadFactory>? configure)
    {
        configure?.Invoke(factory);
        return factory.Build();
    }
}

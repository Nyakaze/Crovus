using Crovus.Client;

namespace Crovus.Models;

public enum ThreadArchiveDuration
{
    OneHour = 60,
    OneDay = 1440,
    ThreeDays = 4320,
    OneWeek = 10080
}

public sealed record ChannelCreateRequest(string Name, ChannelType Type = ChannelType.GuildText)
{
    public string? Topic { get; init; }
    public int? Position { get; init; }
    public bool? Nsfw { get; init; }
    public int? RateLimitPerUser { get; init; }
    public int? Bitrate { get; init; }
    public int? UserLimit { get; init; }
    public Snowflake? ParentId { get; init; }
    public ThreadArchiveDuration? DefaultAutoArchiveDuration { get; init; }
    public IReadOnlyList<DiscordPermissionOverwrite>? PermissionOverwrites { get; init; }
}

public sealed record ChannelModifyRequest
{
    public string? Name { get; init; }
    public ChannelType? Type { get; init; }
    public string? Topic { get; init; }
    public int? Position { get; init; }
    public bool? Nsfw { get; init; }
    public int? RateLimitPerUser { get; init; }
    public int? Bitrate { get; init; }
    public int? UserLimit { get; init; }
    public Snowflake? ParentId { get; init; }
    public ThreadArchiveDuration? DefaultAutoArchiveDuration { get; init; }
    public IReadOnlyList<DiscordPermissionOverwrite>? PermissionOverwrites { get; init; }
    public bool? Archived { get; init; }
    public bool? Locked { get; init; }
    public bool? Invitable { get; init; }
    public ThreadArchiveDuration? AutoArchiveDuration { get; init; }
    public IReadOnlyList<Snowflake>? AppliedTags { get; init; }

    public bool IsEmpty =>
        Name is null && Type is null && Topic is null && Position is null && Nsfw is null &&
        RateLimitPerUser is null && Bitrate is null && UserLimit is null && ParentId is null &&
        DefaultAutoArchiveDuration is null && PermissionOverwrites is null && Archived is null &&
        Locked is null && Invitable is null && AutoArchiveDuration is null && AppliedTags is null;
}

public sealed record ThreadCreateRequest(string Name)
{
    public ChannelType Type { get; init; } = ChannelType.PublicThread;
    public ThreadArchiveDuration? AutoArchiveDuration { get; init; }
    public bool? Invitable { get; init; }
    public int? RateLimitPerUser { get; init; }
    public MessageCreateRequest? Message { get; init; }
    public IReadOnlyList<Snowflake>? AppliedTags { get; init; }
}

public sealed record ThreadFromMessageRequest(string Name)
{
    public ThreadArchiveDuration? AutoArchiveDuration { get; init; }
    public int? RateLimitPerUser { get; init; }
}

public sealed record InviteCreateRequest
{
    public TimeSpan? MaxAge { get; init; }

    public int? MaxUses { get; init; }

    public bool Temporary { get; init; }

    public bool Unique { get; init; }

    public Snowflake? TargetUserId { get; init; }

    public static InviteCreateRequest Permanent() => new() { MaxAge = TimeSpan.Zero };

    public static InviteCreateRequest SingleUse(TimeSpan? maxAge = null) =>
        new() { MaxUses = 1, MaxAge = maxAge, Unique = true };

    public static InviteCreateRequest Expiring(TimeSpan maxAge, int? maxUses = null) =>
        new() { MaxAge = maxAge, MaxUses = maxUses };
}

public sealed record ArchivedThreadQuery
{
    public DateTimeOffset? Before { get; init; }

    public Snowflake? BeforeId { get; init; }

    public int? Limit { get; init; }
}

public sealed record ThreadListing(IReadOnlyList<DiscordChannel> Threads,
    IReadOnlyList<DiscordThreadMember> Members, bool HasMore) : IBoundEntity
{
    public static readonly ThreadListing Empty = new([], [], false);

    public int Count => Threads.Count;

    public bool IsEmpty => Threads.Count == 0;

    public DiscordThreadMember? MembershipOf(Snowflake threadId) =>
        Members.FirstOrDefault(member => member.ThreadId == threadId);

    public ThreadListing Under(Snowflake parentId)
    {
        var threads = Threads.Where(thread => thread.ParentId == parentId).ToArray();

        if (threads.Length == Threads.Count)
            return this;

        var ids = threads.Select(thread => thread.Id).ToHashSet();

        return this with
        {
            Threads = threads,
            Members = Members.Where(member => member.ThreadId is { } id && ids.Contains(id)).ToArray()
        };
    }

    private EntityBinding _binding;

    public ThreadListing Bind(ICrovusContext context)
    {
        var bound = this with {
            Threads = EntityBinder.BindAll(Threads, context),
            Members = EntityBinder.BindAll(Members, context)
        };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}

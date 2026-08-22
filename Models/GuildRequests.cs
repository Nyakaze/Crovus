using Crovus.Client;

namespace Crovus.Models;

public sealed record MemberModifyRequest
{
    public string? Nickname { get; init; }

    public IReadOnlyList<Snowflake>? Roles { get; init; }

    public bool? Mute { get; init; }

    public bool? Deaf { get; init; }

    public Snowflake? VoiceChannelId { get; init; }

    public DateTimeOffset? CommunicationDisabledUntil { get; init; }

    public bool ClearTimeout { get; init; }

    public bool IsEmpty =>
        Nickname is null && Roles is null && Mute is null && Deaf is null && VoiceChannelId is null &&
        CommunicationDisabledUntil is null && !ClearTimeout;

    public static MemberModifyRequest Timeout(DateTimeOffset until) => new() { CommunicationDisabledUntil = until };

    public static MemberModifyRequest RemoveTimeout() => new() { ClearTimeout = true };

    public static MemberModifyRequest Rename(string? nickname) => new() { Nickname = nickname ?? string.Empty };
}

public sealed record RoleCreateRequest(string Name)
{
    public DiscordPermissions Permissions { get; init; } = DiscordPermissions.None;

    public int Color { get; init; }

    public bool Hoist { get; init; }

    public bool Mentionable { get; init; }

    public string? UnicodeEmoji { get; init; }

    public string? IconData { get; init; }
}

public sealed record RoleModifyRequest
{
    public string? Name { get; init; }

    public DiscordPermissions? Permissions { get; init; }

    public int? Color { get; init; }

    public bool? Hoist { get; init; }

    public bool? Mentionable { get; init; }

    public string? UnicodeEmoji { get; init; }

    public string? IconData { get; init; }

    public bool IsEmpty =>
        Name is null && Permissions is null && Color is null && Hoist is null && Mentionable is null &&
        UnicodeEmoji is null && IconData is null;
}

public sealed record BanCreateRequest(TimeSpan DeleteMessageHistory = default)
{
    public int DeleteMessageSeconds => (int)DeleteMessageHistory.TotalSeconds;

    public static BanCreateRequest Purge(TimeSpan history) => new(history);
}

public sealed record MemberQuery
{
    public int? Limit { get; init; }

    public Snowflake? After { get; init; }
}

public sealed record BanQuery
{
    public int? Limit { get; init; }

    public Snowflake? Before { get; init; }

    public Snowflake? After { get; init; }
}

public sealed record GuildMembersRequest
{
    public required Snowflake GuildId { get; init; }

    public string? Query { get; init; }

    public int Limit { get; init; }

    public bool WithPresences { get; init; }

    public IReadOnlyList<Snowflake> UserIds { get; init; } = [];

    public string? Nonce { get; init; }

    public bool IsTargeted => UserIds.Count > 0;

    public static GuildMembersRequest All(Snowflake guildId, bool withPresences = false) =>
        new() { GuildId = guildId, Query = string.Empty, Limit = 0, WithPresences = withPresences };

    public static GuildMembersRequest Search(Snowflake guildId, string query, int limit = 100,
        bool withPresences = false) =>
        new() { GuildId = guildId, Query = query, Limit = limit, WithPresences = withPresences };

    public static GuildMembersRequest For(Snowflake guildId, params Snowflake[] userIds) =>
        new() { GuildId = guildId, UserIds = userIds };

    public static GuildMembersRequest For(Snowflake guildId, IEnumerable<Snowflake> userIds,
        bool withPresences = false) =>
        new() { GuildId = guildId, UserIds = userIds.ToArray(), WithPresences = withPresences };

    public GuildMembersRequest Tagged(string nonce) => this with { Nonce = nonce };
}

public sealed record GuildModifyRequest
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public Snowflake? OwnerId { get; init; }

    public Snowflake? AfkChannelId { get; init; }

    public int? AfkTimeout { get; init; }

    public Snowflake? SystemChannelId { get; init; }

    public Snowflake? RulesChannelId { get; init; }

    public Snowflake? PublicUpdatesChannelId { get; init; }

    public VerificationLevel? VerificationLevel { get; init; }

    public MessageNotificationLevel? DefaultMessageNotifications { get; init; }

    public ExplicitContentFilterLevel? ExplicitContentFilter { get; init; }

    public string? PreferredLocale { get; init; }

    public string? IconData { get; init; }

    public string? BannerData { get; init; }

    public string? SplashData { get; init; }

    public bool IsEmpty =>
        Name is null && Description is null && OwnerId is null && AfkChannelId is null && AfkTimeout is null &&
        SystemChannelId is null && RulesChannelId is null && PublicUpdatesChannelId is null &&
        VerificationLevel is null && DefaultMessageNotifications is null && ExplicitContentFilter is null &&
        PreferredLocale is null && IconData is null && BannerData is null && SplashData is null;
}

public sealed record PruneRequest(int Days = 7)
{
    public IReadOnlyList<Snowflake> IncludeRoles { get; init; } = [];

    public bool ReturnCount { get; init; } = true;

    public static PruneRequest Inactive(int days) => new(days);

    public static PruneRequest Silent(int days) => new(days) { ReturnCount = false };
}

public sealed record AuditLogQuery
{
    public Snowflake? UserId { get; init; }

    public AuditLogAction? Action { get; init; }

    public Snowflake? Before { get; init; }

    public Snowflake? After { get; init; }

    public int? Limit { get; init; }

    public static AuditLogQuery By(Snowflake userId, int? limit = null) => new() { UserId = userId, Limit = limit };

    public static AuditLogQuery Of(AuditLogAction action, int? limit = null) =>
        new() { Action = action, Limit = limit };
}

public sealed record DiscordAuditLog : IBoundEntity
{
    public IReadOnlyList<DiscordAuditLogEntry> Entries { get; init; } = [];

    public IReadOnlyList<DiscordUser> Users { get; init; } = [];

    public IReadOnlyList<DiscordWebhook> Webhooks { get; init; } = [];

    public IReadOnlyList<DiscordScheduledEvent> ScheduledEvents { get; init; } = [];

    public IReadOnlyList<DiscordAutoModerationRule> AutoModerationRules { get; init; } = [];

    public IReadOnlyList<DiscordIntegration> Integrations { get; init; } = [];

    public IReadOnlyList<DiscordChannel> Threads { get; init; } = [];

    public bool IsEmpty => Entries.Count == 0;

    public DiscordUser? UserOf(DiscordAuditLogEntry entry) =>
        entry.UserId is { } userId ? Users.FirstOrDefault(user => user.Id == userId) : null;

    private EntityBinding _binding;

    public DiscordAuditLog Bind(ICrovusContext context)
    {
        var bound = this with {
            Entries = EntityBinder.BindAll(Entries, context),
            Users = EntityBinder.BindAll(Users, context),
            Webhooks = EntityBinder.BindAll(Webhooks, context),
            ScheduledEvents = EntityBinder.BindAll(ScheduledEvents, context),
            AutoModerationRules = EntityBinder.BindAll(AutoModerationRules, context),
            Integrations = EntityBinder.BindAll(Integrations, context),
            Threads = EntityBinder.BindAll(Threads, context)
        };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}

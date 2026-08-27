using System.Text.Json;
using Crovus.Models;

namespace Crovus.Events;

public abstract record DiscordEvent
{
    public string Name { get; init; } = string.Empty;

    public int? Sequence { get; init; }

    public DateTimeOffset ReceivedAt { get; init; }
}

public sealed record ReadyEvent(DiscordUser User, Snowflake? ApplicationId, string SessionId, string? ResumeUrl)
    : DiscordEvent
{
    public IReadOnlyList<DiscordGuild> Guilds { get; init; } = [];

    public Snowflake UserId => User.Id;
}

public sealed record ResumedEvent : DiscordEvent;

public sealed record MessageCreatedEvent(DiscordMessage Message, DiscordChannel Channel, DiscordGuild? Guild)
    : DiscordEvent
{
    public DiscordMember? Member { get; init; }

    public DiscordUser Author => Message.Author;

    public Snowflake MessageId => Message.Id;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;

    public string Content => Message.Content;

    public bool IsDirect => Guild is null;
}

public sealed record MessageUpdatedEvent(DiscordMessage Message, DiscordChannel Channel, DiscordGuild? Guild)
    : DiscordEvent
{
    public DiscordMessage? Previous { get; init; }

    public DiscordUser? Author => Message.IsPartial ? null : Message.Author;

    public Snowflake MessageId => Message.Id;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;

    public bool IsPartial => Message.IsPartial;
}

public sealed record MessageDeletedEvent(DiscordMessage Message, DiscordChannel Channel, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake MessageId => Message.Id;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;

    public bool WasCached => !Message.IsPartial;
}

public sealed record MessagesBulkDeletedEvent(IReadOnlyList<DiscordMessage> Messages, DiscordChannel Channel,
    DiscordGuild? Guild) : DiscordEvent
{
    public int Count => Messages.Count;

    public IReadOnlyList<Snowflake> MessageIds => [.. Messages.Select(message => message.Id)];

    public IReadOnlyList<DiscordMessage> Cached => [.. Messages.Where(message => !message.IsPartial)];

    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ReactionAddedEvent(DiscordMessage Message, DiscordChannel Channel, DiscordUser User,
    DiscordGuild? Guild, DiscordEmoji Emoji) : DiscordEvent
{
    public DiscordMember? Member { get; init; }

    public Snowflake MessageId => Message.Id;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake UserId => User.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ReactionRemovedEvent(DiscordMessage Message, DiscordChannel Channel, DiscordUser User,
    DiscordGuild? Guild, DiscordEmoji Emoji) : DiscordEvent
{
    public DiscordMember? Member { get; init; }

    public Snowflake MessageId => Message.Id;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake UserId => User.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ReactionsClearedEvent(DiscordMessage Message, DiscordChannel Channel, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake MessageId => Message.Id;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ReactionEmojiClearedEvent(DiscordMessage Message, DiscordChannel Channel, DiscordGuild? Guild,
    DiscordEmoji Emoji) : DiscordEvent
{
    public Snowflake MessageId => Message.Id;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ChannelCreatedEvent(DiscordChannel Channel, DiscordGuild? Guild) : DiscordEvent
{
    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ChannelUpdatedEvent(DiscordChannel Channel, DiscordGuild? Guild) : DiscordEvent
{
    public DiscordChannel? Previous { get; init; }

    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ChannelDeletedEvent(DiscordChannel Channel, DiscordGuild? Guild) : DiscordEvent
{
    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ThreadCreatedEvent(DiscordChannel Thread, DiscordChannel? Parent, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake ThreadId => Thread.Id;

    public Snowflake? ParentId => Parent?.Id ?? Thread.ParentId;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ThreadUpdatedEvent(DiscordChannel Thread, DiscordChannel? Parent, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake ThreadId => Thread.Id;

    public Snowflake? ParentId => Parent?.Id ?? Thread.ParentId;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record ThreadDeletedEvent(DiscordChannel Thread, DiscordChannel? Parent, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake ThreadId => Thread.Id;

    public Snowflake? ParentId => Parent?.Id ?? Thread.ParentId;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record GuildAvailableEvent(DiscordGuild Guild, IReadOnlyList<DiscordChannel> Channels) : DiscordEvent
{
    public IReadOnlyList<DiscordPresence> Presences { get; init; } = [];

    public IReadOnlyList<DiscordVoiceState> VoiceStates { get; init; } = [];

    public IReadOnlyList<DiscordChannel> Threads { get; init; } = [];

    public IReadOnlyList<DiscordSticker> Stickers { get; init; } = [];

    public IReadOnlyList<DiscordScheduledEvent> ScheduledEvents { get; init; } = [];

    public IReadOnlyList<DiscordStageInstance> StageInstances { get; init; } = [];

    public IReadOnlyList<DiscordMember> Members { get; init; } = [];

    public Snowflake GuildId => Guild.Id;

    public string GuildName => Guild.Name;

    public IReadOnlyList<DiscordRole> Roles => Guild.Roles;

    public int? MemberCount => Guild.MemberCount;
}

public sealed record GuildUpdatedEvent(DiscordGuild Guild) : DiscordEvent
{
    public DiscordGuild? Previous { get; init; }

    public Snowflake GuildId => Guild.Id;
}

public sealed record GuildUnavailableEvent(DiscordGuild Guild, bool Removed) : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;
}

public sealed record MemberJoinedEvent(DiscordGuild Guild, DiscordMember Member) : DiscordEvent
{
    public DiscordUser User => Member.User;

    public Snowflake GuildId => Guild.Id;

    public Snowflake UserId => Member.User.Id;
}

public sealed record MemberUpdatedEvent(DiscordGuild Guild, DiscordMember Member) : DiscordEvent
{
    public DiscordMember? Previous { get; init; }

    public DiscordUser User => Member.User;

    public Snowflake GuildId => Guild.Id;

    public Snowflake UserId => Member.User.Id;
}

public sealed record MemberLeftEvent(DiscordGuild Guild, DiscordUser User) : DiscordEvent
{
    public DiscordMember? Member { get; init; }

    public Snowflake GuildId => Guild.Id;

    public Snowflake UserId => User.Id;
}

public sealed record RoleCreatedEvent(DiscordGuild Guild, DiscordRole Role) : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;

    public Snowflake RoleId => Role.Id;
}

public sealed record RoleUpdatedEvent(DiscordGuild Guild, DiscordRole Role) : DiscordEvent
{
    public DiscordRole? Previous { get; init; }

    public Snowflake GuildId => Guild.Id;

    public Snowflake RoleId => Role.Id;
}

public sealed record RoleDeletedEvent(DiscordGuild Guild, DiscordRole Role) : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;

    public Snowflake RoleId => Role.Id;

    public bool WasCached => !Role.IsPartial;
}

public sealed record BanAddedEvent(DiscordGuild Guild, DiscordUser User) : DiscordEvent
{
    public DiscordMember? Member { get; init; }

    public Snowflake GuildId => Guild.Id;

    public Snowflake UserId => User.Id;
}

public sealed record BanRemovedEvent(DiscordGuild Guild, DiscordUser User) : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;

    public Snowflake UserId => User.Id;
}

public sealed record WebhooksUpdatedEvent(DiscordChannel Channel, DiscordGuild? Guild) : DiscordEvent
{
    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record TypingStartedEvent(DiscordChannel Channel, DiscordUser User, DiscordGuild? Guild)
    : DiscordEvent
{
    public DiscordMember? Member { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public Snowflake ChannelId => Channel.Id;

    public Snowflake UserId => User.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record InteractionCreatedEvent(DiscordInteraction Interaction, DiscordChannel? Channel,
    DiscordGuild? Guild) : DiscordEvent
{
    public InteractionType InteractionType => Interaction.Type;

    public Snowflake? ChannelId => Channel?.Id ?? Interaction.ChannelId;

    public Snowflake? GuildId => Guild?.Id ?? Interaction.GuildId;

    public DiscordUser? Invoker => Interaction.Invoker;

    public string CommandPath => Interaction.CommandPath;

    public string CustomId => Interaction.CustomId;
}

public sealed record PresenceUpdatedEvent(DiscordPresence Presence, DiscordPresence? Previous) : DiscordEvent
{
    private readonly DiscordUser? _user;

    public DiscordGuild? Guild { get; init; }

    public DiscordUser User
    {
        get => _user ?? Presence.User ?? DiscordUser.Partial(Presence.UserId);
        init => _user = value;
    }

    public Snowflake UserId => Presence.UserId;

    public Snowflake? GuildId => Guild?.Id ?? Presence.GuildId;

    public UserStatus Status => Presence.Status;

    public UserStatus? PreviousStatus => Previous?.Status;

    public IReadOnlyList<DiscordActivity> Activities => Presence.Activities;

    public bool IsFirstSighting => Previous is null;

    public bool StatusChanged => Previous is null || Previous.Status != Presence.Status;

    public bool CameOnline => Presence.IsOnline && Previous is { IsOnline: false };

    public bool WentOffline => !Presence.IsOnline && Previous is { IsOnline: true };

    public bool ActivitiesChanged => StartedActivities.Count > 0 || StoppedActivities.Count > 0;

    public bool CustomStatusChanged => Previous?.CustomText != Presence.CustomText;

    public IReadOnlyList<DiscordActivity> StartedActivities => Difference(Presence, Previous);

    public IReadOnlyList<DiscordActivity> StoppedActivities => Difference(Previous, Presence);

    public ActivityTypes ActiveTypes => Presence.ActiveTypes;

    public ActivityTypes StartedTypes => StartedActivities.Types();

    public ActivityTypes StoppedTypes => StoppedActivities.Types();

    public ActivityTypes ChangedTypes => StartedTypes | StoppedTypes;

    public IReadOnlyList<DiscordActivity> Started(ActivityTypes types) => [.. StartedActivities.WithTypes(types)];

    public IReadOnlyList<DiscordActivity> Stopped(ActivityTypes types) => [.. StoppedActivities.WithTypes(types)];

    public IReadOnlyList<DiscordActivity> Current(ActivityTypes types) => Presence.ActivitiesOf(types);

    public bool Changed(ActivityTypes types) =>
        StartedActivities.HasAny(types) || StoppedActivities.HasAny(types);

    public bool Has(ActivityTypes types) => Presence.Has(types);

    private static IReadOnlyList<DiscordActivity> Difference(DiscordPresence? source, DiscordPresence? other)
    {
        if (source is null || source.Activities.Count == 0)
            return [];

        if (other is null)
            return source.Activities;

        return source.Activities
            .Where(activity => !other.Activities.Any(existing =>
                existing.Type == activity.Type &&
                string.Equals(existing.Name, activity.Name, StringComparison.Ordinal)))
            .ToArray();
    }
}

public sealed record VoiceStateUpdatedEvent(DiscordVoiceState VoiceState, DiscordUser User, DiscordGuild? Guild)
    : DiscordEvent
{
    public DiscordVoiceState? Previous { get; init; }

    public DiscordChannel? Channel { get; init; }

    public DiscordChannel? PreviousChannel { get; init; }

    public Snowflake UserId => VoiceState.UserId;

    public Snowflake? GuildId => Guild?.Id ?? VoiceState.GuildId;

    public Snowflake? ChannelId => Channel?.Id ?? VoiceState.ChannelId;

    public Snowflake? PreviousChannelId => PreviousChannel?.Id ?? Previous?.ChannelId;

    public DiscordMember? Member => VoiceState.Member;

    public bool Joined => VoiceState.ChannelId is not null && Previous?.ChannelId is null;

    public bool Left => VoiceState.ChannelId is null && Previous?.ChannelId is not null;

    public bool Moved => VoiceState.ChannelId is { } current && Previous?.ChannelId is { } before &&
                         current != before;

    public bool StartedStreaming => VoiceState.SelfStream && Previous is { SelfStream: false };

    public bool StoppedStreaming => !VoiceState.SelfStream && Previous is { SelfStream: true };

    public bool StartedVideo => VoiceState.SelfVideo && Previous is { SelfVideo: false };

    public bool MuteChanged => Previous is not null &&
                               (Previous.Mute != VoiceState.Mute || Previous.SelfMute != VoiceState.SelfMute);

    public bool DeafChanged => Previous is not null &&
                               (Previous.Deaf != VoiceState.Deaf || Previous.SelfDeaf != VoiceState.SelfDeaf);
}

public sealed record VoiceServerUpdatedEvent(DiscordGuild Guild, string Token, string? Endpoint) : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;

    public bool IsAwaitingEndpoint => Endpoint is null;
}

public sealed record GuildMembersChunkEvent(DiscordGuild Guild, IReadOnlyList<DiscordMember> Members,
    int ChunkIndex, int ChunkCount) : DiscordEvent
{
    public IReadOnlyList<DiscordUser> NotFound { get; init; } = [];

    public IReadOnlyList<DiscordPresence> Presences { get; init; } = [];

    public string? Nonce { get; init; }

    public Snowflake GuildId => Guild.Id;

    public bool IsLastChunk => ChunkIndex + 1 >= ChunkCount;

    public int Count => Members.Count;
}

public sealed record ThreadListSyncEvent(DiscordGuild Guild, IReadOnlyList<DiscordChannel> Threads,
    IReadOnlyList<DiscordThreadMember> Members, IReadOnlyList<DiscordChannel> Channels) : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;

    public bool IsWholeGuild => Channels.Count == 0;
}

public sealed record ThreadMemberUpdatedEvent(DiscordThreadMember Member, DiscordChannel? Thread,
    DiscordGuild? Guild) : DiscordEvent
{
    public DiscordUser? User => Member.User;

    public Snowflake? ThreadId => Thread?.Id ?? Member.ThreadId;

    public Snowflake? GuildId => Guild?.Id ?? Member.GuildId;
}

public sealed record ThreadMembersUpdatedEvent(DiscordChannel Thread, DiscordGuild Guild, int MemberCount,
    IReadOnlyList<DiscordThreadMember> Added, IReadOnlyList<DiscordUser> Removed) : DiscordEvent
{
    public Snowflake ThreadId => Thread.Id;

    public Snowflake GuildId => Guild.Id;

    public bool HasJoins => Added.Count > 0;

    public bool HasLeaves => Removed.Count > 0;
}

public sealed record ChannelPinsUpdatedEvent(DiscordChannel Channel, DiscordGuild? Guild, DateTimeOffset? LastPinAt)
    : DiscordEvent
{
    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;

    public bool IsEmpty => LastPinAt is null;
}

public sealed record InviteCreatedEvent(DiscordInvite Invite, DiscordChannel Channel, DiscordGuild? Guild)
    : DiscordEvent
{
    public string Code => Invite.Code;

    public string Url => Invite.Url;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;

    public DiscordUser? Inviter => Invite.Inviter;
}

public sealed record InviteDeletedEvent(DiscordInvite Invite, DiscordChannel Channel, DiscordGuild? Guild)
    : DiscordEvent
{
    public string Code => Invite.Code;

    public string Url => Invite.Url;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record UserUpdatedEvent(DiscordUser User) : DiscordEvent
{
    public DiscordUser? Previous { get; init; }

    public Snowflake UserId => User.Id;
}

public sealed record GuildEmojisUpdatedEvent(DiscordGuild Guild, IReadOnlyList<DiscordGuildEmoji> Emojis)
    : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;

    public int Count => Emojis.Count;
}

public sealed record GuildStickersUpdatedEvent(DiscordGuild Guild, IReadOnlyList<DiscordSticker> Stickers)
    : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;

    public int Count => Stickers.Count;
}

public sealed record AuditLogEntryCreatedEvent(DiscordAuditLogEntry Entry, DiscordGuild? Guild, DiscordUser? User)
    : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? Entry.GuildId;

    public AuditLogAction Action => Entry.Action;

    public Snowflake? UserId => User?.Id ?? Entry.UserId;

    public Snowflake? TargetId => Entry.TargetId;

    public string? Reason => Entry.Reason;
}

public sealed record PollVoteAddedEvent(DiscordUser User, DiscordChannel Channel, DiscordMessage Message,
    DiscordGuild? Guild, int AnswerId) : DiscordEvent
{
    public Snowflake UserId => User.Id;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake MessageId => Message.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record PollVoteRemovedEvent(DiscordUser User, DiscordChannel Channel, DiscordMessage Message,
    DiscordGuild? Guild, int AnswerId) : DiscordEvent
{
    public Snowflake UserId => User.Id;

    public Snowflake ChannelId => Channel.Id;

    public Snowflake MessageId => Message.Id;

    public Snowflake? GuildId => Guild?.Id;
}

public sealed record AutoModerationRuleCreatedEvent(DiscordAutoModerationRule Rule, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? Rule.GuildId;

    public Snowflake RuleId => Rule.Id;
}

public sealed record AutoModerationRuleUpdatedEvent(DiscordAutoModerationRule Rule, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? Rule.GuildId;

    public Snowflake RuleId => Rule.Id;
}

public sealed record AutoModerationRuleDeletedEvent(DiscordAutoModerationRule Rule, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? Rule.GuildId;

    public Snowflake RuleId => Rule.Id;
}

public sealed record AutoModerationActionExecutedEvent(DiscordGuild Guild, DiscordAutoModerationRule Rule,
    AutoModerationAction Action, DiscordUser User) : DiscordEvent
{
    public AutoModerationTriggerType TriggerType { get; init; }

    public DiscordChannel? Channel { get; init; }

    public DiscordMessage? Message { get; init; }

    public DiscordMessage? AlertMessage { get; init; }

    public DiscordMember? Member { get; init; }

    public string? Content { get; init; }

    public string? MatchedKeyword { get; init; }

    public string? MatchedContent { get; init; }

    public Snowflake GuildId => Guild.Id;

    public Snowflake RuleId => Rule.Id;

    public Snowflake UserId => User.Id;

    public Snowflake? ChannelId => Channel?.Id;

    public Snowflake? MessageId => Message?.Id;

    public Snowflake? AlertMessageId => AlertMessage?.Id;

    public bool WasBlocked => Action.Type is AutoModerationActionType.BlockMessage;

    public bool WasTimedOut => Action.Type is AutoModerationActionType.Timeout;
}

public sealed record ScheduledEventCreatedEvent(DiscordScheduledEvent ScheduledEvent, DiscordGuild? Guild,
    DiscordChannel? Channel) : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? ScheduledEvent.GuildId;

    public Snowflake? ChannelId => Channel?.Id ?? ScheduledEvent.ChannelId;
}

public sealed record ScheduledEventUpdatedEvent(DiscordScheduledEvent ScheduledEvent, DiscordGuild? Guild,
    DiscordChannel? Channel) : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? ScheduledEvent.GuildId;

    public Snowflake? ChannelId => Channel?.Id ?? ScheduledEvent.ChannelId;

    public ScheduledEventStatus Status => ScheduledEvent.Status;
}

public sealed record ScheduledEventDeletedEvent(DiscordScheduledEvent ScheduledEvent, DiscordGuild? Guild,
    DiscordChannel? Channel) : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? ScheduledEvent.GuildId;

    public Snowflake? ChannelId => Channel?.Id ?? ScheduledEvent.ChannelId;
}

public sealed record ScheduledEventUserAddedEvent(DiscordScheduledEvent ScheduledEvent, DiscordUser User,
    DiscordGuild Guild) : DiscordEvent
{
    public DiscordMember? Member { get; init; }

    public Snowflake ScheduledEventId => ScheduledEvent.Id;

    public Snowflake UserId => User.Id;

    public Snowflake GuildId => Guild.Id;
}

public sealed record ScheduledEventUserRemovedEvent(DiscordScheduledEvent ScheduledEvent, DiscordUser User,
    DiscordGuild Guild) : DiscordEvent
{
    public DiscordMember? Member { get; init; }

    public Snowflake ScheduledEventId => ScheduledEvent.Id;

    public Snowflake UserId => User.Id;

    public Snowflake GuildId => Guild.Id;
}

public sealed record StageInstanceCreatedEvent(DiscordStageInstance Instance, DiscordChannel Channel,
    DiscordGuild? Guild) : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? Instance.GuildId;

    public Snowflake ChannelId => Channel.Id;
}

public sealed record StageInstanceUpdatedEvent(DiscordStageInstance Instance, DiscordChannel Channel,
    DiscordGuild? Guild) : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? Instance.GuildId;

    public Snowflake ChannelId => Channel.Id;
}

public sealed record StageInstanceDeletedEvent(DiscordStageInstance Instance, DiscordChannel Channel,
    DiscordGuild? Guild) : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? Instance.GuildId;

    public Snowflake ChannelId => Channel.Id;
}

public sealed record IntegrationCreatedEvent(DiscordIntegration Integration, DiscordGuild? Guild) : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? Integration.GuildId;

    public Snowflake IntegrationId => Integration.Id;
}

public sealed record IntegrationUpdatedEvent(DiscordIntegration Integration, DiscordGuild? Guild) : DiscordEvent
{
    public Snowflake? GuildId => Guild?.Id ?? Integration.GuildId;

    public Snowflake IntegrationId => Integration.Id;
}

public sealed record IntegrationDeletedEvent(DiscordIntegration Integration, DiscordGuild Guild,
    Snowflake? ApplicationId) : DiscordEvent
{
    public Snowflake IntegrationId => Integration.Id;

    public Snowflake GuildId => Guild.Id;
}

public sealed record GuildIntegrationsUpdatedEvent(DiscordGuild Guild) : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;
}

public sealed record EntitlementCreatedEvent(DiscordEntitlement Entitlement, DiscordUser? User, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake? UserId => User?.Id ?? Entitlement.UserId;

    public Snowflake? GuildId => Guild?.Id ?? Entitlement.GuildId;

    public Snowflake SkuId => Entitlement.SkuId;
}

public sealed record EntitlementUpdatedEvent(DiscordEntitlement Entitlement, DiscordUser? User, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake? UserId => User?.Id ?? Entitlement.UserId;

    public Snowflake? GuildId => Guild?.Id ?? Entitlement.GuildId;

    public Snowflake SkuId => Entitlement.SkuId;

    public bool IsRenewal => !Entitlement.Deleted;
}

public sealed record EntitlementDeletedEvent(DiscordEntitlement Entitlement, DiscordUser? User, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake? UserId => User?.Id ?? Entitlement.UserId;

    public Snowflake? GuildId => Guild?.Id ?? Entitlement.GuildId;

    public Snowflake SkuId => Entitlement.SkuId;
}

public sealed record CommandPermissionsUpdatedEvent(DiscordCommandPermissions Permissions, DiscordGuild? Guild)
    : DiscordEvent
{
    public Snowflake CommandId => Permissions.CommandId;

    public Snowflake GuildId => Guild?.Id ?? Permissions.GuildId;

    public bool IsApplicationWide => Permissions.IsApplicationWide;
}

public sealed record UnknownEvent(JsonElement Data) : DiscordEvent;

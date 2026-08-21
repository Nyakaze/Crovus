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
    : DiscordEvent;

public sealed record ResumedEvent : DiscordEvent;

public sealed record MessageCreatedEvent(DiscordMessage Message) : DiscordEvent
{
    public Snowflake ChannelId => Message.ChannelId;

    public Snowflake MessageId => Message.Id;
}

public sealed record MessageUpdatedEvent(Snowflake MessageId, Snowflake ChannelId, Snowflake? GuildId,
    DiscordMessage? Message) : DiscordEvent
{
    public bool IsPartial => Message is null;
}

public sealed record MessageDeletedEvent(Snowflake MessageId, Snowflake ChannelId, Snowflake? GuildId)
    : DiscordEvent;

public sealed record MessagesBulkDeletedEvent(IReadOnlyList<Snowflake> MessageIds, Snowflake ChannelId,
    Snowflake? GuildId) : DiscordEvent
{
    public int Count => MessageIds.Count;
}

public sealed record ReactionAddedEvent(Snowflake MessageId, Snowflake ChannelId, Snowflake UserId,
    Snowflake? GuildId, DiscordEmoji Emoji) : DiscordEvent;

public sealed record ReactionRemovedEvent(Snowflake MessageId, Snowflake ChannelId, Snowflake UserId,
    Snowflake? GuildId, DiscordEmoji Emoji) : DiscordEvent;

public sealed record ReactionsClearedEvent(Snowflake MessageId, Snowflake ChannelId, Snowflake? GuildId)
    : DiscordEvent;

public sealed record ReactionEmojiClearedEvent(Snowflake MessageId, Snowflake ChannelId, Snowflake? GuildId,
    DiscordEmoji Emoji) : DiscordEvent;

public sealed record ChannelCreatedEvent(DiscordChannel Channel) : DiscordEvent;

public sealed record ChannelUpdatedEvent(DiscordChannel Channel) : DiscordEvent;

public sealed record ChannelDeletedEvent(Snowflake ChannelId, Snowflake? GuildId, DiscordChannel? Channel)
    : DiscordEvent;

public sealed record ThreadCreatedEvent(DiscordChannel Thread) : DiscordEvent;

public sealed record ThreadUpdatedEvent(DiscordChannel Thread) : DiscordEvent;

public sealed record ThreadDeletedEvent(Snowflake ThreadId, Snowflake? ParentId, Snowflake? GuildId) : DiscordEvent;

public sealed record GuildAvailableEvent(Snowflake GuildId, IReadOnlyList<DiscordChannel> Channels,
    DiscordGuild? Guild = null) : DiscordEvent
{
    public IReadOnlyList<DiscordPresence> Presences { get; init; } = [];

    public IReadOnlyList<DiscordVoiceState> VoiceStates { get; init; } = [];

    public IReadOnlyList<DiscordChannel> Threads { get; init; } = [];

    public IReadOnlyList<DiscordSticker> Stickers { get; init; } = [];

    public IReadOnlyList<DiscordScheduledEvent> ScheduledEvents { get; init; } = [];

    public IReadOnlyList<DiscordStageInstance> StageInstances { get; init; } = [];

    public string GuildName => Guild?.Name ?? string.Empty;

    public IReadOnlyList<DiscordRole> Roles => Guild?.Roles ?? [];

    public int? MemberCount => Guild?.MemberCount;
}

public sealed record GuildUpdatedEvent(DiscordGuild Guild) : DiscordEvent
{
    public Snowflake GuildId => Guild.Id;
}

public sealed record GuildUnavailableEvent(Snowflake GuildId, bool Removed) : DiscordEvent;

public sealed record MemberJoinedEvent(Snowflake GuildId, DiscordMember Member) : DiscordEvent
{
    public DiscordUser User => Member.User;
}

public sealed record MemberUpdatedEvent(Snowflake GuildId, DiscordMember Member) : DiscordEvent
{
    public DiscordUser User => Member.User;
}

public sealed record MemberLeftEvent(Snowflake GuildId, DiscordUser User) : DiscordEvent;

public sealed record RoleCreatedEvent(Snowflake GuildId, DiscordRole Role) : DiscordEvent;

public sealed record RoleUpdatedEvent(Snowflake GuildId, DiscordRole Role) : DiscordEvent;

public sealed record RoleDeletedEvent(Snowflake GuildId, Snowflake RoleId) : DiscordEvent;

public sealed record BanAddedEvent(Snowflake GuildId, DiscordUser User) : DiscordEvent;

public sealed record BanRemovedEvent(Snowflake GuildId, DiscordUser User) : DiscordEvent;

public sealed record WebhooksUpdatedEvent(Snowflake ChannelId, Snowflake? GuildId) : DiscordEvent;

public sealed record TypingStartedEvent(Snowflake ChannelId, Snowflake UserId, Snowflake? GuildId) : DiscordEvent;

public sealed record InteractionCreatedEvent(DiscordInteraction Interaction) : DiscordEvent
{
    public InteractionType InteractionType => Interaction.Type;

    public Snowflake? ChannelId => Interaction.ChannelId;

    public Snowflake? GuildId => Interaction.GuildId;

    public DiscordUser? Invoker => Interaction.Invoker;

    public string CommandPath => Interaction.CommandPath;

    public string CustomId => Interaction.CustomId;
}

public sealed record PresenceUpdatedEvent(DiscordPresence Presence, DiscordPresence? Previous) : DiscordEvent
{
    public Snowflake UserId => Presence.UserId;

    public Snowflake? GuildId => Presence.GuildId;

    public DiscordUser? User => Presence.User;

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

public sealed record VoiceStateUpdatedEvent(DiscordVoiceState VoiceState) : DiscordEvent
{
    public DiscordVoiceState? Previous { get; init; }

    public Snowflake UserId => VoiceState.UserId;

    public Snowflake? GuildId => VoiceState.GuildId;

    public Snowflake? ChannelId => VoiceState.ChannelId;

    public Snowflake? PreviousChannelId => Previous?.ChannelId;

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

public sealed record VoiceServerUpdatedEvent(Snowflake GuildId, string Token, string? Endpoint) : DiscordEvent
{
    public bool IsAwaitingEndpoint => Endpoint is null;
}

public sealed record GuildMembersChunkEvent(Snowflake GuildId, IReadOnlyList<DiscordMember> Members,
    int ChunkIndex, int ChunkCount) : DiscordEvent
{
    public IReadOnlyList<Snowflake> NotFound { get; init; } = [];

    public IReadOnlyList<DiscordPresence> Presences { get; init; } = [];

    public string? Nonce { get; init; }

    public bool IsLastChunk => ChunkIndex + 1 >= ChunkCount;

    public int Count => Members.Count;
}

public sealed record ThreadListSyncEvent(Snowflake GuildId, IReadOnlyList<DiscordChannel> Threads,
    IReadOnlyList<DiscordThreadMember> Members, IReadOnlyList<Snowflake> ChannelIds) : DiscordEvent
{
    public bool IsWholeGuild => ChannelIds.Count == 0;
}

public sealed record ThreadMemberUpdatedEvent(DiscordThreadMember Member) : DiscordEvent
{
    public Snowflake? ThreadId => Member.ThreadId;

    public Snowflake? GuildId => Member.GuildId;
}

public sealed record ThreadMembersUpdatedEvent(Snowflake ThreadId, Snowflake GuildId, int MemberCount,
    IReadOnlyList<DiscordThreadMember> Added, IReadOnlyList<Snowflake> Removed) : DiscordEvent
{
    public bool HasJoins => Added.Count > 0;

    public bool HasLeaves => Removed.Count > 0;
}

public sealed record ChannelPinsUpdatedEvent(Snowflake ChannelId, Snowflake? GuildId, DateTimeOffset? LastPinAt)
    : DiscordEvent
{
    public bool IsEmpty => LastPinAt is null;
}

public sealed record InviteCreatedEvent(DiscordInvite Invite) : DiscordEvent
{
    public string Code => Invite.Code;

    public Snowflake? GuildId => Invite.GuildId;

    public Snowflake? ChannelId => Invite.ChannelId;

    public DiscordUser? Inviter => Invite.Inviter;
}

public sealed record InviteDeletedEvent(string Code, Snowflake ChannelId, Snowflake? GuildId) : DiscordEvent
{
    public string Url => $"https://discord.gg/{Code}";
}

public sealed record UserUpdatedEvent(DiscordUser User) : DiscordEvent
{
    public Snowflake UserId => User.Id;
}

public sealed record GuildEmojisUpdatedEvent(Snowflake GuildId, IReadOnlyList<DiscordGuildEmoji> Emojis)
    : DiscordEvent
{
    public int Count => Emojis.Count;
}

public sealed record GuildStickersUpdatedEvent(Snowflake GuildId, IReadOnlyList<DiscordSticker> Stickers)
    : DiscordEvent
{
    public int Count => Stickers.Count;
}

public sealed record AuditLogEntryCreatedEvent(DiscordAuditLogEntry Entry) : DiscordEvent
{
    public Snowflake? GuildId => Entry.GuildId;

    public AuditLogAction Action => Entry.Action;

    public Snowflake? UserId => Entry.UserId;

    public Snowflake? TargetId => Entry.TargetId;

    public string? Reason => Entry.Reason;
}

public sealed record PollVoteAddedEvent(Snowflake UserId, Snowflake ChannelId, Snowflake MessageId,
    Snowflake? GuildId, int AnswerId) : DiscordEvent;

public sealed record PollVoteRemovedEvent(Snowflake UserId, Snowflake ChannelId, Snowflake MessageId,
    Snowflake? GuildId, int AnswerId) : DiscordEvent;

public sealed record AutoModerationRuleCreatedEvent(DiscordAutoModerationRule Rule) : DiscordEvent
{
    public Snowflake? GuildId => Rule.GuildId;
}

public sealed record AutoModerationRuleUpdatedEvent(DiscordAutoModerationRule Rule) : DiscordEvent
{
    public Snowflake? GuildId => Rule.GuildId;
}

public sealed record AutoModerationRuleDeletedEvent(DiscordAutoModerationRule Rule) : DiscordEvent
{
    public Snowflake? GuildId => Rule.GuildId;
}

public sealed record AutoModerationActionExecutedEvent(Snowflake GuildId, Snowflake RuleId,
    AutoModerationAction Action, Snowflake UserId) : DiscordEvent
{
    public AutoModerationTriggerType TriggerType { get; init; }

    public Snowflake? ChannelId { get; init; }

    public Snowflake? MessageId { get; init; }

    public Snowflake? AlertMessageId { get; init; }

    public string? Content { get; init; }

    public string? MatchedKeyword { get; init; }

    public string? MatchedContent { get; init; }

    public bool WasBlocked => Action.Type is AutoModerationActionType.BlockMessage;

    public bool WasTimedOut => Action.Type is AutoModerationActionType.Timeout;
}

public sealed record ScheduledEventCreatedEvent(DiscordScheduledEvent ScheduledEvent) : DiscordEvent
{
    public Snowflake? GuildId => ScheduledEvent.GuildId;
}

public sealed record ScheduledEventUpdatedEvent(DiscordScheduledEvent ScheduledEvent) : DiscordEvent
{
    public Snowflake? GuildId => ScheduledEvent.GuildId;

    public ScheduledEventStatus Status => ScheduledEvent.Status;
}

public sealed record ScheduledEventDeletedEvent(DiscordScheduledEvent ScheduledEvent) : DiscordEvent
{
    public Snowflake? GuildId => ScheduledEvent.GuildId;
}

public sealed record ScheduledEventUserAddedEvent(Snowflake ScheduledEventId, Snowflake UserId, Snowflake GuildId)
    : DiscordEvent;

public sealed record ScheduledEventUserRemovedEvent(Snowflake ScheduledEventId, Snowflake UserId, Snowflake GuildId)
    : DiscordEvent;

public sealed record StageInstanceCreatedEvent(DiscordStageInstance Instance) : DiscordEvent
{
    public Snowflake? GuildId => Instance.GuildId;

    public Snowflake ChannelId => Instance.ChannelId;
}

public sealed record StageInstanceUpdatedEvent(DiscordStageInstance Instance) : DiscordEvent
{
    public Snowflake? GuildId => Instance.GuildId;

    public Snowflake ChannelId => Instance.ChannelId;
}

public sealed record StageInstanceDeletedEvent(DiscordStageInstance Instance) : DiscordEvent
{
    public Snowflake? GuildId => Instance.GuildId;

    public Snowflake ChannelId => Instance.ChannelId;
}

public sealed record IntegrationCreatedEvent(DiscordIntegration Integration) : DiscordEvent
{
    public Snowflake? GuildId => Integration.GuildId;
}

public sealed record IntegrationUpdatedEvent(DiscordIntegration Integration) : DiscordEvent
{
    public Snowflake? GuildId => Integration.GuildId;
}

public sealed record IntegrationDeletedEvent(Snowflake IntegrationId, Snowflake GuildId, Snowflake? ApplicationId)
    : DiscordEvent;

public sealed record GuildIntegrationsUpdatedEvent(Snowflake GuildId) : DiscordEvent;

public sealed record EntitlementCreatedEvent(DiscordEntitlement Entitlement) : DiscordEvent
{
    public Snowflake? UserId => Entitlement.UserId;

    public Snowflake SkuId => Entitlement.SkuId;
}

public sealed record EntitlementUpdatedEvent(DiscordEntitlement Entitlement) : DiscordEvent
{
    public Snowflake? UserId => Entitlement.UserId;

    public Snowflake SkuId => Entitlement.SkuId;

    public bool IsRenewal => !Entitlement.Deleted;
}

public sealed record EntitlementDeletedEvent(DiscordEntitlement Entitlement) : DiscordEvent
{
    public Snowflake? UserId => Entitlement.UserId;

    public Snowflake SkuId => Entitlement.SkuId;
}

public sealed record CommandPermissionsUpdatedEvent(DiscordCommandPermissions Permissions) : DiscordEvent
{
    public Snowflake CommandId => Permissions.CommandId;

    public Snowflake GuildId => Permissions.GuildId;

    public bool IsApplicationWide => Permissions.IsApplicationWide;
}

public sealed record UnknownEvent(JsonElement Data) : DiscordEvent;

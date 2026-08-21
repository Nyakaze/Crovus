namespace Crovus.Logs;

public abstract record TelemetryEvent
{
    public DateTimeOffset Timestamp { get; init; }
}

public sealed record RestRequestCompleted(string Method, string Route, int StatusCode, TimeSpan Duration,
    int Attempt) : TelemetryEvent;

public sealed record RestRequestFailed(string Method, string Route, string Reason, TimeSpan Duration,
    int Attempt) : TelemetryEvent;

public sealed record RestOperationCompleted(string Operation, TimeSpan Duration) : TelemetryEvent;

public sealed record RestOperationFailed(string Operation, string Reason, TimeSpan Duration) : TelemetryEvent;

public sealed record MessageCreated(ulong ChannelId, ulong MessageId) : TelemetryEvent;

public sealed record MessageEdited(ulong ChannelId, ulong MessageId) : TelemetryEvent;

public sealed record MessageDeleted(ulong ChannelId, ulong MessageId, string? Reason) : TelemetryEvent;

public sealed record ReactionAdded(ulong ChannelId, ulong MessageId, string Emoji) : TelemetryEvent;

public sealed record ReactionRemoved(ulong ChannelId, ulong MessageId, string Emoji) : TelemetryEvent;

public sealed record WebhookCreated(ulong WebhookId, ulong ChannelId, string Name) : TelemetryEvent;

public sealed record WebhookModified(ulong WebhookId) : TelemetryEvent;

public sealed record WebhookDeleted(ulong WebhookId) : TelemetryEvent;

public sealed record RateLimitDelayed(string Route, TimeSpan Delay, bool Global) : TelemetryEvent;

public sealed record RateLimitHit(string Route, string? BucketHash, TimeSpan RetryAfter, bool Global)
    : TelemetryEvent;

public sealed record RateLimitBucketUpdated(string Route, string? BucketHash, int Limit, int Remaining,
    TimeSpan ResetAfter) : TelemetryEvent;

public sealed record GatewayStateChanged(string Previous, string Current) : TelemetryEvent;

public sealed record GatewayDispatchReceived(string EventName, int? Sequence) : TelemetryEvent;

public sealed record GatewayHeartbeatAcknowledged(TimeSpan Latency) : TelemetryEvent;

public sealed record GatewayDisconnected(int? CloseCode, string? Reason, bool WillReconnect) : TelemetryEvent;

public sealed record WebhookExecuted(ulong WebhookId, ulong ChannelId, ulong? ThreadId, bool Waited)
    : TelemetryEvent;

public sealed record GatewayConnected(string Url, bool Resuming) : TelemetryEvent;

public sealed record GatewaySessionEstablished(string SessionId, bool Resumed) : TelemetryEvent;

public sealed record GatewaySessionInvalidated(bool Resumable) : TelemetryEvent;

public sealed record GatewayHeartbeatSent(int? Sequence) : TelemetryEvent;

public sealed record GatewayHeartbeatMissed(TimeSpan SinceSent) : TelemetryEvent;

public sealed record GatewayCommandSent(string Opcode, int Bytes, TimeSpan QueueLatency) : TelemetryEvent;

public sealed record GatewayCommandThrottled(string Opcode, TimeSpan Delay) : TelemetryEvent;

public sealed record GatewayCommandDropped(string Opcode, string Reason) : TelemetryEvent;

public sealed record GatewayEventQueueSaturated(int Capacity) : TelemetryEvent;

public sealed record GatewayPresenceUpdated(string Status, int Activities) : TelemetryEvent;

public sealed record GatewayMembersRequested(ulong GuildId, int UserIds, int Limit, bool WithPresences)
    : TelemetryEvent;

public sealed record VoiceStateChanged(ulong UserId, ulong? GuildId, ulong? ChannelId, ulong? PreviousChannelId)
    : TelemetryEvent;

public sealed record GatewayReconnectScheduled(int Attempt, TimeSpan Delay) : TelemetryEvent;

public sealed record CacheHit(string Entity, string Key) : TelemetryEvent;

public sealed record CacheMiss(string Entity, string Key) : TelemetryEvent;

public sealed record CacheEntryWritten(string Entity) : TelemetryEvent;

public sealed record CacheEntryInvalidated(string Entity, string Key) : TelemetryEvent;

public sealed record CacheEntryEvicted(string Entity, string Reason) : TelemetryEvent;

public sealed record CacheCleared : TelemetryEvent;

public sealed record ChannelCreated(ulong GuildId, ulong ChannelId, string Type, string Name) : TelemetryEvent;

public sealed record ChannelModified(ulong ChannelId) : TelemetryEvent;

public sealed record ChannelDeleted(ulong ChannelId, string? Reason) : TelemetryEvent;

public sealed record ThreadCreated(ulong ChannelId, ulong ThreadId, string Type, string Name) : TelemetryEvent;

public sealed record EmojiCreated(ulong GuildId, ulong EmojiId, string Name) : TelemetryEvent;

public sealed record EmojiModified(ulong GuildId, ulong EmojiId) : TelemetryEvent;

public sealed record EmojiDeleted(ulong GuildId, ulong EmojiId, string? Reason) : TelemetryEvent;

public sealed record ApplicationCommandCreated(ulong ApplicationId, ulong CommandId, string Name, ulong? GuildId)
    : TelemetryEvent;

public sealed record ApplicationCommandEdited(ulong ApplicationId, ulong CommandId, ulong? GuildId) : TelemetryEvent;

public sealed record ApplicationCommandDeleted(ulong ApplicationId, ulong CommandId, ulong? GuildId) : TelemetryEvent;

public sealed record ApplicationCommandsOverwritten(ulong ApplicationId, int Count, ulong? GuildId) : TelemetryEvent;

public sealed record ServiceOperationCompleted(string Service, string Operation, TimeSpan Duration) : TelemetryEvent;

public sealed record ServiceOperationFailed(string Service, string Operation, string Reason, TimeSpan Duration)
    : TelemetryEvent;

public sealed record MessageBroadcast(int Targets, int Delivered, int Failed) : TelemetryEvent;

public sealed record MessagesPurged(ulong ChannelId, int Deleted, int Failed) : TelemetryEvent;

public sealed record ReactionsApplied(ulong ChannelId, ulong MessageId, int Count) : TelemetryEvent;

public sealed record ReactionsWithdrawn(ulong ChannelId, ulong MessageId, int Count) : TelemetryEvent;

public sealed record WebhookResolved(ulong ChannelId, ulong WebhookId, bool Created) : TelemetryEvent;

public sealed record EmojiResolved(ulong GuildId, ulong EmojiId, bool Created) : TelemetryEvent;

public sealed record ThreadArchiveToggled(ulong ThreadId, bool Archived) : TelemetryEvent;

public sealed record ThreadLockToggled(ulong ThreadId, bool Locked) : TelemetryEvent;

public sealed record CommandsSynchronized(ulong ApplicationId, ulong? GuildId, int Added, int Changed, int Removed,
    int Unchanged) : TelemetryEvent;

public sealed record ClientConnected(ulong UserId, ulong? ApplicationId, string SessionId) : TelemetryEvent;

public sealed record ClientDisconnected(TimeSpan Uptime, long Dispatched) : TelemetryEvent;

public sealed record EventDispatched(string Event, int Handlers, TimeSpan Duration) : TelemetryEvent;

public sealed record EventHandlerFailed(string Event, string Handler, string Reason) : TelemetryEvent;

public sealed record EventDecodeFailed(string Event, string Reason) : TelemetryEvent;

public sealed record PresencePublished(ulong UserId, string Status, int Handlers, TimeSpan Duration)
    : TelemetryEvent;

public sealed record PresenceHandlerFailed(ulong UserId, string Handler, string Reason) : TelemetryEvent;

public sealed record InteractionReceived(ulong InteractionId, string Type, string Command, ulong? GuildId)
    : TelemetryEvent;

public sealed record InteractionResponded(ulong InteractionId, string Callback, bool Ephemeral) : TelemetryEvent;

public sealed record InteractionFollowedUp(ulong ApplicationId, ulong MessageId, bool Ephemeral) : TelemetryEvent;

public sealed record InteractionAutocompleted(ulong InteractionId, string Option, int Choices) : TelemetryEvent;

public sealed record InteractionExpired(ulong InteractionId, TimeSpan Age) : TelemetryEvent;

public sealed record MembersFetched(ulong GuildId, int Count, ulong? After) : TelemetryEvent;

public sealed record MemberModified(ulong GuildId, ulong UserId, string Changes) : TelemetryEvent;

public sealed record MemberRoleChanged(ulong GuildId, ulong UserId, ulong RoleId, bool Granted) : TelemetryEvent;

public sealed record MemberKicked(ulong GuildId, ulong UserId, string? Reason) : TelemetryEvent;

public sealed record MemberBanned(ulong GuildId, ulong UserId, int DeleteMessageSeconds, string? Reason)
    : TelemetryEvent;

public sealed record MemberUnbanned(ulong GuildId, ulong UserId, string? Reason) : TelemetryEvent;

public sealed record MemberTimedOut(ulong GuildId, ulong UserId, TimeSpan Duration) : TelemetryEvent;

public sealed record RoleCreated(ulong GuildId, ulong RoleId, string Name, ulong Permissions) : TelemetryEvent;

public sealed record RoleModified(ulong GuildId, ulong RoleId, string Name, ulong Permissions) : TelemetryEvent;

public sealed record RoleDeleted(ulong GuildId, ulong RoleId, string? Reason) : TelemetryEvent;

public sealed record RoleResolved(ulong GuildId, ulong RoleId, bool Created) : TelemetryEvent;

public sealed record GuildFetched(ulong GuildId, string Name, int MemberCount) : TelemetryEvent;

public sealed record MessagesBulkDeleted(ulong ChannelId, int Count, string? Reason) : TelemetryEvent;

public sealed record MessageCrossposted(ulong ChannelId, ulong MessageId) : TelemetryEvent;

public sealed record MessagePinned(ulong ChannelId, ulong MessageId, string? Reason) : TelemetryEvent;

public sealed record MessageUnpinned(ulong ChannelId, ulong MessageId, string? Reason) : TelemetryEvent;

public sealed record TypingTriggered(ulong ChannelId) : TelemetryEvent;

public sealed record UserReactionRemoved(ulong ChannelId, ulong MessageId, string Emoji, ulong UserId)
    : TelemetryEvent;

public sealed record ReactionsCleared(ulong ChannelId, ulong MessageId, string? Emoji) : TelemetryEvent;

public sealed record DirectChannelOpened(ulong UserId, ulong ChannelId) : TelemetryEvent;

public sealed record GuildLeft(ulong GuildId) : TelemetryEvent;

public sealed record GuildModified(ulong GuildId, string Changes, string? Reason) : TelemetryEvent;

public sealed record GuildPruned(ulong GuildId, int Days, int? Removed, string? Reason) : TelemetryEvent;

public sealed record InviteIssued(ulong ChannelId, string Code, int MaxUses, TimeSpan MaxAge) : TelemetryEvent;

public sealed record InviteRevoked(string Code, string? Reason) : TelemetryEvent;

public sealed record ThreadJoined(ulong ThreadId) : TelemetryEvent;

public sealed record ThreadLeft(ulong ThreadId) : TelemetryEvent;

public sealed record ThreadMemberAdded(ulong ThreadId, ulong UserId) : TelemetryEvent;

public sealed record ThreadMemberRemoved(ulong ThreadId, ulong UserId) : TelemetryEvent;

public sealed record AttachmentsUploaded(string Method, string Route, int Files, long Bytes) : TelemetryEvent;

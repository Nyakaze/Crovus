using System.Text.Json;
using System.Text.Json.Serialization;

namespace Crovus.Models;

public enum AuditLogAction
{
    Unknown = -1,
    GuildUpdate = 1,
    ChannelCreate = 10,
    ChannelUpdate = 11,
    ChannelDelete = 12,
    ChannelOverwriteCreate = 13,
    ChannelOverwriteUpdate = 14,
    ChannelOverwriteDelete = 15,
    MemberKick = 20,
    MemberPrune = 21,
    MemberBanAdd = 22,
    MemberBanRemove = 23,
    MemberUpdate = 24,
    MemberRoleUpdate = 25,
    MemberMove = 26,
    MemberDisconnect = 27,
    BotAdd = 28,
    RoleCreate = 30,
    RoleUpdate = 31,
    RoleDelete = 32,
    InviteCreate = 40,
    InviteUpdate = 41,
    InviteDelete = 42,
    WebhookCreate = 50,
    WebhookUpdate = 51,
    WebhookDelete = 52,
    EmojiCreate = 60,
    EmojiUpdate = 61,
    EmojiDelete = 62,
    MessageDelete = 72,
    MessageBulkDelete = 73,
    MessagePin = 74,
    MessageUnpin = 75,
    IntegrationCreate = 80,
    IntegrationUpdate = 81,
    IntegrationDelete = 82,
    StageInstanceCreate = 83,
    StageInstanceUpdate = 84,
    StageInstanceDelete = 85,
    StickerCreate = 90,
    StickerUpdate = 91,
    StickerDelete = 92,
    ScheduledEventCreate = 100,
    ScheduledEventUpdate = 101,
    ScheduledEventDelete = 102,
    ThreadCreate = 110,
    ThreadUpdate = 111,
    ThreadDelete = 112,
    ApplicationCommandPermissionUpdate = 121,
    AutoModerationRuleCreate = 140,
    AutoModerationRuleUpdate = 141,
    AutoModerationRuleDelete = 142,
    AutoModerationBlockMessage = 143,
    AutoModerationFlagToChannel = 144,
    AutoModerationUserCommunicationDisabled = 145,
    CreatorMonetizationRequestCreated = 150,
    CreatorMonetizationTermsAccepted = 151,
    OnboardingPromptCreate = 163,
    OnboardingPromptUpdate = 164,
    OnboardingPromptDelete = 165,
    OnboardingCreate = 166,
    OnboardingUpdate = 167,
    HomeSettingsCreate = 190,
    HomeSettingsUpdate = 191
}

public sealed record DiscordAuditLogChange(string Key, JsonElement? OldValue, JsonElement? NewValue)
{
    [JsonIgnore]
    public bool IsAddition => OldValue is null && NewValue is not null;

    [JsonIgnore]
    public bool IsRemoval => OldValue is not null && NewValue is null;

    public T? Before<T>(JsonSerializerOptions? options = null) =>
        OldValue is { } value ? value.Deserialize<T>(options) : default;

    public T? After<T>(JsonSerializerOptions? options = null) =>
        NewValue is { } value ? value.Deserialize<T>(options) : default;

    public override string ToString() => $"{Key}: {OldValue} -> {NewValue}";
}

public sealed record DiscordAuditLogEntry
{
    public required Snowflake Id { get; init; }

    public Snowflake? GuildId { get; init; }

    public Snowflake? TargetId { get; init; }

    public Snowflake? UserId { get; init; }

    public AuditLogAction Action { get; init; } = AuditLogAction.Unknown;

    public string? Reason { get; init; }

    public IReadOnlyList<DiscordAuditLogChange> Changes { get; init; } = [];

    public Snowflake? ChannelId { get; init; }

    public Snowflake? MessageId { get; init; }

    public Snowflake? RoleId { get; init; }

    public string? RoleName { get; init; }

    public int? Count { get; init; }

    public int? DeleteMemberDays { get; init; }

    public int? MembersRemoved { get; init; }

    public string? AutoModerationRuleName { get; init; }

    public string? AutoModerationTriggerType { get; init; }

    [JsonIgnore]
    public DateTimeOffset CreatedAt => Id.CreatedAt;

    [JsonIgnore]
    public bool HasChanges => Changes.Count > 0;

    public DiscordAuditLogEntry In(Snowflake guildId) => GuildId is null ? this with { GuildId = guildId } : this;

    public DiscordAuditLogChange? Change(string key) =>
        Changes.FirstOrDefault(change => string.Equals(change.Key, key, StringComparison.Ordinal));

    public override string ToString() => $"{Action} by {UserId}";
}

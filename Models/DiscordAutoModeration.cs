using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public enum AutoModerationTriggerType
{
    Keyword = 1,
    Spam = 3,
    KeywordPreset = 4,
    MentionSpam = 5,
    MemberProfile = 6
}

public enum AutoModerationEventType
{
    MessageSend = 1,
    MemberUpdate = 2
}

public enum AutoModerationActionType
{
    BlockMessage = 1,
    SendAlertMessage = 2,
    Timeout = 3,
    BlockMemberInteraction = 4
}

public enum AutoModerationKeywordPreset
{
    Profanity = 1,
    SexualContent = 2,
    Slurs = 3
}

public sealed record AutoModerationTriggerMetadata
{
    public IReadOnlyList<string> KeywordFilter { get; init; } = [];

    public IReadOnlyList<string> RegexPatterns { get; init; } = [];

    public IReadOnlyList<AutoModerationKeywordPreset> Presets { get; init; } = [];

    public IReadOnlyList<string> AllowList { get; init; } = [];

    public int? MentionTotalLimit { get; init; }

    public bool MentionRaidProtectionEnabled { get; init; }
}

public sealed record AutoModerationAction
{
    public AutoModerationActionType Type { get; init; }

    public Snowflake? ChannelId { get; init; }

    public TimeSpan? Duration { get; init; }

    public string? CustomMessage { get; init; }

    public override string ToString() => Type.ToString();
}

public sealed record DiscordAutoModerationRule : IBoundEntity
{
    public required Snowflake Id { get; init; }

    public Snowflake? GuildId { get; init; }

    [JsonIgnore]
    public bool IsPartial { get; init; }

    public static DiscordAutoModerationRule Partial(Snowflake id, Snowflake? guildId = null) =>
        new() { Id = id, GuildId = guildId, Name = string.Empty, IsPartial = true };

    public required string Name { get; init; }

    public Snowflake? CreatorId { get; init; }

    public AutoModerationEventType EventType { get; init; } = AutoModerationEventType.MessageSend;

    public AutoModerationTriggerType TriggerType { get; init; } = AutoModerationTriggerType.Keyword;

    public AutoModerationTriggerMetadata? TriggerMetadata { get; init; }

    public IReadOnlyList<AutoModerationAction> Actions { get; init; } = [];

    public bool Enabled { get; init; }

    public IReadOnlyList<Snowflake> ExemptRoles { get; init; } = [];

    public IReadOnlyList<Snowflake> ExemptChannels { get; init; } = [];

    [JsonIgnore]
    public bool Blocks => Actions.Any(action => action.Type is AutoModerationActionType.BlockMessage);

    [JsonIgnore]
    public bool Alerts => Actions.Any(action => action.Type is AutoModerationActionType.SendAlertMessage);

    [JsonIgnore]
    public bool TimesOut => Actions.Any(action => action.Type is AutoModerationActionType.Timeout);

    public DiscordAutoModerationRule In(Snowflake guildId) => GuildId is null ? this with { GuildId = guildId } : this;

    public override string ToString() => Name;

    private EntityBinding _binding;

    public DiscordAutoModerationRule Bind(ICrovusContext context)
    {
        var bound = this with { };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}

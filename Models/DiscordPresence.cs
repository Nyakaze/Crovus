using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public enum UserStatus
{
    Offline,
    Online,
    Idle,
    DoNotDisturb,
    Invisible
}

public enum ActivityType
{
    Playing = 0,
    Streaming = 1,
    Listening = 2,
    Watching = 3,
    Custom = 4,
    Competing = 5
}

[Flags]
public enum ActivityFlags
{
    None = 0,
    Instance = 1 << 0,
    Join = 1 << 1,
    Spectate = 1 << 2,
    JoinRequest = 1 << 3,
    Sync = 1 << 4,
    Play = 1 << 5,
    PartyPrivacyFriends = 1 << 6,
    PartyPrivacyVoiceChannel = 1 << 7,
    Embedded = 1 << 8
}

public sealed record DiscordActivityTimestamps(DateTimeOffset? Start, DateTimeOffset? End)
{
    [JsonIgnore]
    public TimeSpan? Elapsed => Start is { } start ? DateTimeOffset.UtcNow - start : null;

    [JsonIgnore]
    public TimeSpan? Remaining =>
        End is { } end && end > DateTimeOffset.UtcNow ? end - DateTimeOffset.UtcNow : null;

    [JsonIgnore]
    public TimeSpan? Duration => Start is { } start && End is { } end ? end - start : null;
}

public sealed record DiscordActivityParty(string? Id, int? CurrentSize, int? MaxSize)
{
    [JsonIgnore]
    public bool IsFull => CurrentSize is { } current && MaxSize is { } max && current >= max;

    [JsonIgnore]
    public int? OpenSlots => CurrentSize is { } current && MaxSize is { } max ? Math.Max(0, max - current) : null;
}

public sealed record DiscordActivityAssets(string? LargeImage, string? LargeText, string? SmallImage,
    string? SmallText)
{
    public string? LargeImageUrl(Snowflake? applicationId) => BuildUrl(LargeImage, applicationId);

    public string? SmallImageUrl(Snowflake? applicationId) => BuildUrl(SmallImage, applicationId);

    private static string? BuildUrl(string? asset, Snowflake? applicationId)
    {
        if (string.IsNullOrEmpty(asset))
            return null;

        if (asset.StartsWith("mp:external/", StringComparison.Ordinal))
            return $"https://media.discordapp.net/{asset[3..]}";

        if (asset.StartsWith("spotify:", StringComparison.Ordinal))
            return $"https://i.scdn.co/image/{asset["spotify:".Length..]}";

        return applicationId is { } id ? $"https://cdn.discordapp.com/app-assets/{id.Value}/{asset}.png" : null;
    }
}

public sealed record DiscordActivity
{
    public required string Name { get; init; }

    public ActivityType Type { get; init; }

    public string? Url { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DiscordActivityTimestamps? Timestamps { get; init; }

    public Snowflake? ApplicationId { get; init; }

    public string? Details { get; init; }

    public string? State { get; init; }

    public DiscordEmoji? Emoji { get; init; }

    public DiscordActivityParty? Party { get; init; }

    public DiscordActivityAssets? Assets { get; init; }

    public ActivityFlags Flags { get; init; }

    public IReadOnlyList<string> Buttons { get; init; } = [];

    [JsonIgnore]
    public bool IsCustomStatus => Type is ActivityType.Custom;

    [JsonIgnore]
    public bool IsStreaming => Type is ActivityType.Streaming;

    [JsonIgnore]
    public bool IsRichPresence => Details is not null || Assets is not null || Party is not null;

    [JsonIgnore]
    public string? CustomText => IsCustomStatus ? State : null;

    [JsonIgnore]
    public string? LargeImageUrl => Assets?.LargeImageUrl(ApplicationId);

    [JsonIgnore]
    public string? SmallImageUrl => Assets?.SmallImageUrl(ApplicationId);

    [JsonIgnore]
    public TimeSpan? Elapsed => Timestamps?.Elapsed;

    public override string ToString() => Type switch
    {
        ActivityType.Custom => Emoji is { } emoji && !string.IsNullOrEmpty(State)
            ? $"{emoji} {State}"
            : State ?? Emoji?.ToString() ?? string.Empty,
        ActivityType.Playing => $"Playing {Name}",
        ActivityType.Streaming => $"Streaming {Name}",
        ActivityType.Listening => $"Listening to {Name}",
        ActivityType.Watching => $"Watching {Name}",
        ActivityType.Competing => $"Competing in {Name}",
        _ => Name
    };
}

public sealed record DiscordClientStatus(UserStatus? Desktop, UserStatus? Mobile, UserStatus? Web)
{
    public static readonly DiscordClientStatus None = new(null, null, null);

    [JsonIgnore]
    public bool IsOnDesktop => Desktop is not null;

    [JsonIgnore]
    public bool IsOnMobile => Mobile is not null;

    [JsonIgnore]
    public bool IsOnWeb => Web is not null;

    [JsonIgnore]
    public bool IsOnAnyClient => IsOnDesktop || IsOnMobile || IsOnWeb;
}

public sealed record DiscordPresence : IBoundEntity
{
    public required Snowflake UserId { get; init; }

    public Snowflake? GuildId { get; init; }

    public DiscordUser? User { get; init; }

    public UserStatus Status { get; init; } = UserStatus.Offline;

    public IReadOnlyList<DiscordActivity> Activities { get; init; } = [];

    public DiscordClientStatus ClientStatus { get; init; } = DiscordClientStatus.None;

    [JsonIgnore]
    public bool IsOnline => Status is not (UserStatus.Offline or UserStatus.Invisible);

    [JsonIgnore]
    public bool IsIdle => Status is UserStatus.Idle;

    [JsonIgnore]
    public bool IsBusy => Status is UserStatus.DoNotDisturb;

    [JsonIgnore]
    public bool HasActivities => Activities.Count > 0;

    [JsonIgnore]
    public DiscordActivity? CustomStatus => Find(ActivityType.Custom);

    [JsonIgnore]
    public DiscordActivity? Playing => Find(ActivityType.Playing);

    [JsonIgnore]
    public DiscordActivity? Streaming => Find(ActivityType.Streaming);

    [JsonIgnore]
    public DiscordActivity? Listening => Find(ActivityType.Listening);

    [JsonIgnore]
    public DiscordActivity? Watching => Find(ActivityType.Watching);

    [JsonIgnore]
    public DiscordActivity? Competing => Find(ActivityType.Competing);

    [JsonIgnore]
    public DiscordActivity? Primary =>
        Activities.FirstOrDefault(activity => activity.Type is not ActivityType.Custom) ??
        Activities.FirstOrDefault();

    [JsonIgnore]
    public string? CustomText => CustomStatus?.CustomText;

    [JsonIgnore]
    public ActivityTypes ActiveTypes => Activities.Types();

    public DiscordActivity? Find(ActivityType type) =>
        Activities.FirstOrDefault(activity => activity.Type == type);

    public DiscordActivity? Find(ActivityTypes types) =>
        Activities.FirstOrDefault(activity => types.Includes(activity.Type));

    public IReadOnlyList<DiscordActivity> ActivitiesOf(ActivityTypes types) => [.. Activities.WithTypes(types)];

    public IReadOnlyList<DiscordActivity> ActivitiesOf(ActivityType type) => [.. Activities.WithType(type)];

    public bool Has(ActivityTypes types) => Activities.HasAny(types);

    public bool Has(ActivityType type) => Find(type) is not null;

    public bool Has(ActivityType type, string name) => Activities.Any(activity =>
        activity.Type == type &&
        string.Equals(activity.Name, name, StringComparison.OrdinalIgnoreCase));

    public bool IsPlaying(string name) => Has(ActivityType.Playing, name);

    public bool IsListeningTo(string name) => Has(ActivityType.Listening, name);

    public bool IsWatching(string name) => Has(ActivityType.Watching, name);

    public bool IsCompetingIn(string name) => Has(ActivityType.Competing, name);

    public DiscordPresence In(Snowflake guildId) => this with { GuildId = guildId };

    public override string ToString() =>
        Primary is { } activity ? $"{Status} - {activity}" : Status.ToString();

    private EntityBinding _binding;

    public DiscordPresence Bind(ICrovusContext context)
    {
        var bound = this with { User = User?.Bind(context) };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}

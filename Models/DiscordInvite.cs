using System.Text.Json.Serialization;

namespace Crovus.Models;

public enum InviteTargetType
{
    None = 0,
    Stream = 1,
    EmbeddedApplication = 2
}

public sealed record DiscordInvite
{
    public required string Code { get; init; }

    public Snowflake? GuildId { get; init; }

    public Snowflake? ChannelId { get; init; }

    public DiscordUser? Inviter { get; init; }

    public DiscordUser? TargetUser { get; init; }

    public InviteTargetType TargetType { get; init; }

    public int Uses { get; init; }

    public int MaxUses { get; init; }

    public TimeSpan MaxAge { get; init; }

    public bool Temporary { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public int? ApproximateMemberCount { get; init; }

    public int? ApproximatePresenceCount { get; init; }

    [JsonIgnore]
    public string Url => $"https://discord.gg/{Code}";

    [JsonIgnore]
    public bool IsPermanent => MaxAge == TimeSpan.Zero;

    [JsonIgnore]
    public bool IsUnlimited => MaxUses == 0;

    [JsonIgnore]
    public int? RemainingUses => IsUnlimited ? null : Math.Max(0, MaxUses - Uses);

    [JsonIgnore]
    public bool IsExhausted => !IsUnlimited && Uses >= MaxUses;

    [JsonIgnore]
    public bool IsExpired => ExpiresAt is { } expiry && expiry <= DateTimeOffset.UtcNow;

    public override string ToString() => Url;
}

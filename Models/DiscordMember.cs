using System.Text.Json.Serialization;

namespace Crovus.Models;

[Flags]
public enum GuildMemberFlags
{
    None = 0,
    DidRejoin = 1 << 0,
    CompletedOnboarding = 1 << 1,
    BypassesVerification = 1 << 2,
    StartedOnboarding = 1 << 3
}

public sealed record DiscordMember
{
    public static readonly TimeSpan MaxTimeout = TimeSpan.FromDays(28);

    public required DiscordUser User { get; init; }

    public Snowflake? GuildId { get; init; }

    public string? Nickname { get; init; }

    public string? Avatar { get; init; }

    public IReadOnlyList<Snowflake> Roles { get; init; } = [];

    public DateTimeOffset? JoinedAt { get; init; }

    public DateTimeOffset? PremiumSince { get; init; }

    public DateTimeOffset? CommunicationDisabledUntil { get; init; }

    public DiscordPermissions Permissions { get; init; }

    public GuildMemberFlags Flags { get; init; }

    public bool Deaf { get; init; }

    public bool Mute { get; init; }

    public bool Pending { get; init; }

    [JsonIgnore]
    public Snowflake Id => User.Id;

    [JsonIgnore]
    public string DisplayName => Nickname ?? User.DisplayName;

    [JsonIgnore]
    public string Mention => User.Mention;

    [JsonIgnore]
    public bool IsBoosting => PremiumSince is not null;

    [JsonIgnore]
    public bool IsTimedOut => CommunicationDisabledUntil > DateTimeOffset.UtcNow;

    [JsonIgnore]
    public TimeSpan? RemainingTimeout =>
        CommunicationDisabledUntil is { } until && until > DateTimeOffset.UtcNow
            ? until - DateTimeOffset.UtcNow
            : null;

    [JsonIgnore]
    public string AvatarUrl => Avatar is null || GuildId is null
        ? User.AvatarUrl
        : $"https://cdn.discordapp.com/guilds/{GuildId.Value.Value}/users/{User.Id.Value}/avatars/{Avatar}." +
          (Avatar.StartsWith("a_") ? "gif" : "png");

    public bool Can(DiscordPermissions permission) =>
        (Permissions & DiscordPermissions.Administrator) == DiscordPermissions.Administrator ||
        (Permissions & permission) == permission;

    public bool HasRole(Snowflake roleId) => Roles.Contains(roleId);

    public bool HasRole(DiscordRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return Roles.Contains(role.Id);
    }

    public DiscordMember In(Snowflake guildId) => this with { GuildId = guildId };

    public override string ToString() => $"{DisplayName} ({User.Id.Value})";
}

public sealed record DiscordBan(DiscordUser User, string? Reason);

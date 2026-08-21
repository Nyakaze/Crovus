using System.Text.Json.Serialization;

namespace Crovus.Models;

public enum VerificationLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4
}

public enum MessageNotificationLevel
{
    AllMessages = 0,
    OnlyMentions = 1
}

public enum ExplicitContentFilterLevel
{
    Disabled = 0,
    MembersWithoutRoles = 1,
    AllMembers = 2
}

public enum MfaLevel
{
    None = 0,
    Elevated = 1
}

public enum GuildNsfwLevel
{
    Default = 0,
    Explicit = 1,
    Safe = 2,
    AgeRestricted = 3
}

public enum PremiumTier
{
    None = 0,
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3
}

public sealed record DiscordGuild
{
    public required Snowflake Id { get; init; }

    public required string Name { get; init; }

    [JsonIgnore]
    public bool IsPartial { get; init; }

    public static DiscordGuild Partial(Snowflake id) =>
        new() { Id = id, Name = string.Empty, IsPartial = true };

    public Snowflake OwnerId { get; init; }

    public string? Icon { get; init; }

    public string? Banner { get; init; }

    public string? Splash { get; init; }

    public string? Description { get; init; }

    public string? VanityUrlCode { get; init; }

    public string PreferredLocale { get; init; } = "en-US";

    public Snowflake? AfkChannelId { get; init; }

    public int AfkTimeout { get; init; }

    public Snowflake? SystemChannelId { get; init; }

    public Snowflake? RulesChannelId { get; init; }

    public Snowflake? PublicUpdatesChannelId { get; init; }

    public VerificationLevel VerificationLevel { get; init; }

    public MessageNotificationLevel DefaultMessageNotifications { get; init; }

    public ExplicitContentFilterLevel ExplicitContentFilter { get; init; }

    public MfaLevel MfaLevel { get; init; }

    public GuildNsfwLevel NsfwLevel { get; init; }

    public PremiumTier PremiumTier { get; init; }

    public int PremiumSubscriptionCount { get; init; }

    public int? MemberCount { get; init; }

    public int? MaxMembers { get; init; }

    public int? ApproximateMemberCount { get; init; }

    public int? ApproximatePresenceCount { get; init; }

    public bool Large { get; init; }

    public bool Unavailable { get; init; }

    public IReadOnlyList<string> Features { get; init; } = [];

    public IReadOnlyList<DiscordRole> Roles { get; init; } = [];

    public IReadOnlyList<DiscordGuildEmoji> Emojis { get; init; } = [];

    [JsonIgnore]
    public DateTimeOffset CreatedAt => Id.CreatedAt;

    [JsonIgnore]
    public DiscordRole? EveryoneRole => Roles.FirstOrDefault(role => role.Id.Value == Id.Value);

    [JsonIgnore]
    public string? IconUrl => Icon is null
        ? null
        : $"https://cdn.discordapp.com/icons/{Id.Value}/{Icon}.{(Icon.StartsWith("a_") ? "gif" : "png")}";

    [JsonIgnore]
    public string? BannerUrl => Banner is null
        ? null
        : $"https://cdn.discordapp.com/banners/{Id.Value}/{Banner}.{(Banner.StartsWith("a_") ? "gif" : "png")}";

    [JsonIgnore]
    public string? SplashUrl => Splash is null
        ? null
        : $"https://cdn.discordapp.com/splashes/{Id.Value}/{Splash}.png";

    [JsonIgnore]
    public string? VanityUrl => VanityUrlCode is null ? null : $"https://discord.gg/{VanityUrlCode}";

    [JsonIgnore]
    public int MaxEmojis => PremiumTier switch
    {
        PremiumTier.Tier1 => 100,
        PremiumTier.Tier2 => 150,
        PremiumTier.Tier3 => 250,
        _ => 50
    };

    [JsonIgnore]
    public int MaxUploadBytes => PremiumTier switch
    {
        PremiumTier.Tier2 => 50 * 1024 * 1024,
        PremiumTier.Tier3 => 100 * 1024 * 1024,
        _ => 10 * 1024 * 1024
    };

    public bool Has(string feature) =>
        Features.Any(existing => string.Equals(existing, feature, StringComparison.OrdinalIgnoreCase));

    public DiscordRole? Role(Snowflake roleId) => Roles.FirstOrDefault(role => role.Id == roleId);

    public DiscordRole? Role(string name) =>
        Roles.FirstOrDefault(role => string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase));

    public DiscordPermissions PermissionsOf(DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        var permissions = EveryoneRole?.Permissions ?? DiscordPermissions.None;

        foreach (var roleId in member.Roles)
            if (Role(roleId) is { } role)
                permissions |= role.Permissions;

        return member.User.Id == OwnerId ? permissions | DiscordPermissions.Administrator : permissions;
    }

    public bool Allows(DiscordMember member, DiscordPermissions permission)
    {
        var permissions = PermissionsOf(member);

        return (permissions & DiscordPermissions.Administrator) == DiscordPermissions.Administrator ||
               (permissions & permission) == permission;
    }

    public DiscordRole? HighestRoleOf(DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        DiscordRole? highest = null;

        foreach (var roleId in member.Roles)
            if (Role(roleId) is { } role && (highest is null || role.IsAbove(highest)))
                highest = role;

        return highest;
    }

    public override string ToString() => $"{Name} ({Id.Value})";
}

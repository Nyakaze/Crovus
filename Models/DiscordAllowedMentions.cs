namespace Crovus.Models;

[Flags]
public enum MentionTypes
{
    None = 0,
    Users = 1 << 0,
    Roles = 1 << 1,
    Everyone = 1 << 2,
    All = Users | Roles | Everyone
}

public sealed record DiscordAllowedMentions
{
    public const int MaxIds = 100;

    public static DiscordAllowedMentions None { get; } = new();

    public static DiscordAllowedMentions All { get; } = new() { Parse = MentionTypes.All, RepliedUser = true };

    public MentionTypes Parse { get; init; }

    public IReadOnlyList<Snowflake> Users { get; init; } = [];

    public IReadOnlyList<Snowflake> Roles { get; init; } = [];

    public bool RepliedUser { get; init; }

    public static DiscordAllowedMentions OnlyUsers(params Snowflake[] userIds) => new() { Users = userIds };

    public static DiscordAllowedMentions OnlyRoles(params Snowflake[] roleIds) => new() { Roles = roleIds };

    public DiscordAllowedMentions AllowingUsers(params Snowflake[] userIds) =>
        this with { Users = [.. Users, .. userIds] };

    public DiscordAllowedMentions AllowingRoles(params Snowflake[] roleIds) =>
        this with { Roles = [.. Roles, .. roleIds] };

    public DiscordAllowedMentions Parsing(MentionTypes types) => this with { Parse = Parse | types };

    public DiscordAllowedMentions WithRepliedUser(bool mention = true) => this with { RepliedUser = mention };

    public void Validate()
    {
        if (Parse.HasFlag(MentionTypes.Users) && Users.Count > 0)
            throw new InvalidOperationException(
                "Allowed mentions cannot parse all users and list specific users at the same time.");

        if (Parse.HasFlag(MentionTypes.Roles) && Roles.Count > 0)
            throw new InvalidOperationException(
                "Allowed mentions cannot parse all roles and list specific roles at the same time.");

        if (Users.Count > MaxIds)
            throw new InvalidOperationException($"Allowed mentions can list at most {MaxIds} users.");

        if (Roles.Count > MaxIds)
            throw new InvalidOperationException($"Allowed mentions can list at most {MaxIds} roles.");
    }
}

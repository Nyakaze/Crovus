namespace Crovus.Models;

public sealed record DiscordUser(Snowflake Id, string Username, string? GlobalName, string? Discriminator,
    string? Avatar, bool IsBot)
{
    public string DisplayName => GlobalName ?? Username;

    public string Mention => $"<@{Id.Value}>";

    public string AvatarUrl => Avatar is null
        ? $"https://cdn.discordapp.com/embed/avatars/{(Id.Value >> 22) % 6}.png"
        : $"https://cdn.discordapp.com/avatars/{Id.Value}/{Avatar}.{(Avatar.StartsWith("a_") ? "gif" : "png")}";
}

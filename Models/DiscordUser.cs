using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public sealed record DiscordUser(Snowflake Id, string Username, string? GlobalName, string? Discriminator,
    string? Avatar, bool? IsBot) : IBoundEntity
{
    [JsonIgnore]
    public bool IsPartial { get; init; }

    public static DiscordUser Partial(Snowflake id) =>
        new(id, string.Empty, null, null, null, null) { IsPartial = true };

    public string DisplayName => GlobalName ?? (Username.Length > 0 ? Username : Id.ToString());

    public string Mention => $"<@{Id.Value}>";

    public string AvatarUrl => Avatar is null
        ? $"https://cdn.discordapp.com/embed/avatars/{(Id.Value >> 22) % 6}.png"
        : $"https://cdn.discordapp.com/avatars/{Id.Value}/{Avatar}.{(Avatar.StartsWith("a_") ? "gif" : "png")}";

    private EntityBinding _binding;

    public DiscordUser Bind(ICrovusContext context)
    {
        var bound = this with { };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}

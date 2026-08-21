using System.Globalization;
using System.Text.Json.Serialization;

namespace Crovus.Models;

public sealed record DiscordRoleTags(Snowflake? BotId, Snowflake? IntegrationId, bool Premium,
    Snowflake? SubscriptionListingId, bool AvailableForPurchase, bool GuildConnections);

public sealed record DiscordRole(Snowflake Id, Snowflake? GuildId, string Name, int Color, bool Hoist, int Position,
    DiscordPermissions Permissions, bool Managed, bool Mentionable, string? Icon, string? UnicodeEmoji,
    DiscordRoleTags? Tags)
{
    [JsonIgnore]
    public bool IsPartial { get; init; }

    public static DiscordRole Partial(Snowflake id, Snowflake? guildId = null) =>
        new(id, guildId, string.Empty, 0, false, 0, DiscordPermissions.None, false, false, null, null, null)
        {
            IsPartial = true
        };

    public DiscordRole In(Snowflake guildId) => GuildId is null ? this with { GuildId = guildId } : this;

    [JsonIgnore]
    public string Mention => $"<@&{Id.Value}>";

    [JsonIgnore]
    public bool IsEveryone => GuildId is { } guild && guild.Value == Id.Value;

    [JsonIgnore]
    public bool IsBotRole => Tags?.BotId is not null;

    [JsonIgnore]
    public bool IsBoosterRole => Tags?.Premium ?? false;

    [JsonIgnore]
    public bool HasColor => Color != 0;

    [JsonIgnore]
    public string HexColor => $"#{Color:X6}";

    [JsonIgnore]
    public string? IconUrl => Icon is null
        ? null
        : $"https://cdn.discordapp.com/role-icons/{Id.Value}/{Icon}.{(Icon.StartsWith("a_") ? "gif" : "png")}";

    public bool Grants(DiscordPermissions permission) =>
        (Permissions & DiscordPermissions.Administrator) == DiscordPermissions.Administrator ||
        (Permissions & permission) == permission;

    public bool IsAbove(DiscordRole other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Position != other.Position
            ? Position > other.Position
            : Id.Value < other.Id.Value;
    }

    public override string ToString() => $"{Name} ({Id.Value.ToString(CultureInfo.InvariantCulture)})";
}

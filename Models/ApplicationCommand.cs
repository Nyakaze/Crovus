using System.Globalization;
using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public enum ApplicationCommandType
{
    ChatInput = 1,
    User = 2,
    Message = 3,
    PrimaryEntryPoint = 4
}

public enum ApplicationCommandOptionType
{
    SubCommand = 1,
    SubCommandGroup = 2,
    String = 3,
    Integer = 4,
    Boolean = 5,
    User = 6,
    Channel = 7,
    Role = 8,
    Mentionable = 9,
    Number = 10,
    Attachment = 11
}

public enum InteractionContextType
{
    Guild = 0,
    BotDm = 1,
    PrivateChannel = 2
}

public enum ApplicationIntegrationType
{
    GuildInstall = 0,
    UserInstall = 1
}

public sealed record DiscordApplicationCommandChoice(string Name, object Value)
{
    public static DiscordApplicationCommandChoice Text(string name, string value) => new(name, value);

    public static DiscordApplicationCommandChoice Integer(string name, long value) => new(name, value);

    public static DiscordApplicationCommandChoice Number(string name, double value) => new(name, value);

    public string AsText => Value as string ?? Convert.ToString(Value, CultureInfo.InvariantCulture)!;

    public long AsInteger => Convert.ToInt64(Value, CultureInfo.InvariantCulture);

    public double AsNumber => Convert.ToDouble(Value, CultureInfo.InvariantCulture);

    public bool Matches(ApplicationCommandOptionType type) => type switch
    {
        ApplicationCommandOptionType.String => Value is string,
        ApplicationCommandOptionType.Integer => Value is byte or short or int or long,
        ApplicationCommandOptionType.Number => Value is float or double or decimal or byte or short or int or long,
        _ => false
    };
}

public sealed record DiscordApplicationCommandOption(ApplicationCommandOptionType Type, string Name,
    string Description)
{
    public bool Required { get; init; }
    public IReadOnlyList<DiscordApplicationCommandChoice>? Choices { get; init; }
    public IReadOnlyList<DiscordApplicationCommandOption>? Options { get; init; }
    public IReadOnlyList<ChannelType>? ChannelTypes { get; init; }
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public bool? Autocomplete { get; init; }

    [JsonIgnore]
    public bool IsSubCommand =>
        Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup;
}

public sealed record DiscordApplicationCommand(Snowflake Id, ApplicationCommandType Type, Snowflake ApplicationId,
    Snowflake? GuildId, string Name, string Description, IReadOnlyList<DiscordApplicationCommandOption>? Options,
    DiscordPermissions? DefaultMemberPermissions, bool Nsfw, string Version,
    IReadOnlyList<ApplicationIntegrationType>? IntegrationTypes, IReadOnlyList<InteractionContextType>? Contexts) : IBoundEntity
{
    [JsonIgnore]
    public bool IsGlobal => GuildId is null;

    [JsonIgnore]
    public string Mention => $"</{Name}:{Id.Value}>";

    private EntityBinding _binding;

    public DiscordApplicationCommand Bind(ICrovusContext context)
    {
        var bound = this with { };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}

public sealed record ApplicationCommandRequest(string Name, ApplicationCommandType Type)
{
    public string? Description { get; init; }
    public IReadOnlyList<DiscordApplicationCommandOption>? Options { get; init; }
    public DiscordPermissions? DefaultMemberPermissions { get; init; }
    public bool? Nsfw { get; init; }
    public IReadOnlyList<ApplicationIntegrationType>? IntegrationTypes { get; init; }
    public IReadOnlyList<InteractionContextType>? Contexts { get; init; }
}

public enum CommandPermissionTarget
{
    Role = 1,
    User = 2,
    Channel = 3
}

public sealed record DiscordCommandPermission(Snowflake Id, CommandPermissionTarget Target, bool Allowed);

public sealed record DiscordCommandPermissions
{
    public required Snowflake CommandId { get; init; }

    public required Snowflake ApplicationId { get; init; }

    public required Snowflake GuildId { get; init; }

    public IReadOnlyList<DiscordCommandPermission> Permissions { get; init; } = [];

    public bool IsApplicationWide => CommandId == ApplicationId;

    public bool EveryoneAllowed =>
        Permissions.FirstOrDefault(permission =>
            permission.Target is CommandPermissionTarget.Role && permission.Id == GuildId)?.Allowed ?? true;

    public bool AllowsRole(Snowflake roleId) => Allows(CommandPermissionTarget.Role, roleId);

    public bool AllowsUser(Snowflake userId) => Allows(CommandPermissionTarget.User, userId);

    public bool AllowsChannel(Snowflake channelId) => Allows(CommandPermissionTarget.Channel, channelId);

    private bool Allows(CommandPermissionTarget target, Snowflake id) =>
        Permissions.FirstOrDefault(permission => permission.Target == target && permission.Id == id)?.Allowed ?? true;
}

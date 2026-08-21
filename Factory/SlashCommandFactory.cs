using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Factory;

public sealed class SlashCommandFactory
{
    private readonly List<CommandOptionFactory> _options = [];
    private readonly List<InteractionContextType> _contexts = [];
    private readonly List<ApplicationIntegrationType> _integrations = [];

    private string _name;
    private string _description;
    private DiscordPermissions? _permissions;
    private bool? _nsfw;

    private SlashCommandFactory(ApplicationCommandType type, string name, string description)
    {
        Type = type;
        _name = ValidateName(type, name);
        _description = description;
    }

    public ApplicationCommandType Type { get; }

    public static SlashCommandFactory Slash(string name, string description) =>
        new(ApplicationCommandType.ChatInput, name,
            Limit.Required(description, DiscordLimits.CommandDescription, nameof(description)));

    public static SlashCommandFactory UserCommand(string name) =>
        new(ApplicationCommandType.User, name, string.Empty);

    public static SlashCommandFactory MessageCommand(string name) =>
        new(ApplicationCommandType.Message, name, string.Empty);

    public int OptionCount => _options.Count;

    public SlashCommandFactory WithName(string name)
    {
        _name = ValidateName(Type, name);
        return this;
    }

    public SlashCommandFactory WithDescription(string description)
    {
        if (Type is not ApplicationCommandType.ChatInput)
            throw new InvalidOperationException($"A {Type} command must not carry a description.");

        _description = Limit.Required(description, DiscordLimits.CommandDescription, nameof(description));

        return this;
    }

    public SlashCommandFactory RequiringPermissions(DiscordPermissions permissions)
    {
        _permissions = permissions;
        return this;
    }

    public SlashCommandFactory AsNsfw(bool nsfw = true)
    {
        _nsfw = nsfw;
        return this;
    }

    public SlashCommandFactory InContexts(params InteractionContextType[] contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        _contexts.Clear();
        _contexts.AddRange(contexts.Distinct());

        return this;
    }

    public SlashCommandFactory GuildOnly() => InContexts(InteractionContextType.Guild);

    public SlashCommandFactory WithIntegrations(params ApplicationIntegrationType[] integrations)
    {
        ArgumentNullException.ThrowIfNull(integrations);

        _integrations.Clear();
        _integrations.AddRange(integrations.Distinct());

        return this;
    }

    public SlashCommandFactory AddOption(CommandOptionFactory option)
    {
        ArgumentNullException.ThrowIfNull(option);

        if (Type is not ApplicationCommandType.ChatInput)
            throw new InvalidOperationException($"A {Type} command cannot carry options.");

        Limit.Count(_options.Count + 1, DiscordLimits.CommandOptions, nameof(option));
        _options.Add(option);

        return this;
    }

    public SlashCommandFactory AddText(string name, string description, bool required = false,
        Action<CommandOptionFactory>? configure = null) =>
        AddConfigured(CommandOptionFactory.Text(name, description), required, configure);

    public SlashCommandFactory AddInteger(string name, string description, bool required = false,
        Action<CommandOptionFactory>? configure = null) =>
        AddConfigured(CommandOptionFactory.Integer(name, description), required, configure);

    public SlashCommandFactory AddNumber(string name, string description, bool required = false,
        Action<CommandOptionFactory>? configure = null) =>
        AddConfigured(CommandOptionFactory.Number(name, description), required, configure);

    public SlashCommandFactory AddBoolean(string name, string description, bool required = false) =>
        AddConfigured(CommandOptionFactory.Boolean(name, description), required, null);

    public SlashCommandFactory AddUser(string name, string description, bool required = false) =>
        AddConfigured(CommandOptionFactory.User(name, description), required, null);

    public SlashCommandFactory AddChannel(string name, string description, bool required = false,
        Action<CommandOptionFactory>? configure = null) =>
        AddConfigured(CommandOptionFactory.Channel(name, description), required, configure);

    public SlashCommandFactory AddRole(string name, string description, bool required = false) =>
        AddConfigured(CommandOptionFactory.Role(name, description), required, null);

    public SlashCommandFactory AddMentionable(string name, string description, bool required = false) =>
        AddConfigured(CommandOptionFactory.Mentionable(name, description), required, null);

    public SlashCommandFactory AddAttachment(string name, string description, bool required = false) =>
        AddConfigured(CommandOptionFactory.Attachment(name, description), required, null);

    public SlashCommandFactory AddSubCommand(string name, string description,
        Action<CommandOptionFactory>? configure = null) =>
        AddConfigured(CommandOptionFactory.SubCommand(name, description), false, configure);

    public SlashCommandFactory AddSubCommandGroup(string name, string description,
        Action<CommandOptionFactory>? configure = null) =>
        AddConfigured(CommandOptionFactory.SubCommandGroup(name, description), false, configure);

    public ApplicationCommandRequest Build()
    {
        var options = _options.Select(option => option.Build()).ToArray();

        CommandNames.RequireUnique(options.Select(option => option.Name), "option");
        CommandNames.RequireRequiredFirst(options);

        var total = _name.Length + _description.Length + _options.Sum(option => option.TotalLength);

        if (total > DiscordLimits.CommandTotal)
            throw new InvalidOperationException(
                $"Command {_name} spans {total} characters but Discord accepts at most {DiscordLimits.CommandTotal}.");

        if (Type is ApplicationCommandType.ChatInput && _description.Length == 0)
            throw new InvalidOperationException("A slash command needs a description.");

        return new ApplicationCommandRequest(_name, Type)
        {
            Description = Type is ApplicationCommandType.ChatInput ? _description : null,
            Options = options.Length == 0 ? null : options,
            DefaultMemberPermissions = _permissions,
            Nsfw = _nsfw,
            IntegrationTypes = _integrations.Count == 0 ? null : _integrations.ToArray(),
            Contexts = _contexts.Count == 0 ? null : _contexts.ToArray()
        };
    }

    public Task<DiscordApplicationCommand> RegisterAsync(IDiscordRest rest, Snowflake applicationId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.CreateApplicationCommandAsync(applicationId, Build(), guildId, cancellationToken);
    }

    public Task<DiscordApplicationCommand> ApplyAsync(IDiscordRest rest, Snowflake applicationId, Snowflake commandId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.EditApplicationCommandAsync(applicationId, commandId, Build(), guildId, cancellationToken);
    }

    public static Task<IReadOnlyList<DiscordApplicationCommand>> DeployAsync(IDiscordRest rest,
        Snowflake applicationId, IEnumerable<SlashCommandFactory> commands, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);
        ArgumentNullException.ThrowIfNull(commands);

        var requests = commands.Select(command => command.Build()).ToArray();

        CommandNames.RequireUnique(requests.Select(request => request.Name), "command");

        return rest.SetApplicationCommandsAsync(applicationId, requests, guildId, cancellationToken);
    }

    private SlashCommandFactory AddConfigured(CommandOptionFactory option, bool required,
        Action<CommandOptionFactory>? configure)
    {
        if (required)
            option.AsRequired();

        configure?.Invoke(option);

        return AddOption(option);
    }

    private static string ValidateName(ApplicationCommandType type, string name)
    {
        if (type is ApplicationCommandType.ChatInput)
            return CommandNames.Validate(name, nameof(name));

        return Limit.Required(name, DiscordLimits.CommandName, nameof(name));
    }
}

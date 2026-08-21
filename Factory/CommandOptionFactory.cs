using Crovus.Models;

namespace Crovus.Factory;

public sealed class CommandOptionFactory
{
    private readonly List<DiscordApplicationCommandChoice> _choices = [];
    private readonly List<CommandOptionFactory> _options = [];
    private readonly List<ChannelType> _channelTypes = [];

    private string _name;
    private string _description;
    private bool _required;
    private bool _autocomplete;
    private double? _minValue;
    private double? _maxValue;
    private int? _minLength;
    private int? _maxLength;

    private CommandOptionFactory(ApplicationCommandOptionType type, string name, string description)
    {
        Type = type;
        _name = CommandNames.Validate(name, nameof(name));
        _description = Limit.Required(description, DiscordLimits.CommandDescription, nameof(description));
    }

    public ApplicationCommandOptionType Type { get; }

    public static CommandOptionFactory Text(string name, string description) =>
        new(ApplicationCommandOptionType.String, name, description);

    public static CommandOptionFactory Integer(string name, string description) =>
        new(ApplicationCommandOptionType.Integer, name, description);

    public static CommandOptionFactory Number(string name, string description) =>
        new(ApplicationCommandOptionType.Number, name, description);

    public static CommandOptionFactory Boolean(string name, string description) =>
        new(ApplicationCommandOptionType.Boolean, name, description);

    public static CommandOptionFactory User(string name, string description) =>
        new(ApplicationCommandOptionType.User, name, description);

    public static CommandOptionFactory Channel(string name, string description) =>
        new(ApplicationCommandOptionType.Channel, name, description);

    public static CommandOptionFactory Role(string name, string description) =>
        new(ApplicationCommandOptionType.Role, name, description);

    public static CommandOptionFactory Mentionable(string name, string description) =>
        new(ApplicationCommandOptionType.Mentionable, name, description);

    public static CommandOptionFactory Attachment(string name, string description) =>
        new(ApplicationCommandOptionType.Attachment, name, description);

    public static CommandOptionFactory SubCommand(string name, string description) =>
        new(ApplicationCommandOptionType.SubCommand, name, description);

    public static CommandOptionFactory SubCommandGroup(string name, string description) =>
        new(ApplicationCommandOptionType.SubCommandGroup, name, description);

    public CommandOptionFactory WithName(string name)
    {
        _name = CommandNames.Validate(name, nameof(name));
        return this;
    }

    public CommandOptionFactory WithDescription(string description)
    {
        _description = Limit.Required(description, DiscordLimits.CommandDescription, nameof(description));
        return this;
    }

    public CommandOptionFactory AsRequired(bool required = true)
    {
        if (required && Type is ApplicationCommandOptionType.SubCommand
                or ApplicationCommandOptionType.SubCommandGroup)
            throw new InvalidOperationException($"A {Type} option cannot be marked required.");

        _required = required;

        return this;
    }

    public CommandOptionFactory WithChoice(string name, string value)
    {
        Limit.Text(value, DiscordLimits.CommandChoiceValue, nameof(value));
        return AddChoice(name, value);
    }

    public CommandOptionFactory WithChoice(string name, long value) => AddChoice(name, value);

    public CommandOptionFactory WithChoice(string name, double value) => AddChoice(name, value);

    public CommandOptionFactory WithChoices(IEnumerable<DiscordApplicationCommandChoice> choices)
    {
        ArgumentNullException.ThrowIfNull(choices);

        foreach (var choice in choices)
            AddChoice(choice.Name, choice.Value);

        return this;
    }

    public CommandOptionFactory WithAutocomplete(bool autocomplete = true)
    {
        _autocomplete = autocomplete;
        return this;
    }

    public CommandOptionFactory WithRange(double? minimum, double? maximum)
    {
        _minValue = minimum;
        _maxValue = maximum;

        return this;
    }

    public CommandOptionFactory WithLength(int? minimum, int? maximum)
    {
        _minLength = minimum;
        _maxLength = maximum;

        return this;
    }

    public CommandOptionFactory WithChannelTypes(params ChannelType[] channelTypes)
    {
        ArgumentNullException.ThrowIfNull(channelTypes);

        _channelTypes.Clear();
        _channelTypes.AddRange(channelTypes);

        return this;
    }

    public CommandOptionFactory AddOption(CommandOptionFactory option)
    {
        ArgumentNullException.ThrowIfNull(option);
        Limit.Count(_options.Count + 1, DiscordLimits.CommandOptions, nameof(option));

        _options.Add(option);

        return this;
    }

    public CommandOptionFactory AddSubCommand(string name, string description,
        Action<CommandOptionFactory>? configure = null)
    {
        var option = SubCommand(name, description);
        configure?.Invoke(option);

        return AddOption(option);
    }

    public DiscordApplicationCommandOption Build()
    {
        Validate();

        return new DiscordApplicationCommandOption(Type, _name, _description)
        {
            Required = _required,
            Choices = _choices.Count == 0 ? null : _choices.ToArray(),
            Options = _options.Count == 0 ? null : BuildOptions(),
            ChannelTypes = _channelTypes.Count == 0 ? null : _channelTypes.ToArray(),
            MinValue = _minValue,
            MaxValue = _maxValue,
            MinLength = _minLength,
            MaxLength = _maxLength,
            Autocomplete = _autocomplete ? true : null
        };
    }

    internal int TotalLength =>
        _name.Length + _description.Length +
        _choices.Sum(choice => choice.Name.Length + (choice.Value is string text ? text.Length : 0)) +
        _options.Sum(option => option.TotalLength);

    private CommandOptionFactory AddChoice(string name, object value)
    {
        if (Type is not (ApplicationCommandOptionType.String or ApplicationCommandOptionType.Integer
            or ApplicationCommandOptionType.Number))
            throw new InvalidOperationException($"A {Type} option cannot carry choices.");

        Limit.Required(name, DiscordLimits.CommandChoiceName, nameof(name));
        Limit.Count(_choices.Count + 1, DiscordLimits.CommandChoices, nameof(name));

        var choice = new DiscordApplicationCommandChoice(name, value);

        if (!choice.Matches(Type))
            throw new ArgumentException($"A {Type} option cannot hold a {value.GetType().Name} choice.", nameof(value));

        _choices.Add(choice);

        return this;
    }

    private DiscordApplicationCommandOption[] BuildOptions()
    {
        var built = _options.Select(option => option.Build()).ToArray();

        CommandNames.RequireUnique(built.Select(option => option.Name), "option");
        CommandNames.RequireRequiredFirst(built);

        return built;
    }

    private void Validate()
    {
        if (_autocomplete && _choices.Count > 0)
            throw new InvalidOperationException(
                $"Option {_name} cannot offer both autocomplete and a fixed choice list.");

        if (_autocomplete && Type is not (ApplicationCommandOptionType.String or ApplicationCommandOptionType.Integer
                or ApplicationCommandOptionType.Number))
            throw new InvalidOperationException($"A {Type} option does not support autocomplete.");

        if ((_minValue is not null || _maxValue is not null) &&
            Type is not (ApplicationCommandOptionType.Integer or ApplicationCommandOptionType.Number))
            throw new InvalidOperationException($"A {Type} option does not support a value range.");

        if (_minValue is { } min && _maxValue is { } max && min > max)
            throw new InvalidOperationException($"Option {_name} has a minimum above its maximum.");

        if ((_minLength is not null || _maxLength is not null) && Type is not ApplicationCommandOptionType.String)
            throw new InvalidOperationException($"A {Type} option does not support a length range.");

        Limit.Range(_minLength, 0, DiscordLimits.CommandStringLength, "minLength");
        Limit.Range(_maxLength, 1, DiscordLimits.CommandStringLength, "maxLength");

        if (_minLength is { } minLength && _maxLength is { } maxLength && minLength > maxLength)
            throw new InvalidOperationException($"Option {_name} has a minimum length above its maximum length.");

        if (_channelTypes.Count > 0 && Type is not ApplicationCommandOptionType.Channel)
            throw new InvalidOperationException($"A {Type} option does not support a channel type filter.");

        if (_options.Count > 0 && Type is not (ApplicationCommandOptionType.SubCommand
                or ApplicationCommandOptionType.SubCommandGroup))
            throw new InvalidOperationException($"A {Type} option cannot nest further options.");

        foreach (var nested in _options)
        {
            switch (Type)
            {
                case ApplicationCommandOptionType.SubCommandGroup
                    when nested.Type is not ApplicationCommandOptionType.SubCommand:
                    throw new InvalidOperationException(
                        $"A subcommand group holds subcommands only but {_name} holds a {nested.Type}.");

                case ApplicationCommandOptionType.SubCommand when nested.Type is
                    ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup:
                    throw new InvalidOperationException(
                        $"A subcommand cannot nest another {nested.Type}.");
            }
        }
    }
}

internal static class CommandNames
{
    public static string Validate(string name, string field)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"{field} must not be empty.", field);

        if (name.Length > DiscordLimits.CommandName)
            throw new ArgumentException(
                $"{field} must be at most {DiscordLimits.CommandName} characters but was {name.Length}.", field);

        if (name != name.ToLowerInvariant())
            throw new ArgumentException($"{field} must be lowercase but was \"{name}\".", field);

        foreach (var character in name)
        {
            if (!char.IsLetterOrDigit(character) && character != '_' && character != '-')
                throw new ArgumentException(
                    $"{field} accepts letters, digits, underscores and hyphens only but contained '{character}'.",
                    field);
        }

        return name;
    }

    public static void RequireUnique(IEnumerable<string> names, string what)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            if (!seen.Add(name))
                throw new InvalidOperationException($"Duplicate {what} name \"{name}\".");
        }
    }

    public static void RequireRequiredFirst(IReadOnlyList<DiscordApplicationCommandOption> options)
    {
        var seenOptional = false;

        foreach (var option in options)
        {
            if (option.Required)
            {
                if (seenOptional)
                    throw new InvalidOperationException(
                        $"Required option \"{option.Name}\" must be declared before every optional option.");
            }
            else
            {
                seenOptional = true;
            }
        }
    }
}

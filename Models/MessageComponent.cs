namespace Crovus.Models;

public enum ButtonStyle
{
    Primary = 1,
    Secondary = 2,
    Success = 3,
    Danger = 4,
    Link = 5,
    Premium = 6
}

public enum TextInputStyle
{
    Short = 1,
    Paragraph = 2
}

public enum SelectKind
{
    String = 3,
    User = 5,
    Role = 6,
    Mentionable = 7,
    Channel = 8
}

public enum SelectDefaultValueType
{
    User,
    Role,
    Channel
}

public abstract record DiscordComponent
{
    public abstract MessageComponentType Type { get; }

    public virtual string? CustomId => null;

    public virtual IEnumerable<DiscordComponent> Descendants() => [];

    public IEnumerable<DiscordComponent> Flatten()
    {
        yield return this;

        foreach (var child in Descendants())
        foreach (var nested in child.Flatten())
            yield return nested;
    }

    public DiscordComponent? Find(string customId) =>
        Flatten().FirstOrDefault(component =>
            string.Equals(component.CustomId, customId, StringComparison.Ordinal));
}

public sealed record DiscordActionRow : DiscordComponent
{
    public override MessageComponentType Type => MessageComponentType.ActionRow;

    public IReadOnlyList<DiscordComponent> Components { get; init; } = [];

    public bool IsEmpty => Components.Count == 0;

    public override IEnumerable<DiscordComponent> Descendants() => Components;

    public static DiscordActionRow Of(params DiscordComponent[] components) =>
        new() { Components = components };

    public static DiscordActionRow Of(IEnumerable<DiscordComponent> components) =>
        new() { Components = [.. components] };
}

public sealed record DiscordButton : DiscordComponent
{
    public override MessageComponentType Type => MessageComponentType.Button;

    public ButtonStyle Style { get; init; } = ButtonStyle.Primary;

    public string? Label { get; init; }

    public DiscordEmoji? Emoji { get; init; }

    public string? Id { get; init; }

    public string? Url { get; init; }

    public Snowflake? SkuId { get; init; }

    public bool Disabled { get; init; }

    public override string? CustomId => Id;

    public bool IsLink => Style is ButtonStyle.Link;

    public bool IsPremium => Style is ButtonStyle.Premium;

    public static DiscordButton Primary(string customId, string label) =>
        new() { Style = ButtonStyle.Primary, Id = customId, Label = label };

    public static DiscordButton Secondary(string customId, string label) =>
        new() { Style = ButtonStyle.Secondary, Id = customId, Label = label };

    public static DiscordButton Success(string customId, string label) =>
        new() { Style = ButtonStyle.Success, Id = customId, Label = label };

    public static DiscordButton Danger(string customId, string label) =>
        new() { Style = ButtonStyle.Danger, Id = customId, Label = label };

    public static DiscordButton Link(string url, string label) =>
        new() { Style = ButtonStyle.Link, Url = url, Label = label };

    public static DiscordButton Premium(Snowflake skuId) =>
        new() { Style = ButtonStyle.Premium, SkuId = skuId };

    public DiscordButton With(DiscordEmoji emoji) => this with { Emoji = emoji };

    public DiscordButton AsDisabled(bool disabled = true) => this with { Disabled = disabled };
}

public sealed record DiscordSelectOption(string Label, string Value)
{
    public string? Description { get; init; }

    public DiscordEmoji? Emoji { get; init; }

    public bool Default { get; init; }

    public DiscordSelectOption Describing(string description) => this with { Description = description };

    public DiscordSelectOption With(DiscordEmoji emoji) => this with { Emoji = emoji };

    public DiscordSelectOption AsDefault(bool selected = true) => this with { Default = selected };
}

public sealed record DiscordSelectDefaultValue(Snowflake Id, SelectDefaultValueType Type)
{
    public static DiscordSelectDefaultValue User(Snowflake id) => new(id, SelectDefaultValueType.User);

    public static DiscordSelectDefaultValue Role(Snowflake id) => new(id, SelectDefaultValueType.Role);

    public static DiscordSelectDefaultValue Channel(Snowflake id) => new(id, SelectDefaultValueType.Channel);
}

public sealed record DiscordSelectMenu : DiscordComponent
{
    public SelectKind Kind { get; init; } = SelectKind.String;

    public override MessageComponentType Type => (MessageComponentType)Kind;

    public required string Id { get; init; }

    public string? Placeholder { get; init; }

    public IReadOnlyList<DiscordSelectOption> Options { get; init; } = [];

    public IReadOnlyList<ChannelType> ChannelTypes { get; init; } = [];

    public IReadOnlyList<DiscordSelectDefaultValue> DefaultValues { get; init; } = [];

    public int? MinValues { get; init; }

    public int? MaxValues { get; init; }

    public bool Disabled { get; init; }

    public override string? CustomId => Id;

    public bool IsStringSelect => Kind is SelectKind.String;

    public bool IsAutoPopulated => Kind is not SelectKind.String;

    public static DiscordSelectMenu String(string customId, params DiscordSelectOption[] options) =>
        new() { Kind = SelectKind.String, Id = customId, Options = options };

    public static DiscordSelectMenu Users(string customId) =>
        new() { Kind = SelectKind.User, Id = customId };

    public static DiscordSelectMenu Roles(string customId) =>
        new() { Kind = SelectKind.Role, Id = customId };

    public static DiscordSelectMenu Mentionables(string customId) =>
        new() { Kind = SelectKind.Mentionable, Id = customId };

    public static DiscordSelectMenu Channels(string customId, params ChannelType[] channelTypes) =>
        new() { Kind = SelectKind.Channel, Id = customId, ChannelTypes = channelTypes };

    public DiscordSelectMenu Prompting(string placeholder) => this with { Placeholder = placeholder };

    public DiscordSelectMenu Choosing(int min, int max) => this with { MinValues = min, MaxValues = max };

    public DiscordSelectMenu AsDisabled(bool disabled = true) => this with { Disabled = disabled };
}

public sealed record DiscordTextInput : DiscordComponent
{
    public override MessageComponentType Type => MessageComponentType.TextInput;

    public required string Id { get; init; }

    public required string Label { get; init; }

    public TextInputStyle Style { get; init; } = TextInputStyle.Short;

    public string? Value { get; init; }

    public string? Placeholder { get; init; }

    public int? MinLength { get; init; }

    public int? MaxLength { get; init; }

    public bool Required { get; init; } = true;

    public override string? CustomId => Id;

    public static DiscordTextInput Short(string customId, string label) =>
        new() { Id = customId, Label = label, Style = TextInputStyle.Short };

    public static DiscordTextInput Paragraph(string customId, string label) =>
        new() { Id = customId, Label = label, Style = TextInputStyle.Paragraph };

    public DiscordTextInput Prefilled(string value) => this with { Value = value };

    public DiscordTextInput Prompting(string placeholder) => this with { Placeholder = placeholder };

    public DiscordTextInput Between(int min, int max) => this with { MinLength = min, MaxLength = max };

    public DiscordTextInput AsOptional(bool optional = true) => this with { Required = !optional };
}

public sealed record DiscordModal
{
    public required string CustomId { get; init; }

    public required string Title { get; init; }

    public IReadOnlyList<DiscordComponent> Components { get; init; } = [];

    public IEnumerable<DiscordTextInput> Inputs =>
        Components.SelectMany(component => component.Flatten()).OfType<DiscordTextInput>();

    public static DiscordModal Of(string customId, string title, params DiscordTextInput[] inputs) =>
        new()
        {
            CustomId = customId,
            Title = title,
            Components = [.. inputs.Select(input => DiscordActionRow.Of(input))]
        };
}

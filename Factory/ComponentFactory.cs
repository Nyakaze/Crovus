using Crovus.Models;

namespace Crovus.Factory;

public sealed class ComponentFactory
{
    private readonly List<DiscordActionRow> _rows = [];
    private readonly List<DiscordComponent> _pending = [];

    public static ComponentFactory Create() => new();

    public static ComponentFactory From(DiscordMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var factory = new ComponentFactory();
        factory._rows.AddRange(message.Components.OfType<DiscordActionRow>());

        return factory;
    }

    public int RowCount => _rows.Count + (_pending.Count == 0 ? 0 : 1);

    public bool IsEmpty => RowCount == 0;

    public ComponentFactory AddButton(DiscordButton button)
    {
        ArgumentNullException.ThrowIfNull(button);
        ComponentLimit.Button(button, nameof(button));

        if (_pending.Count == DiscordLimits.ButtonsPerRow)
            NewRow();

        _pending.Add(button);

        return this;
    }

    public ComponentFactory AddButton(string customId, string label, ButtonStyle style = ButtonStyle.Primary) =>
        AddButton(new DiscordButton { Id = customId, Label = label, Style = style });

    public ComponentFactory AddButtons(IEnumerable<DiscordButton> buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);

        foreach (var button in buttons)
            AddButton(button);

        return this;
    }

    public ComponentFactory AddLink(string url, string label) => AddButton(DiscordButton.Link(url, label));

    public ComponentFactory AddSelect(DiscordSelectMenu select)
    {
        ArgumentNullException.ThrowIfNull(select);
        ComponentLimit.Select(select, nameof(select));

        NewRow();
        _pending.Add(select);

        return NewRow();
    }

    public ComponentFactory AddSelect(string customId, params DiscordSelectOption[] options) =>
        AddSelect(DiscordSelectMenu.String(customId, options));

    public ComponentFactory AddUserSelect(string customId, string? placeholder = null) =>
        AddSelect(Prompted(DiscordSelectMenu.Users(customId), placeholder));

    public ComponentFactory AddRoleSelect(string customId, string? placeholder = null) =>
        AddSelect(Prompted(DiscordSelectMenu.Roles(customId), placeholder));

    public ComponentFactory AddMentionableSelect(string customId, string? placeholder = null) =>
        AddSelect(Prompted(DiscordSelectMenu.Mentionables(customId), placeholder));

    public ComponentFactory AddChannelSelect(string customId, params ChannelType[] channelTypes) =>
        AddSelect(DiscordSelectMenu.Channels(customId, channelTypes));

    public ComponentFactory AddRow(params DiscordComponent[] components)
    {
        ArgumentNullException.ThrowIfNull(components);

        return AddRow(DiscordActionRow.Of(components));
    }

    public ComponentFactory AddRow(DiscordActionRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        ComponentLimit.Row(row, nameof(row));

        NewRow();
        Limit.Count(_rows.Count + 1, DiscordLimits.MessageActionRows, nameof(row));
        _rows.Add(row);

        return this;
    }

    public ComponentFactory NewRow()
    {
        if (_pending.Count == 0)
            return this;

        Limit.Count(_rows.Count + 1, DiscordLimits.MessageActionRows, nameof(NewRow));
        _rows.Add(DiscordActionRow.Of(_pending));
        _pending.Clear();

        return this;
    }

    public ComponentFactory Clear()
    {
        _rows.Clear();
        _pending.Clear();

        return this;
    }

    public IReadOnlyList<DiscordComponent> Build()
    {
        NewRow();

        return [.. _rows];
    }

    private static DiscordSelectMenu Prompted(DiscordSelectMenu select, string? placeholder) =>
        placeholder is null ? select : select.Prompting(placeholder);
}

internal static class ComponentLimit
{
    public static void Rows(IReadOnlyList<DiscordComponent>? components, string field)
    {
        if (components is null)
            return;

        Limit.Count(components.Count, DiscordLimits.MessageActionRows, field);

        foreach (var component in components)
        {
            if (component is not DiscordActionRow row)
                throw new ArgumentException(
                    $"{field} must hold action rows at the top level but held a {component.Type}.", field);

            Row(row, field);
        }
    }

    public static void Row(DiscordActionRow row, string field)
    {
        var buttons = 0;

        foreach (var component in row.Components)
            switch (component)
            {
                case DiscordButton button:
                    Button(button, field);
                    buttons++;
                    break;

                case DiscordSelectMenu select when row.Components.Count == 1:
                    Select(select, field);
                    break;

                case DiscordSelectMenu:
                    throw new ArgumentException(
                        $"{field} seats a select menu beside {row.Components.Count - 1} other component(s) but a " +
                        "select menu owns its row.", field);

                default:
                    throw new ArgumentException(
                        $"{field} holds a {component.Type} which a message action row does not accept.", field);
            }

        Limit.Count(buttons, DiscordLimits.ButtonsPerRow, field);
    }

    public static void Button(DiscordButton button, string field)
    {
        Limit.Text(button.Label, DiscordLimits.ButtonLabel, field);

        switch (button.Style)
        {
            case ButtonStyle.Link when string.IsNullOrWhiteSpace(button.Url):
                throw new ArgumentException($"{field} holds a link button without a url.", field);

            case ButtonStyle.Premium when button.SkuId is null:
                throw new ArgumentException($"{field} holds a premium button without a sku id.", field);

            case ButtonStyle.Link or ButtonStyle.Premium:
                break;

            default:
                Limit.Required(button.Id, DiscordLimits.ComponentCustomId, field);

                if (string.IsNullOrWhiteSpace(button.Label) && button.Emoji is null)
                    throw new ArgumentException($"{field} holds a button without a label or emoji.", field);

                break;
        }
    }

    public static void Select(DiscordSelectMenu select, string field)
    {
        Limit.Required(select.Id, DiscordLimits.ComponentCustomId, field);
        Limit.Text(select.Placeholder, DiscordLimits.SelectPlaceholder, field);
        Limit.Count(select.Options.Count, DiscordLimits.SelectOptions, field);
        Limit.Range(select.MinValues, 0, DiscordLimits.SelectOptions, field);
        Limit.Range(select.MaxValues, 1, DiscordLimits.SelectOptions, field);

        if (select.IsStringSelect && select.Options.Count == 0)
            throw new ArgumentException($"{field} holds a string select without options.", field);

        if (select.IsAutoPopulated && select.Options.Count > 0)
            throw new ArgumentException(
                $"{field} gives a {select.Kind} select its own options but only string selects carry options.", field);

        foreach (var option in select.Options)
        {
            Limit.Required(option.Label, DiscordLimits.SelectOptionLabel, field);
            Limit.Required(option.Value, DiscordLimits.SelectOptionValue, field);
            Limit.Text(option.Description, DiscordLimits.SelectOptionDescription, field);
        }
    }

    public static void Modal(DiscordModal modal, string field)
    {
        Limit.Required(modal.CustomId, DiscordLimits.ComponentCustomId, field);
        Limit.Required(modal.Title, DiscordLimits.ModalTitle, field);
        Limit.Count(modal.Components.Count, DiscordLimits.ModalInputs, field);

        if (modal.Components.Count == 0)
            throw new ArgumentException($"{field} holds a modal without inputs.", field);

        foreach (var component in modal.Components)
        {
            if (component is not DiscordActionRow row)
                throw new ArgumentException(
                    $"{field} must hold action rows at the top level but held a {component.Type}.", field);

            if (row.Components is not [DiscordTextInput input])
                throw new ArgumentException(
                    $"{field} holds a modal row carrying {row.Components.Count} components but a modal row carries " +
                    "exactly one text input.", field);

            TextInput(input, field);
        }
    }

    public static void TextInput(DiscordTextInput input, string field)
    {
        Limit.Required(input.Id, DiscordLimits.ComponentCustomId, field);
        Limit.Required(input.Label, DiscordLimits.TextInputLabel, field);
        Limit.Text(input.Value, DiscordLimits.TextInputValue, field);
        Limit.Text(input.Placeholder, DiscordLimits.TextInputPlaceholder, field);
        Limit.Range(input.MinLength, 0, DiscordLimits.TextInputValue, field);
        Limit.Range(input.MaxLength, 1, DiscordLimits.TextInputValue, field);
    }
}

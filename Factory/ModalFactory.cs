using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Factory;

public sealed class ModalFactory
{
    private readonly List<DiscordTextInput> _inputs = [];

    private string _customId;
    private string _title;

    private ModalFactory(string customId, string title)
    {
        _customId = Limit.Required(customId, DiscordLimits.ComponentCustomId, nameof(customId));
        _title = Limit.Required(title, DiscordLimits.ModalTitle, nameof(title));
    }

    public static ModalFactory Create(string customId, string title) => new(customId, title);

    public int InputCount => _inputs.Count;

    public bool IsEmpty => _inputs.Count == 0;

    public ModalFactory WithCustomId(string customId)
    {
        _customId = Limit.Required(customId, DiscordLimits.ComponentCustomId, nameof(customId));

        return this;
    }

    public ModalFactory WithTitle(string title)
    {
        _title = Limit.Required(title, DiscordLimits.ModalTitle, nameof(title));

        return this;
    }

    public ModalFactory AddInput(DiscordTextInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ComponentLimit.TextInput(input, nameof(input));
        Limit.Count(_inputs.Count + 1, DiscordLimits.ModalInputs, nameof(input));

        _inputs.Add(input);

        return this;
    }

    public ModalFactory AddShort(string customId, string label, string? placeholder = null, bool required = true) =>
        AddInput(Shaped(DiscordTextInput.Short(customId, label), placeholder, required));

    public ModalFactory AddParagraph(string customId, string label, string? placeholder = null,
        bool required = true) =>
        AddInput(Shaped(DiscordTextInput.Paragraph(customId, label), placeholder, required));

    public ModalFactory ClearInputs()
    {
        _inputs.Clear();

        return this;
    }

    public DiscordModal Build()
    {
        var modal = DiscordModal.Of(_customId, _title, [.. _inputs]);
        ComponentLimit.Modal(modal, nameof(modal));

        return modal;
    }

    public InteractionResponseRequest BuildResponse() => InteractionResponseRequest.ShowModal(Build());

    public Task ShowAsync(IDiscordRest rest, DiscordInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);
        ArgumentNullException.ThrowIfNull(interaction);

        return rest.CreateInteractionResponseAsync(interaction.Id, interaction.Token, BuildResponse(),
            cancellationToken);
    }

    private static DiscordTextInput Shaped(DiscordTextInput input, string? placeholder, bool required)
    {
        var shaped = required ? input : input.AsOptional();

        return placeholder is null ? shaped : shaped.Prompting(placeholder);
    }
}

namespace Crovus.Models;

public sealed record InteractionMessageRequest(string? Content = null, IReadOnlyList<DiscordEmbed>? Embeds = null,
    bool Ephemeral = false, bool Tts = false, MessageFlags Flags = MessageFlags.None,
    IReadOnlyList<DiscordFile>? Files = null, IReadOnlyList<Snowflake>? KeptAttachments = null,
    IReadOnlyList<DiscordComponent>? Components = null)
{
    public MessageFlags EffectiveFlags => Ephemeral ? Flags | MessageFlags.Ephemeral : Flags;

    public bool HasFiles => Files is { Count: > 0 };

    public bool HasComponents => Components is { Count: > 0 };

    public InteractionMessageRequest Showing(params DiscordComponent[] components) =>
        this with { Components = [.. Components ?? [], .. components] };

    public InteractionMessageRequest WithoutComponents() => this with { Components = [] };

    public bool RewritesAttachments => HasFiles || KeptAttachments is not null;

    public bool IsEmpty =>
        string.IsNullOrEmpty(Content) && (Embeds is null || Embeds.Count == 0) && !HasFiles && !HasComponents;

    public InteractionMessageRequest Attaching(DiscordFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return this with { Files = [.. Files ?? [], file] };
    }

    public InteractionMessageRequest Keeping(IEnumerable<Snowflake> attachmentIds)
    {
        ArgumentNullException.ThrowIfNull(attachmentIds);

        return this with { KeptAttachments = [.. KeptAttachments ?? [], .. attachmentIds] };
    }

    public InteractionMessageRequest DroppingAttachments() => this with { KeptAttachments = [] };
}

public sealed record InteractionResponseRequest(InteractionCallbackType Type,
    InteractionMessageRequest? Message = null, IReadOnlyList<DiscordApplicationCommandChoice>? Choices = null,
    DiscordModal? Modal = null)
{
    public static InteractionResponseRequest Pong() => new(InteractionCallbackType.Pong);

    public static InteractionResponseRequest ShowModal(DiscordModal modal)
    {
        ArgumentNullException.ThrowIfNull(modal);

        return new InteractionResponseRequest(InteractionCallbackType.Modal, Modal: modal);
    }

    public static InteractionResponseRequest Reply(InteractionMessageRequest message) =>
        new(InteractionCallbackType.ChannelMessageWithSource, message);

    public static InteractionResponseRequest Reply(string content, bool ephemeral = false) =>
        Reply(new InteractionMessageRequest(content, Ephemeral: ephemeral));

    public static InteractionResponseRequest Defer(bool ephemeral = false) =>
        new(InteractionCallbackType.DeferredChannelMessageWithSource,
            ephemeral ? new InteractionMessageRequest(Ephemeral: true) : null);

    public static InteractionResponseRequest DeferUpdate() => new(InteractionCallbackType.DeferredUpdateMessage);

    public static InteractionResponseRequest Update(InteractionMessageRequest message) =>
        new(InteractionCallbackType.UpdateMessage, message);

    public static InteractionResponseRequest Autocomplete(IReadOnlyList<DiscordApplicationCommandChoice> choices) =>
        new(InteractionCallbackType.ApplicationCommandAutocompleteResult, Choices: choices);
}

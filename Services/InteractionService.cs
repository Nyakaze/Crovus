using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class InteractionService : DiscordService
{
    public InteractionService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Interaction", logger, telemetry)
    {
    }

    public InteractionService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task RespondAsync(DiscordInteraction interaction, InteractionResponseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interaction);
        ArgumentNullException.ThrowIfNull(request);

        Guard(interaction);

        return TrackAsync(nameof(RespondAsync), Describe(interaction),
            () => Rest.CreateInteractionResponseAsync(interaction.Id, interaction.Token, request, cancellationToken),
            $"Answered {Describe(interaction)} with {request.Type}");
    }

    public Task RespondAsync(DiscordInteraction interaction, string content, bool ephemeral = false,
        CancellationToken cancellationToken = default) =>
        RespondAsync(interaction, InteractionResponseRequest.Reply(content, ephemeral), cancellationToken);

    public Task RespondAsync(DiscordInteraction interaction, InteractionMessageRequest message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return RespondAsync(interaction, InteractionResponseRequest.Reply(message), cancellationToken);
    }

    public Task RespondAsync(DiscordInteraction interaction, Action<InteractionResponseFactory> configure,
        CancellationToken cancellationToken = default) =>
        RespondAsync(interaction, InteractionResponseRequest.Reply(Compose(configure)), cancellationToken);

    public Task RespondAsync(DiscordInteraction interaction, DiscordEmbed embed, bool ephemeral = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embed);

        return RespondAsync(interaction,
            new InteractionMessageRequest(Embeds: [embed], Ephemeral: ephemeral), cancellationToken);
    }

    public Task RespondAsync(DiscordInteraction interaction, DiscordFile file, string? content = null,
        bool ephemeral = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        return RespondAsync(interaction,
            InteractionResponseFactory.Create().WithContent(content).AddFile(file).AsEphemeral(ephemeral).Build(),
            cancellationToken);
    }

    public async Task RespondWithFileAsync(DiscordInteraction interaction, string path, string? content = null,
        bool ephemeral = false, CancellationToken cancellationToken = default)
    {
        var file = await DiscordFile.FromPathAsync(path, cancellationToken: cancellationToken);

        await RespondAsync(interaction, file, content, ephemeral, cancellationToken);
    }

    public Task DeferAsync(DiscordInteraction interaction, bool ephemeral = false,
        CancellationToken cancellationToken = default) =>
        RespondAsync(interaction, InteractionResponseRequest.Defer(ephemeral), cancellationToken);

    public Task DeferUpdateAsync(DiscordInteraction interaction, CancellationToken cancellationToken = default) =>
        RespondAsync(interaction, InteractionResponseRequest.DeferUpdate(), cancellationToken);

    public Task UpdateAsync(DiscordInteraction interaction, InteractionMessageRequest message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return RespondAsync(interaction, InteractionResponseRequest.Update(message), cancellationToken);
    }

    public Task UpdateAsync(DiscordInteraction interaction, Action<InteractionResponseFactory> configure,
        CancellationToken cancellationToken = default) =>
        RespondAsync(interaction, InteractionResponseRequest.Update(Compose(configure)), cancellationToken);

    public Task UpdateAsync(DiscordInteraction interaction, string content,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(interaction, new InteractionMessageRequest(content), cancellationToken);

    public Task ShowModalAsync(DiscordInteraction interaction, DiscordModal modal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modal);

        return RespondAsync(interaction, InteractionResponseRequest.ShowModal(modal), cancellationToken);
    }

    public Task ShowModalAsync(DiscordInteraction interaction, ModalFactory modal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modal);

        return ShowModalAsync(interaction, modal.Build(), cancellationToken);
    }

    public Task ShowModalAsync(DiscordInteraction interaction, string customId, string title,
        Action<ModalFactory> configure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var modal = ModalFactory.Create(customId, title);
        configure(modal);

        return ShowModalAsync(interaction, modal.Build(), cancellationToken);
    }

    public Task PongAsync(DiscordInteraction interaction, CancellationToken cancellationToken = default) =>
        RespondAsync(interaction, InteractionResponseRequest.Pong(), cancellationToken);

    public async Task AutocompleteAsync(DiscordInteraction interaction,
        IEnumerable<DiscordApplicationCommandChoice> choices, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interaction);
        ArgumentNullException.ThrowIfNull(choices);

        var payload = choices.ToArray();

        Limit.Count(payload.Length, DiscordLimits.CommandChoices, nameof(choices));
        Guard(interaction);

        await TrackAsync(nameof(AutocompleteAsync), Describe(interaction),
            () => Rest.CreateInteractionResponseAsync(interaction.Id, interaction.Token,
                InteractionResponseRequest.Autocomplete(payload), cancellationToken),
            $"Suggested {payload.Length} choices for {Describe(interaction)}", LogLevel.Debug);

        Emit(new InteractionAutocompleted(interaction.Id, interaction.FocusedOption?.Name ?? string.Empty,
            payload.Length));
    }

    public Task AutocompleteAsync(DiscordInteraction interaction, IEnumerable<string> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        return AutocompleteAsync(interaction,
            values.Select(value => DiscordApplicationCommandChoice.Text(value, value)), cancellationToken);
    }

    public Task<DiscordMessage> GetResponseAsync(DiscordInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interaction);

        return TrackAsync(nameof(GetResponseAsync), Describe(interaction),
            () => Rest.GetOriginalInteractionResponseAsync(interaction.ApplicationId, interaction.Token,
                cancellationToken),
            message => $"Loaded the response {message.Id} of {Describe(interaction)}", LogLevel.Debug);
    }

    public Task<DiscordMessage> EditResponseAsync(DiscordInteraction interaction, InteractionMessageRequest message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interaction);
        ArgumentNullException.ThrowIfNull(message);

        Guard(interaction);

        return TrackAsync(nameof(EditResponseAsync), Describe(interaction),
            () => Rest.EditOriginalInteractionResponseAsync(interaction.ApplicationId, interaction.Token, message,
                cancellationToken),
            edited => $"Edited the response {edited.Id} of {Describe(interaction)}");
    }

    public Task<DiscordMessage> EditResponseAsync(DiscordInteraction interaction, string content,
        CancellationToken cancellationToken = default) =>
        EditResponseAsync(interaction, new InteractionMessageRequest(content), cancellationToken);

    public Task<DiscordMessage> EditResponseAsync(DiscordInteraction interaction,
        Action<InteractionResponseFactory> configure, CancellationToken cancellationToken = default) =>
        EditResponseAsync(interaction, Compose(configure), cancellationToken);

    public Task DeleteResponseAsync(DiscordInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interaction);

        Guard(interaction);

        return TrackAsync(nameof(DeleteResponseAsync), Describe(interaction),
            () => Rest.DeleteOriginalInteractionResponseAsync(interaction.ApplicationId, interaction.Token,
                cancellationToken),
            $"Deleted the response of {Describe(interaction)}");
    }

    public Task<DiscordMessage> FollowUpAsync(DiscordInteraction interaction, InteractionMessageRequest message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interaction);
        ArgumentNullException.ThrowIfNull(message);

        Guard(interaction);

        return TrackAsync(nameof(FollowUpAsync), Describe(interaction),
            () => Rest.CreateFollowupMessageAsync(interaction.ApplicationId, interaction.Token, message,
                cancellationToken),
            sent => $"Sent follow-up {sent.Id} for {Describe(interaction)}");
    }

    public Task<DiscordMessage> FollowUpAsync(DiscordInteraction interaction, DiscordFile file,
        string? content = null, bool ephemeral = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        return FollowUpAsync(interaction,
            InteractionResponseFactory.Create().WithContent(content).AddFile(file).AsEphemeral(ephemeral).Build(),
            cancellationToken);
    }

    public Task<DiscordMessage> FollowUpAsync(DiscordInteraction interaction, string content, bool ephemeral = false,
        CancellationToken cancellationToken = default) =>
        FollowUpAsync(interaction, new InteractionMessageRequest(content, Ephemeral: ephemeral), cancellationToken);

    public Task<DiscordMessage> FollowUpAsync(DiscordInteraction interaction,
        Action<InteractionResponseFactory> configure, CancellationToken cancellationToken = default) =>
        FollowUpAsync(interaction, Compose(configure), cancellationToken);

    public Task<DiscordMessage> EditFollowUpAsync(DiscordInteraction interaction, Snowflake messageId,
        InteractionMessageRequest message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interaction);
        ArgumentNullException.ThrowIfNull(message);

        Guard(interaction);

        return TrackAsync(nameof(EditFollowUpAsync), $"follow-up {messageId} of {Describe(interaction)}",
            () => Rest.EditFollowupMessageAsync(interaction.ApplicationId, interaction.Token, messageId, message,
                cancellationToken),
            edited => $"Edited follow-up {edited.Id} of {Describe(interaction)}");
    }

    public Task DeleteFollowUpAsync(DiscordInteraction interaction, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interaction);

        Guard(interaction);

        return TrackAsync(nameof(DeleteFollowUpAsync), $"follow-up {messageId} of {Describe(interaction)}",
            () => Rest.DeleteFollowupMessageAsync(interaction.ApplicationId, interaction.Token, messageId,
                cancellationToken),
            $"Deleted follow-up {messageId} of {Describe(interaction)}");
    }

    private static InteractionMessageRequest Compose(Action<InteractionResponseFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = InteractionResponseFactory.Create();
        configure(factory);

        return factory.Build();
    }

    private static string Describe(DiscordInteraction interaction) =>
        interaction.CommandPath is { Length: > 0 } path
            ? $"interaction {interaction.Id} ({path})"
            : $"interaction {interaction.Id} ({interaction.Type})";

    private void Guard(DiscordInteraction interaction)
    {
        if (!interaction.IsExpired)
            return;

        var age = DateTimeOffset.UtcNow - interaction.CreatedAt;

        Warn($"The token of {Describe(interaction)} expired {(age - DiscordInteraction.TokenLifetime).TotalSeconds:F0}s ago");
        Emit(new InteractionExpired(interaction.Id, age));
    }
}

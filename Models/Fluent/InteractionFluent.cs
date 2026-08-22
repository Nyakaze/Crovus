using Crovus.Factory;

namespace Crovus.Models;

public static class InteractionFluent
{
    public static Task RespondAsync(this DiscordInteraction interaction, InteractionResponseRequest request,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.RespondAsync(interaction, request, cancellationToken);

    public static Task RespondAsync(this DiscordInteraction interaction, string content, bool ephemeral = false,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.RespondAsync(interaction, content, ephemeral, cancellationToken);

    public static Task RespondAsync(this DiscordInteraction interaction, InteractionMessageRequest message,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.RespondAsync(interaction, message, cancellationToken);

    public static Task RespondAsync(this DiscordInteraction interaction,
        Action<InteractionResponseFactory> configure, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.RespondAsync(interaction, configure, cancellationToken);

    public static Task RespondAsync(this DiscordInteraction interaction, DiscordEmbed embed,
        bool ephemeral = false, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.RespondAsync(interaction, embed, ephemeral, cancellationToken);

    public static Task RespondAsync(this DiscordInteraction interaction, DiscordFile file, string? content = null,
        bool ephemeral = false, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.RespondAsync(interaction, file, content, ephemeral, cancellationToken);

    public static Task RespondWithFileAsync(this DiscordInteraction interaction, string path,
        string? content = null, bool ephemeral = false, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.RespondWithFileAsync(interaction, path, content, ephemeral,
            cancellationToken);

    public static Task DeferAsync(this DiscordInteraction interaction, bool ephemeral = false,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.DeferAsync(interaction, ephemeral, cancellationToken);

    public static Task DeferUpdateAsync(this DiscordInteraction interaction,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.DeferUpdateAsync(interaction, cancellationToken);

    public static Task UpdateAsync(this DiscordInteraction interaction, InteractionMessageRequest message,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.UpdateAsync(interaction, message, cancellationToken);

    public static Task UpdateAsync(this DiscordInteraction interaction,
        Action<InteractionResponseFactory> configure, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.UpdateAsync(interaction, configure, cancellationToken);

    public static Task UpdateAsync(this DiscordInteraction interaction, string content,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.UpdateAsync(interaction, content, cancellationToken);

    public static Task ShowModalAsync(this DiscordInteraction interaction, DiscordModal modal,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.ShowModalAsync(interaction, modal, cancellationToken);

    public static Task ShowModalAsync(this DiscordInteraction interaction, ModalFactory modal,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.ShowModalAsync(interaction, modal, cancellationToken);

    public static Task ShowModalAsync(this DiscordInteraction interaction, string customId, string title,
        Action<ModalFactory> configure, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.ShowModalAsync(interaction, customId, title, configure,
            cancellationToken);

    public static Task AutocompleteAsync(this DiscordInteraction interaction,
        IEnumerable<DiscordApplicationCommandChoice> choices, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.AutocompleteAsync(interaction, choices, cancellationToken);

    public static Task AutocompleteAsync(this DiscordInteraction interaction, IEnumerable<string> values,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.AutocompleteAsync(interaction, values, cancellationToken);

    public static Task PongAsync(this DiscordInteraction interaction,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.PongAsync(interaction, cancellationToken);

    public static Task<DiscordMessage> GetResponseAsync(this DiscordInteraction interaction,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.GetResponseAsync(interaction, cancellationToken);

    public static Task<DiscordMessage> EditResponseAsync(this DiscordInteraction interaction,
        InteractionMessageRequest message, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.EditResponseAsync(interaction, message, cancellationToken);

    public static Task<DiscordMessage> EditResponseAsync(this DiscordInteraction interaction, string content,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.EditResponseAsync(interaction, content, cancellationToken);

    public static Task<DiscordMessage> EditResponseAsync(this DiscordInteraction interaction,
        Action<InteractionResponseFactory> configure, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.EditResponseAsync(interaction, configure, cancellationToken);

    public static Task DeleteResponseAsync(this DiscordInteraction interaction,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.DeleteResponseAsync(interaction, cancellationToken);

    public static Task<DiscordMessage> FollowUpAsync(this DiscordInteraction interaction,
        InteractionMessageRequest message, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.FollowUpAsync(interaction, message, cancellationToken);

    public static Task<DiscordMessage> FollowUpAsync(this DiscordInteraction interaction, string content,
        bool ephemeral = false, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.FollowUpAsync(interaction, content, ephemeral, cancellationToken);

    public static Task<DiscordMessage> FollowUpAsync(this DiscordInteraction interaction,
        Action<InteractionResponseFactory> configure, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.FollowUpAsync(interaction, configure, cancellationToken);

    public static Task<DiscordMessage> FollowUpAsync(this DiscordInteraction interaction, DiscordFile file,
        string? content = null, bool ephemeral = false, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.FollowUpAsync(interaction, file, content, ephemeral,
            cancellationToken);

    public static Task<DiscordMessage> EditFollowUpAsync(this DiscordInteraction interaction, Snowflake messageId,
        InteractionMessageRequest message, CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.EditFollowUpAsync(interaction, messageId, message, cancellationToken);

    public static Task DeleteFollowUpAsync(this DiscordInteraction interaction, Snowflake messageId,
        CancellationToken cancellationToken = default) =>
        interaction.Services().Interactions.DeleteFollowUpAsync(interaction, messageId, cancellationToken);

    public static async Task<DiscordChannel?> GetChannelAsync(this DiscordInteraction interaction,
        CancellationToken cancellationToken = default) =>
        interaction.ChannelId is { } channelId
            ? await interaction.Rest().GetChannelAsync(channelId, cancellationToken)
            : null;

    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordInteraction interaction,
        CancellationToken cancellationToken = default) =>
        interaction.GuildId is { } guildId
            ? await interaction.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;
}

using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class ReactionService : DiscordService
{
    public ReactionService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Reaction", logger, telemetry)
    {
    }

    public ReactionService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task AddAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        return TrackAsync(nameof(AddAsync), $"message {messageId} in channel {channelId}",
            () => Rest.CreateReactionAsync(channelId, messageId, emoji, cancellationToken),
            $"Reacted {ReactionFactory.Format(emoji)} to message {messageId} in channel {channelId}");
    }

    public Task AddAsync(Snowflake channelId, Snowflake messageId, string emoji,
        CancellationToken cancellationToken = default) =>
        AddAsync(channelId, messageId, ReactionFactory.Parse(emoji), cancellationToken);

    public Task AddAsync(DiscordMessage message, string emoji, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return AddAsync(message.ChannelId, message.Id, ReactionFactory.Parse(emoji), cancellationToken);
    }

    public Task RemoveAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        return TrackAsync(nameof(RemoveAsync), $"message {messageId} in channel {channelId}",
            () => Rest.DeleteOwnReactionAsync(channelId, messageId, emoji, cancellationToken),
            $"Removed reaction {ReactionFactory.Format(emoji)} from message {messageId} in channel {channelId}");
    }

    public Task RemoveAsync(Snowflake channelId, Snowflake messageId, string emoji,
        CancellationToken cancellationToken = default) =>
        RemoveAsync(channelId, messageId, ReactionFactory.Parse(emoji), cancellationToken);

    public Task RemoveAsync(DiscordMessage message, string emoji, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return RemoveAsync(message.ChannelId, message.Id, ReactionFactory.Parse(emoji), cancellationToken);
    }

    public async Task ApplyAsync(Snowflake channelId, Snowflake messageId, IReadOnlyList<DiscordEmoji> emojis,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emojis);

        if (emojis.Count == 0)
            return;

        await TrackAsync(nameof(ApplyAsync), $"message {messageId} in channel {channelId}",
            async () =>
            {
                foreach (var emoji in emojis)
                    await Rest.CreateReactionAsync(channelId, messageId, emoji, cancellationToken);
            },
            $"Applied {emojis.Count} reactions to message {messageId} in channel {channelId}");

        Emit(new ReactionsApplied(channelId.Value, messageId.Value, emojis.Count));
    }

    public Task ApplyAsync(Snowflake channelId, Snowflake messageId, ReactionFactory reactions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reactions);

        return ApplyAsync(channelId, messageId, reactions.Build(), cancellationToken);
    }

    public Task ApplyAsync(Snowflake channelId, Snowflake messageId, Action<ReactionFactory> configure,
        CancellationToken cancellationToken = default) =>
        ApplyAsync(channelId, messageId, Compose(configure), cancellationToken);

    public Task ApplyAsync(DiscordMessage message, params string[] emojis)
    {
        ArgumentNullException.ThrowIfNull(message);

        return ApplyAsync(message.ChannelId, message.Id, ReactionFactory.Of(emojis).Build());
    }

    public Task ApplyAsync(DiscordMessage message, IEnumerable<string> emojis,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(emojis);

        return ApplyAsync(message.ChannelId, message.Id, ReactionFactory.Of(emojis.ToArray()).Build(),
            cancellationToken);
    }

    public async Task WithdrawAsync(Snowflake channelId, Snowflake messageId, IReadOnlyList<DiscordEmoji> emojis,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emojis);

        if (emojis.Count == 0)
            return;

        await TrackAsync(nameof(WithdrawAsync), $"message {messageId} in channel {channelId}",
            async () =>
            {
                foreach (var emoji in emojis)
                    await Rest.DeleteOwnReactionAsync(channelId, messageId, emoji, cancellationToken);
            },
            $"Withdrew {emojis.Count} reactions from message {messageId} in channel {channelId}");

        Emit(new ReactionsWithdrawn(channelId.Value, messageId.Value, emojis.Count));
    }

    public Task WithdrawAsync(Snowflake channelId, Snowflake messageId, Action<ReactionFactory> configure,
        CancellationToken cancellationToken = default) =>
        WithdrawAsync(channelId, messageId, Compose(configure), cancellationToken);

    public Task WithdrawAsync(DiscordMessage message, params string[] emojis)
    {
        ArgumentNullException.ThrowIfNull(message);

        return WithdrawAsync(message.ChannelId, message.Id, ReactionFactory.Of(emojis).Build());
    }

    public async Task<DiscordMessage> PromptAsync(Snowflake channelId, Action<MessageFactory> configure,
        IEnumerable<string> options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(options);

        var prompt = MessageFactory.Create();
        configure(prompt);

        var choices = ReactionFactory.Of(options.ToArray()).Build();

        if (choices.Count == 0)
            throw new ArgumentException("A prompt needs at least one option.", nameof(options));

        var message = await TrackAsync(nameof(PromptAsync), $"channel {channelId}",
            async () =>
            {
                var posted = await Rest.CreateMessageAsync(channelId, prompt.Build(), cancellationToken);

                foreach (var choice in choices)
                    await Rest.CreateReactionAsync(channelId, posted.Id, choice, cancellationToken);

                return posted;
            },
            posted => $"Posted prompt {posted.Id} with {choices.Count} options in channel {channelId}");

        Emit(new ReactionsApplied(channelId.Value, message.Id.Value, choices.Count));

        return message;
    }

    public Task<DiscordMessage> PromptAsync(Snowflake channelId, string content, IEnumerable<string> options,
        CancellationToken cancellationToken = default) =>
        PromptAsync(channelId, message => message.WithContent(content), options, cancellationToken);

    private static IReadOnlyList<DiscordEmoji> Compose(Action<ReactionFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = ReactionFactory.Create();
        configure(factory);

        return factory.Build();
    }
}

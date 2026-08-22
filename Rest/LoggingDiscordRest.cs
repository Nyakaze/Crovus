using System.Diagnostics;
using System.Runtime.CompilerServices;
using Crovus.Client;
using Crovus.Logs;
using Crovus.Models;

namespace Crovus.Rest;

public sealed class LoggingDiscordRest : IDiscordRest, IContextAware
{
    private const string LogCategory = "Rest.Client";

    private readonly IDiscordRest _inner;
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;

    public LoggingDiscordRest(IDiscordRest inner, ILogger? logger = null, ITelemetry? telemetry = null)
    {
        _inner = inner;
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;
    }

    public LoggingDiscordRest(IDiscordRest inner, DiagnosticsHub diagnostics)
        : this(inner, diagnostics, diagnostics)
    {
    }

    public ICrovusContext? Context
    {
        get => (_inner as IContextAware)?.Context;
        set
        {
            if (_inner is IContextAware aware)
                aware.Context = value;
        }
    }

    public async Task<DiscordChannel> GetChannelAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var channel = await _inner.GetChannelAsync(channelId, cancellationToken);
            Succeeded(nameof(GetChannelAsync), start, LogLevel.Debug, $"Fetched channel {channelId}");
            return channel;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetChannelAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<DiscordMessage> GetMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var message = await _inner.GetMessageAsync(channelId, messageId, cancellationToken);
            Succeeded(nameof(GetMessageAsync), start, LogLevel.Debug,
                $"Fetched message {messageId} from channel {channelId}");
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetMessageAsync), start, exception, $"message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, Snowflake? before = null,
        int? limit = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        var count = 0;

        var messages = _inner
            .GetMessagesAsync(channelId, before, limit, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                DiscordMessage message;

                try
                {
                    if (!await messages.MoveNextAsync())
                        break;

                    message = messages.Current;
                }
                catch (Exception exception)
                {
                    Failed(nameof(GetMessagesAsync), start, exception, $"channel {channelId} after {count} messages");
                    throw;
                }

                count++;
                yield return message;
            }
        }
        finally
        {
            await messages.DisposeAsync();
        }

        Succeeded(nameof(GetMessagesAsync), start, LogLevel.Debug, $"Read {count} messages from channel {channelId}");
    }

    public async Task<DiscordMessage> CreateMessageAsync(Snowflake channelId, MessageCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var message = await _inner.CreateMessageAsync(channelId, request, cancellationToken);
            Succeeded(nameof(CreateMessageAsync), start, LogLevel.Information,
                $"Created message {message.Id} in channel {channelId}{Uploaded(request.Files)}{Showing(request.Components)}");
            Emit(new MessageCreated(channelId.Value, message.Id.Value));
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateMessageAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<DiscordMessage> EditMessageAsync(Snowflake channelId, Snowflake messageId,
        MessageEditRequest request, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var message = await _inner.EditMessageAsync(channelId, messageId, request, cancellationToken);
            Succeeded(nameof(EditMessageAsync), start, LogLevel.Information,
                $"Edited message {messageId} in channel {channelId}{Uploaded(request.Files)}{Showing(request.Components)}");
            Emit(new MessageEdited(channelId.Value, messageId.Value));
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(EditMessageAsync), start, exception, $"message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async Task DeleteMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteMessageAsync(channelId, messageId, reason, cancellationToken);
            Succeeded(nameof(DeleteMessageAsync), start, LogLevel.Information,
                $"Deleted message {messageId} in channel {channelId}{Because(reason)}");
            Emit(new MessageDeleted(channelId.Value, messageId.Value, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteMessageAsync), start, exception, $"message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async Task CreateReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.CreateReactionAsync(channelId, messageId, emoji, cancellationToken);
            Succeeded(nameof(CreateReactionAsync), start, LogLevel.Debug,
                $"Added reaction {Describe(emoji)} to message {messageId} in channel {channelId}");
            Emit(new ReactionAdded(channelId.Value, messageId.Value, Describe(emoji)));
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateReactionAsync), start, exception, $"reaction {Describe(emoji)} on message {messageId}");
            throw;
        }
    }

    public async Task DeleteOwnReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteOwnReactionAsync(channelId, messageId, emoji, cancellationToken);
            Succeeded(nameof(DeleteOwnReactionAsync), start, LogLevel.Debug,
                $"Removed reaction {Describe(emoji)} from message {messageId} in channel {channelId}");
            Emit(new ReactionRemoved(channelId.Value, messageId.Value, Describe(emoji)));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteOwnReactionAsync), start, exception,
                $"reaction {Describe(emoji)} on message {messageId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordWebhook>> GetChannelWebhooksAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var webhooks = await _inner.GetChannelWebhooksAsync(channelId, cancellationToken);
            Succeeded(nameof(GetChannelWebhooksAsync), start, LogLevel.Debug,
                $"Fetched {webhooks.Count} webhooks for channel {channelId}");
            return webhooks;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetChannelWebhooksAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<DiscordWebhook> GetWebhookAsync(Snowflake webhookId, string? token = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var webhook = await _inner.GetWebhookAsync(webhookId, token, cancellationToken);
            Succeeded(nameof(GetWebhookAsync), start, LogLevel.Debug, $"Fetched webhook {webhookId}");
            return webhook;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetWebhookAsync), start, exception, $"webhook {webhookId}");
            throw;
        }
    }

    public async Task<DiscordWebhook> CreateWebhookAsync(Snowflake channelId, WebhookCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var webhook = await _inner.CreateWebhookAsync(channelId, request, reason, cancellationToken);
            Succeeded(nameof(CreateWebhookAsync), start, LogLevel.Information,
                $"Created webhook {webhook.Id} named '{request.Name}' in channel {channelId}{Because(reason)}");
            Emit(new WebhookCreated(webhook.Id.Value, channelId.Value, request.Name));
            return webhook;
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateWebhookAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<DiscordWebhook> ModifyWebhookAsync(Snowflake webhookId, WebhookModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var webhook = await _inner.ModifyWebhookAsync(webhookId, request, reason, cancellationToken);
            Succeeded(nameof(ModifyWebhookAsync), start, LogLevel.Information,
                $"Modified webhook {webhookId}{Because(reason)}");
            Emit(new WebhookModified(webhookId.Value));
            return webhook;
        }
        catch (Exception exception)
        {
            Failed(nameof(ModifyWebhookAsync), start, exception, $"webhook {webhookId}");
            throw;
        }
    }

    public async Task DeleteWebhookAsync(Snowflake webhookId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteWebhookAsync(webhookId, reason, cancellationToken);
            Succeeded(nameof(DeleteWebhookAsync), start, LogLevel.Information,
                $"Deleted webhook {webhookId}{Because(reason)}");
            Emit(new WebhookDeleted(webhookId.Value));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteWebhookAsync), start, exception, $"webhook {webhookId}");
            throw;
        }
    }

    public async Task<DiscordMessage?> ExecuteWebhookAsync(DiscordWebhook webhook, WebhookExecuteRequest request,
        Snowflake? threadId = null, bool wait = false, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        var target = threadId is { } thread ? $"thread {thread}" : $"channel {webhook.ChannelId}";

        try
        {
            var message = await _inner.ExecuteWebhookAsync(webhook, request, threadId, wait, cancellationToken);
            Succeeded(nameof(ExecuteWebhookAsync), start, LogLevel.Information,
                $"Executed webhook {webhook.Id} into {target}{(message is null ? string.Empty : $", message {message.Id}")}{Uploaded(request.Files)}{Showing(request.Components)}");
            Emit(new WebhookExecuted(webhook.Id.Value, webhook.ChannelId.Value, threadId?.Value, wait));
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(ExecuteWebhookAsync), start, exception, $"webhook {webhook.Id} into {target}");
            throw;
        }
    }

    public async Task<DiscordChannel> CreateChannelAsync(Snowflake guildId, ChannelCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var channel = await _inner.CreateChannelAsync(guildId, request, reason, cancellationToken);
            Succeeded(nameof(CreateChannelAsync), start, LogLevel.Information,
                $"Created {channel.Type} channel {channel.Name} ({channel.Id}) in guild {guildId}{Because(reason)}");
            Emit(new ChannelCreated(guildId, channel.Id, channel.Type.ToString(), channel.Name));
            return channel;
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateChannelAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordChannel> ModifyChannelAsync(Snowflake channelId, ChannelModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var channel = await _inner.ModifyChannelAsync(channelId, request, reason, cancellationToken);
            Succeeded(nameof(ModifyChannelAsync), start, LogLevel.Information,
                $"Modified channel {channelId}{Because(reason)}");
            Emit(new ChannelModified(channelId));
            return channel;
        }
        catch (Exception exception)
        {
            Failed(nameof(ModifyChannelAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task DeleteChannelAsync(Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteChannelAsync(channelId, reason, cancellationToken);
            Succeeded(nameof(DeleteChannelAsync), start, LogLevel.Information,
                $"Deleted channel {channelId}{Because(reason)}");
            Emit(new ChannelDeleted(channelId, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteChannelAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<DiscordChannel> StartThreadAsync(Snowflake channelId, ThreadCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var thread = await _inner.StartThreadAsync(channelId, request, reason, cancellationToken);
            Succeeded(nameof(StartThreadAsync), start, LogLevel.Information,
                $"Started {thread.Type} thread {thread.Name} ({thread.Id}) in channel {channelId}" +
                $"{Uploaded(request.Message?.Files)}{Because(reason)}");
            Emit(new ThreadCreated(channelId, thread.Id, thread.Type.ToString(), thread.Name));
            return thread;
        }
        catch (Exception exception)
        {
            Failed(nameof(StartThreadAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<DiscordChannel> StartThreadFromMessageAsync(Snowflake channelId, Snowflake messageId,
        ThreadFromMessageRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var thread = await _inner.StartThreadFromMessageAsync(channelId, messageId, request, reason,
                cancellationToken);
            Succeeded(nameof(StartThreadFromMessageAsync), start, LogLevel.Information,
                $"Started thread {thread.Name} ({thread.Id}) from message {messageId} in channel {channelId}{Because(reason)}");
            Emit(new ThreadCreated(channelId, thread.Id, thread.Type.ToString(), thread.Name));
            return thread;
        }
        catch (Exception exception)
        {
            Failed(nameof(StartThreadFromMessageAsync), start, exception,
                $"message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordGuildEmoji>> GetGuildEmojisAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var emojis = await _inner.GetGuildEmojisAsync(guildId, cancellationToken);
            Succeeded(nameof(GetGuildEmojisAsync), start, LogLevel.Debug,
                $"Fetched {emojis.Count} emojis from guild {guildId}");
            return emojis;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildEmojisAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordGuildEmoji> GetGuildEmojiAsync(Snowflake guildId, Snowflake emojiId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var emoji = await _inner.GetGuildEmojiAsync(guildId, emojiId, cancellationToken);
            Succeeded(nameof(GetGuildEmojiAsync), start, LogLevel.Debug,
                $"Fetched emoji {emojiId} from guild {guildId}");
            return emoji;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildEmojiAsync), start, exception, $"emoji {emojiId} in guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordGuildEmoji> CreateGuildEmojiAsync(Snowflake guildId, EmojiCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var emoji = await _inner.CreateGuildEmojiAsync(guildId, request, reason, cancellationToken);
            Succeeded(nameof(CreateGuildEmojiAsync), start, LogLevel.Information,
                $"Created emoji {emoji.Name} ({emoji.Id}) in guild {guildId}{Because(reason)}");
            Emit(new EmojiCreated(guildId, emoji.Id, emoji.Name));
            return emoji;
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateGuildEmojiAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordGuildEmoji> ModifyGuildEmojiAsync(Snowflake guildId, Snowflake emojiId,
        EmojiModifyRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var emoji = await _inner.ModifyGuildEmojiAsync(guildId, emojiId, request, reason, cancellationToken);
            Succeeded(nameof(ModifyGuildEmojiAsync), start, LogLevel.Information,
                $"Modified emoji {emojiId} in guild {guildId}{Because(reason)}");
            Emit(new EmojiModified(guildId, emojiId));
            return emoji;
        }
        catch (Exception exception)
        {
            Failed(nameof(ModifyGuildEmojiAsync), start, exception, $"emoji {emojiId} in guild {guildId}");
            throw;
        }
    }

    public async Task DeleteGuildEmojiAsync(Snowflake guildId, Snowflake emojiId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteGuildEmojiAsync(guildId, emojiId, reason, cancellationToken);
            Succeeded(nameof(DeleteGuildEmojiAsync), start, LogLevel.Information,
                $"Deleted emoji {emojiId} from guild {guildId}{Because(reason)}");
            Emit(new EmojiDeleted(guildId, emojiId, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteGuildEmojiAsync), start, exception, $"emoji {emojiId} in guild {guildId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordApplicationCommand>> GetApplicationCommandsAsync(Snowflake applicationId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var commands = await _inner.GetApplicationCommandsAsync(applicationId, guildId, cancellationToken);
            Succeeded(nameof(GetApplicationCommandsAsync), start, LogLevel.Debug,
                $"Fetched {commands.Count} commands for {Scope(applicationId, guildId)}");
            return commands;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetApplicationCommandsAsync), start, exception, Scope(applicationId, guildId));
            throw;
        }
    }

    public async Task<DiscordApplicationCommand> CreateApplicationCommandAsync(Snowflake applicationId,
        ApplicationCommandRequest request, Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var command = await _inner.CreateApplicationCommandAsync(applicationId, request, guildId,
                cancellationToken);
            Succeeded(nameof(CreateApplicationCommandAsync), start, LogLevel.Information,
                $"Registered command {command.Name} ({command.Id}) for {Scope(applicationId, guildId)}");
            Emit(new ApplicationCommandCreated(applicationId, command.Id, command.Name, guildId?.Value));
            return command;
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateApplicationCommandAsync), start, exception, Scope(applicationId, guildId));
            throw;
        }
    }

    public async Task<DiscordApplicationCommand> EditApplicationCommandAsync(Snowflake applicationId,
        Snowflake commandId, ApplicationCommandRequest request, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var command = await _inner.EditApplicationCommandAsync(applicationId, commandId, request, guildId,
                cancellationToken);
            Succeeded(nameof(EditApplicationCommandAsync), start, LogLevel.Information,
                $"Edited command {commandId} for {Scope(applicationId, guildId)}");
            Emit(new ApplicationCommandEdited(applicationId, commandId, guildId?.Value));
            return command;
        }
        catch (Exception exception)
        {
            Failed(nameof(EditApplicationCommandAsync), start, exception,
                $"command {commandId} for {Scope(applicationId, guildId)}");
            throw;
        }
    }

    public async Task DeleteApplicationCommandAsync(Snowflake applicationId, Snowflake commandId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteApplicationCommandAsync(applicationId, commandId, guildId, cancellationToken);
            Succeeded(nameof(DeleteApplicationCommandAsync), start, LogLevel.Information,
                $"Deleted command {commandId} for {Scope(applicationId, guildId)}");
            Emit(new ApplicationCommandDeleted(applicationId, commandId, guildId?.Value));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteApplicationCommandAsync), start, exception,
                $"command {commandId} for {Scope(applicationId, guildId)}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordApplicationCommand>> SetApplicationCommandsAsync(Snowflake applicationId,
        IReadOnlyList<ApplicationCommandRequest> requests, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var commands = await _inner.SetApplicationCommandsAsync(applicationId, requests, guildId,
                cancellationToken);
            Succeeded(nameof(SetApplicationCommandsAsync), start, LogLevel.Information,
                $"Overwrote {Scope(applicationId, guildId)} with {commands.Count} commands");
            Emit(new ApplicationCommandsOverwritten(applicationId, commands.Count, guildId?.Value));
            return commands;
        }
        catch (Exception exception)
        {
            Failed(nameof(SetApplicationCommandsAsync), start, exception, Scope(applicationId, guildId));
            throw;
        }
    }

    public async Task CreateInteractionResponseAsync(Snowflake interactionId, string interactionToken,
        InteractionResponseRequest request, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.CreateInteractionResponseAsync(interactionId, interactionToken, request, cancellationToken);
            Succeeded(nameof(CreateInteractionResponseAsync), start, LogLevel.Debug,
                $"Answered interaction {interactionId} with {request.Type}{Uploaded(request.Message?.Files)}{Showing(request.Message?.Components)}");
            Emit(new InteractionResponded(interactionId, request.Type.ToString(),
                request.Message?.EffectiveFlags.HasFlag(MessageFlags.Ephemeral) ?? false));
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateInteractionResponseAsync), start, exception, $"interaction {interactionId}");
            throw;
        }
    }

    public async Task<DiscordMessage> GetOriginalInteractionResponseAsync(Snowflake applicationId,
        string interactionToken, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var message = await _inner.GetOriginalInteractionResponseAsync(applicationId, interactionToken,
                cancellationToken);
            Succeeded(nameof(GetOriginalInteractionResponseAsync), start, LogLevel.Debug,
                $"Fetched the original response {message.Id} of application {applicationId}");
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetOriginalInteractionResponseAsync), start, exception, $"application {applicationId}");
            throw;
        }
    }

    public async Task<DiscordMessage> EditOriginalInteractionResponseAsync(Snowflake applicationId,
        string interactionToken, InteractionMessageRequest request, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var message = await _inner.EditOriginalInteractionResponseAsync(applicationId, interactionToken, request,
                cancellationToken);
            Succeeded(nameof(EditOriginalInteractionResponseAsync), start, LogLevel.Information,
                $"Edited the original response {message.Id} of application {applicationId}{Uploaded(request.Files)}{Showing(request.Components)}");
            Emit(new MessageEdited(message.ChannelId, message.Id));
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(EditOriginalInteractionResponseAsync), start, exception, $"application {applicationId}");
            throw;
        }
    }

    public async Task DeleteOriginalInteractionResponseAsync(Snowflake applicationId, string interactionToken,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteOriginalInteractionResponseAsync(applicationId, interactionToken, cancellationToken);
            Succeeded(nameof(DeleteOriginalInteractionResponseAsync), start, LogLevel.Information,
                $"Deleted the original response of application {applicationId}");
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteOriginalInteractionResponseAsync), start, exception, $"application {applicationId}");
            throw;
        }
    }

    public async Task<DiscordMessage> CreateFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        InteractionMessageRequest request, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var message = await _inner.CreateFollowupMessageAsync(applicationId, interactionToken, request,
                cancellationToken);
            Succeeded(nameof(CreateFollowupMessageAsync), start, LogLevel.Information,
                $"Sent follow-up {message.Id} for application {applicationId}{Uploaded(request.Files)}{Showing(request.Components)}");
            Emit(new InteractionFollowedUp(applicationId, message.Id,
                request.EffectiveFlags.HasFlag(MessageFlags.Ephemeral)));
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateFollowupMessageAsync), start, exception, $"application {applicationId}");
            throw;
        }
    }

    public async Task<DiscordMessage> EditFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        Snowflake messageId, InteractionMessageRequest request, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var message = await _inner.EditFollowupMessageAsync(applicationId, interactionToken, messageId, request,
                cancellationToken);
            Succeeded(nameof(EditFollowupMessageAsync), start, LogLevel.Information,
                $"Edited follow-up {messageId} of application {applicationId}{Uploaded(request.Files)}{Showing(request.Components)}");
            Emit(new MessageEdited(message.ChannelId, message.Id));
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(EditFollowupMessageAsync), start, exception,
                $"follow-up {messageId} of application {applicationId}");
            throw;
        }
    }

    public async Task DeleteFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        Snowflake messageId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteFollowupMessageAsync(applicationId, interactionToken, messageId, cancellationToken);
            Succeeded(nameof(DeleteFollowupMessageAsync), start, LogLevel.Information,
                $"Deleted follow-up {messageId} of application {applicationId}");
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteFollowupMessageAsync), start, exception,
                $"follow-up {messageId} of application {applicationId}");
            throw;
        }
    }

    public async Task<DiscordGuild> GetGuildAsync(Snowflake guildId, bool withCounts = false,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var guild = await _inner.GetGuildAsync(guildId, withCounts, cancellationToken);
            Succeeded(nameof(GetGuildAsync), start, LogLevel.Debug, $"Fetched guild {guildId}");
            return guild;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordChannel>> GetGuildChannelsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var channels = await _inner.GetGuildChannelsAsync(guildId, cancellationToken);
            Succeeded(nameof(GetGuildChannelsAsync), start, LogLevel.Debug,
                $"Fetched {channels.Count} channels of guild {guildId}");
            return channels;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildChannelsAsync), start, exception, $"channels of guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordMember> GetGuildMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var member = await _inner.GetGuildMemberAsync(guildId, userId, cancellationToken);
            Succeeded(nameof(GetGuildMemberAsync), start, LogLevel.Debug,
                $"Fetched member {userId} of guild {guildId}");
            return member;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildMemberAsync), start, exception, $"member {userId} of guild {guildId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordMember>> GetGuildMembersAsync(Snowflake guildId, MemberQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var members = await _inner.GetGuildMembersAsync(guildId, query, cancellationToken);
            Succeeded(nameof(GetGuildMembersAsync), start, LogLevel.Debug,
                $"Fetched {members.Count} members of guild {guildId}");
            Emit(new MembersFetched(guildId, members.Count, query?.After?.Value));
            return members;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildMembersAsync), start, exception, $"members of guild {guildId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordMember>> SearchGuildMembersAsync(Snowflake guildId, string search,
        int limit = 1, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var members = await _inner.SearchGuildMembersAsync(guildId, search, limit, cancellationToken);
            Succeeded(nameof(SearchGuildMembersAsync), start, LogLevel.Debug,
                $"Found {members.Count} members matching '{search}' in guild {guildId}");
            return members;
        }
        catch (Exception exception)
        {
            Failed(nameof(SearchGuildMembersAsync), start, exception,
                $"member search '{search}' in guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordMember> ModifyGuildMemberAsync(Snowflake guildId, Snowflake userId,
        MemberModifyRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var member = await _inner.ModifyGuildMemberAsync(guildId, userId, request, reason, cancellationToken);
            Succeeded(nameof(ModifyGuildMemberAsync), start, LogLevel.Information,
                $"Modified member {userId} of guild {guildId}{Because(reason)}");
            Emit(new MemberModified(guildId, userId, Changes(request)));
            return member;
        }
        catch (Exception exception)
        {
            Failed(nameof(ModifyGuildMemberAsync), start, exception, $"member {userId} of guild {guildId}");
            throw;
        }
    }

    public async Task AddGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.AddGuildMemberRoleAsync(guildId, userId, roleId, reason, cancellationToken);
            Succeeded(nameof(AddGuildMemberRoleAsync), start, LogLevel.Information,
                $"Granted role {roleId} to member {userId} of guild {guildId}{Because(reason)}");
            Emit(new MemberRoleChanged(guildId, userId, roleId, true));
        }
        catch (Exception exception)
        {
            Failed(nameof(AddGuildMemberRoleAsync), start, exception,
                $"role {roleId} for member {userId} of guild {guildId}");
            throw;
        }
    }

    public async Task RemoveGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.RemoveGuildMemberRoleAsync(guildId, userId, roleId, reason, cancellationToken);
            Succeeded(nameof(RemoveGuildMemberRoleAsync), start, LogLevel.Information,
                $"Revoked role {roleId} from member {userId} of guild {guildId}{Because(reason)}");
            Emit(new MemberRoleChanged(guildId, userId, roleId, false));
        }
        catch (Exception exception)
        {
            Failed(nameof(RemoveGuildMemberRoleAsync), start, exception,
                $"role {roleId} for member {userId} of guild {guildId}");
            throw;
        }
    }

    public async Task RemoveGuildMemberAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.RemoveGuildMemberAsync(guildId, userId, reason, cancellationToken);
            Succeeded(nameof(RemoveGuildMemberAsync), start, LogLevel.Warning,
                $"Kicked member {userId} from guild {guildId}{Because(reason)}");
            Emit(new MemberKicked(guildId, userId, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(RemoveGuildMemberAsync), start, exception, $"member {userId} of guild {guildId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordBan>> GetGuildBansAsync(Snowflake guildId, BanQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var bans = await _inner.GetGuildBansAsync(guildId, query, cancellationToken);
            Succeeded(nameof(GetGuildBansAsync), start, LogLevel.Debug,
                $"Fetched {bans.Count} bans of guild {guildId}");
            return bans;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildBansAsync), start, exception, $"bans of guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordBan?> GetGuildBanAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var ban = await _inner.GetGuildBanAsync(guildId, userId, cancellationToken);
            Succeeded(nameof(GetGuildBanAsync), start, LogLevel.Debug,
                ban is null
                    ? $"User {userId} is not banned in guild {guildId}"
                    : $"Fetched the ban of {userId} in guild {guildId}");
            return ban;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildBanAsync), start, exception, $"ban of {userId} in guild {guildId}");
            throw;
        }
    }

    public async Task CreateGuildBanAsync(Snowflake guildId, Snowflake userId, BanCreateRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.CreateGuildBanAsync(guildId, userId, request, reason, cancellationToken);
            Succeeded(nameof(CreateGuildBanAsync), start, LogLevel.Warning,
                $"Banned {userId} from guild {guildId}{Because(reason)}");
            Emit(new MemberBanned(guildId, userId, request?.DeleteMessageSeconds ?? 0, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateGuildBanAsync), start, exception, $"ban of {userId} in guild {guildId}");
            throw;
        }
    }

    public async Task RemoveGuildBanAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.RemoveGuildBanAsync(guildId, userId, reason, cancellationToken);
            Succeeded(nameof(RemoveGuildBanAsync), start, LogLevel.Information,
                $"Unbanned {userId} in guild {guildId}{Because(reason)}");
            Emit(new MemberUnbanned(guildId, userId, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(RemoveGuildBanAsync), start, exception, $"ban of {userId} in guild {guildId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordRole>> GetGuildRolesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var roles = await _inner.GetGuildRolesAsync(guildId, cancellationToken);
            Succeeded(nameof(GetGuildRolesAsync), start, LogLevel.Debug,
                $"Fetched {roles.Count} roles of guild {guildId}");
            return roles;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildRolesAsync), start, exception, $"roles of guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordRole> CreateGuildRoleAsync(Snowflake guildId, RoleCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var role = await _inner.CreateGuildRoleAsync(guildId, request, reason, cancellationToken);
            Succeeded(nameof(CreateGuildRoleAsync), start, LogLevel.Information,
                $"Created role {role.Name} ({role.Id}) in guild {guildId}{Because(reason)}");
            Emit(new RoleCreated(guildId, role.Id, role.Name, (ulong)role.Permissions));
            return role;
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateGuildRoleAsync), start, exception, $"role in guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordRole> ModifyGuildRoleAsync(Snowflake guildId, Snowflake roleId,
        RoleModifyRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var role = await _inner.ModifyGuildRoleAsync(guildId, roleId, request, reason, cancellationToken);
            Succeeded(nameof(ModifyGuildRoleAsync), start, LogLevel.Information,
                $"Modified role {role.Name} ({roleId}) in guild {guildId}{Because(reason)}");
            Emit(new RoleModified(guildId, roleId, role.Name, (ulong)role.Permissions));
            return role;
        }
        catch (Exception exception)
        {
            Failed(nameof(ModifyGuildRoleAsync), start, exception, $"role {roleId} in guild {guildId}");
            throw;
        }
    }

    public async Task DeleteGuildRoleAsync(Snowflake guildId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteGuildRoleAsync(guildId, roleId, reason, cancellationToken);
            Succeeded(nameof(DeleteGuildRoleAsync), start, LogLevel.Warning,
                $"Deleted role {roleId} in guild {guildId}{Because(reason)}");
            Emit(new RoleDeleted(guildId, roleId, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteGuildRoleAsync), start, exception, $"role {roleId} in guild {guildId}");
            throw;
        }
    }

    public async IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, MessageQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        var count = 0;

        var messages = _inner
            .GetMessagesAsync(channelId, query, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                DiscordMessage message;

                try
                {
                    if (!await messages.MoveNextAsync())
                        break;

                    message = messages.Current;
                }
                catch (Exception exception)
                {
                    Failed(nameof(GetMessagesAsync), start, exception, $"channel {channelId} after {count} messages");
                    throw;
                }

                count++;
                yield return message;
            }
        }
        finally
        {
            await messages.DisposeAsync();
        }

        Succeeded(nameof(GetMessagesAsync), start, LogLevel.Debug,
            $"Read {count} messages from channel {channelId} ({Describe(query)})");
    }

    public async Task BulkDeleteMessagesAsync(Snowflake channelId, IReadOnlyList<Snowflake> messageIds,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.BulkDeleteMessagesAsync(channelId, messageIds, reason, cancellationToken);
            Succeeded(nameof(BulkDeleteMessagesAsync), start, LogLevel.Warning,
                $"Bulk deleted {messageIds.Count} messages in channel {channelId}{Because(reason)}");
            Emit(new MessagesBulkDeleted(channelId, messageIds.Count, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(BulkDeleteMessagesAsync), start, exception,
                $"{messageIds.Count} messages in channel {channelId}");
            throw;
        }
    }

    public async Task<DiscordMessage> CrosspostMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var message = await _inner.CrosspostMessageAsync(channelId, messageId, cancellationToken);
            Succeeded(nameof(CrosspostMessageAsync), start, LogLevel.Information,
                $"Crossposted message {messageId} from channel {channelId}");
            Emit(new MessageCrossposted(channelId, messageId));
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(CrosspostMessageAsync), start, exception, $"message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordMessage>> GetPinnedMessagesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var messages = await _inner.GetPinnedMessagesAsync(channelId, cancellationToken);
            Succeeded(nameof(GetPinnedMessagesAsync), start, LogLevel.Debug,
                $"Fetched {messages.Count} pinned messages from channel {channelId}");
            return messages;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetPinnedMessagesAsync), start, exception, $"pins in channel {channelId}");
            throw;
        }
    }

    public async Task PinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.PinMessageAsync(channelId, messageId, reason, cancellationToken);
            Succeeded(nameof(PinMessageAsync), start, LogLevel.Information,
                $"Pinned message {messageId} in channel {channelId}{Because(reason)}");
            Emit(new MessagePinned(channelId, messageId, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(PinMessageAsync), start, exception, $"message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async Task UnpinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.UnpinMessageAsync(channelId, messageId, reason, cancellationToken);
            Succeeded(nameof(UnpinMessageAsync), start, LogLevel.Information,
                $"Unpinned message {messageId} in channel {channelId}{Because(reason)}");
            Emit(new MessageUnpinned(channelId, messageId, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(UnpinMessageAsync), start, exception, $"message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async Task TriggerTypingAsync(Snowflake channelId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.TriggerTypingAsync(channelId, cancellationToken);
            Succeeded(nameof(TriggerTypingAsync), start, LogLevel.Trace, $"Triggered typing in channel {channelId}");
            Emit(new TypingTriggered(channelId));
        }
        catch (Exception exception)
        {
            Failed(nameof(TriggerTypingAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordUser>> GetReactionsAsync(Snowflake channelId, Snowflake messageId,
        DiscordEmoji emoji, ReactionQuery? query = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var users = await _inner.GetReactionsAsync(channelId, messageId, emoji, query, cancellationToken);
            Succeeded(nameof(GetReactionsAsync), start, LogLevel.Debug,
                $"Fetched {users.Count} reactors of {Describe(emoji)} on message {messageId}");
            return users;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetReactionsAsync), start, exception,
                $"{Describe(emoji)} on message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async Task DeleteUserReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        Snowflake userId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteUserReactionAsync(channelId, messageId, emoji, userId, cancellationToken);
            Succeeded(nameof(DeleteUserReactionAsync), start, LogLevel.Debug,
                $"Removed {Describe(emoji)} by {userId} from message {messageId}");
            Emit(new UserReactionRemoved(channelId, messageId, Describe(emoji), userId));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteUserReactionAsync), start, exception,
                $"{Describe(emoji)} by {userId} on message {messageId}");
            throw;
        }
    }

    public async Task DeleteAllReactionsAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteAllReactionsAsync(channelId, messageId, cancellationToken);
            Succeeded(nameof(DeleteAllReactionsAsync), start, LogLevel.Information,
                $"Cleared all reactions on message {messageId} in channel {channelId}");
            Emit(new ReactionsCleared(channelId, messageId, null));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteAllReactionsAsync), start, exception, $"message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async Task DeleteEmojiReactionsAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteEmojiReactionsAsync(channelId, messageId, emoji, cancellationToken);
            Succeeded(nameof(DeleteEmojiReactionsAsync), start, LogLevel.Information,
                $"Cleared {Describe(emoji)} reactions on message {messageId}");
            Emit(new ReactionsCleared(channelId, messageId, Describe(emoji)));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteEmojiReactionsAsync), start, exception,
                $"{Describe(emoji)} on message {messageId} in channel {channelId}");
            throw;
        }
    }

    public async Task<DiscordUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var user = await _inner.GetCurrentUserAsync(cancellationToken);
            Succeeded(nameof(GetCurrentUserAsync), start, LogLevel.Debug,
                $"Fetched current user {user.Username} ({user.Id})");
            return user;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetCurrentUserAsync), start, exception, "current user");
            throw;
        }
    }

    public async Task<DiscordUser> GetUserAsync(Snowflake userId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var user = await _inner.GetUserAsync(userId, cancellationToken);
            Succeeded(nameof(GetUserAsync), start, LogLevel.Debug, $"Fetched user {user.Username} ({userId})");
            return user;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetUserAsync), start, exception, $"user {userId}");
            throw;
        }
    }

    public async Task<DiscordChannel> CreateDirectMessageChannelAsync(Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var channel = await _inner.CreateDirectMessageChannelAsync(userId, cancellationToken);
            Succeeded(nameof(CreateDirectMessageChannelAsync), start, LogLevel.Information,
                $"Opened direct channel {channel.Id} with user {userId}");
            Emit(new DirectChannelOpened(userId, channel.Id));
            return channel;
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateDirectMessageChannelAsync), start, exception, $"user {userId}");
            throw;
        }
    }

    public async Task LeaveGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.LeaveGuildAsync(guildId, cancellationToken);
            Succeeded(nameof(LeaveGuildAsync), start, LogLevel.Warning, $"Left guild {guildId}");
            Emit(new GuildLeft(guildId));
        }
        catch (Exception exception)
        {
            Failed(nameof(LeaveGuildAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<GatewayBotInfo> GetGatewayBotAsync(CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var info = await _inner.GetGatewayBotAsync(cancellationToken);
            Succeeded(nameof(GetGatewayBotAsync), start, LogLevel.Information,
                $"Gateway {info.Url} recommends {info.Shards} shards, " +
                $"{info.SessionStartLimit.Remaining}/{info.SessionStartLimit.Total} sessions left");
            return info;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGatewayBotAsync), start, exception, "gateway bot info");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordInvite>> GetChannelInvitesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var invites = await _inner.GetChannelInvitesAsync(channelId, cancellationToken);
            Succeeded(nameof(GetChannelInvitesAsync), start, LogLevel.Debug,
                $"Fetched {invites.Count} invites for channel {channelId}");
            return invites;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetChannelInvitesAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordInvite>> GetGuildInvitesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var invites = await _inner.GetGuildInvitesAsync(guildId, cancellationToken);
            Succeeded(nameof(GetGuildInvitesAsync), start, LogLevel.Debug,
                $"Fetched {invites.Count} invites for guild {guildId}");
            return invites;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildInvitesAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordInvite> CreateChannelInviteAsync(Snowflake channelId,
        InviteCreateRequest? request = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var invite = await _inner.CreateChannelInviteAsync(channelId, request, reason, cancellationToken);
            Succeeded(nameof(CreateChannelInviteAsync), start, LogLevel.Information,
                $"Created invite {invite.Code} for channel {channelId}{Because(reason)}");
            Emit(new InviteIssued(channelId, invite.Code, invite.MaxUses, invite.MaxAge));
            return invite;
        }
        catch (Exception exception)
        {
            Failed(nameof(CreateChannelInviteAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<DiscordInvite> GetInviteAsync(string code, bool withCounts = false,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var invite = await _inner.GetInviteAsync(code, withCounts, cancellationToken);
            Succeeded(nameof(GetInviteAsync), start, LogLevel.Debug, $"Fetched invite {code}");
            return invite;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetInviteAsync), start, exception, $"invite {code}");
            throw;
        }
    }

    public async Task DeleteInviteAsync(string code, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.DeleteInviteAsync(code, reason, cancellationToken);
            Succeeded(nameof(DeleteInviteAsync), start, LogLevel.Warning, $"Deleted invite {code}{Because(reason)}");
            Emit(new InviteRevoked(code, reason));
        }
        catch (Exception exception)
        {
            Failed(nameof(DeleteInviteAsync), start, exception, $"invite {code}");
            throw;
        }
    }

    public async Task<DiscordAuditLog> GetGuildAuditLogAsync(Snowflake guildId, AuditLogQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var log = await _inner.GetGuildAuditLogAsync(guildId, query, cancellationToken);
            Succeeded(nameof(GetGuildAuditLogAsync), start, LogLevel.Debug,
                $"Fetched {log.Entries.Count} audit log entries for guild {guildId}");
            return log;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildAuditLogAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<DiscordGuild> ModifyGuildAsync(Snowflake guildId, GuildModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var guild = await _inner.ModifyGuildAsync(guildId, request, reason, cancellationToken);
            Succeeded(nameof(ModifyGuildAsync), start, LogLevel.Information,
                $"Modified guild {guild.Name} ({guildId}){Because(reason)}");
            Emit(new GuildModified(guildId, Changes(request), reason));
            return guild;
        }
        catch (Exception exception)
        {
            Failed(nameof(ModifyGuildAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<int> GetGuildPruneCountAsync(Snowflake guildId, PruneRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var count = await _inner.GetGuildPruneCountAsync(guildId, request, cancellationToken);
            Succeeded(nameof(GetGuildPruneCountAsync), start, LogLevel.Debug,
                $"Prune of guild {guildId} would remove {count} members");
            return count;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildPruneCountAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<int?> BeginGuildPruneAsync(Snowflake guildId, PruneRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        var days = (request ?? new PruneRequest()).Days;

        try
        {
            var removed = await _inner.BeginGuildPruneAsync(guildId, request, reason, cancellationToken);
            Succeeded(nameof(BeginGuildPruneAsync), start, LogLevel.Warning,
                $"Pruned {Report(removed)} members inactive for {days} days from guild {guildId}{Because(reason)}");
            Emit(new GuildPruned(guildId, days, removed, reason));
            return removed;
        }
        catch (Exception exception)
        {
            Failed(nameof(BeginGuildPruneAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<ThreadListing> GetActiveThreadsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var listing = await _inner.GetActiveThreadsAsync(guildId, cancellationToken);
            Succeeded(nameof(GetActiveThreadsAsync), start, LogLevel.Debug,
                $"Fetched {listing.Count} active threads in guild {guildId}");
            return listing;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetActiveThreadsAsync), start, exception, $"guild {guildId}");
            throw;
        }
    }

    public async Task<ThreadListing> GetPublicArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var listing = await _inner.GetPublicArchivedThreadsAsync(channelId, query, cancellationToken);
            Succeeded(nameof(GetPublicArchivedThreadsAsync), start, LogLevel.Debug,
                $"Fetched {listing.Count} public archived threads in channel {channelId}");
            return listing;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetPublicArchivedThreadsAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<ThreadListing> GetPrivateArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var listing = await _inner.GetPrivateArchivedThreadsAsync(channelId, query, cancellationToken);
            Succeeded(nameof(GetPrivateArchivedThreadsAsync), start, LogLevel.Debug,
                $"Fetched {listing.Count} private archived threads in channel {channelId}");
            return listing;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetPrivateArchivedThreadsAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task<ThreadListing> GetJoinedPrivateArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var listing = await _inner.GetJoinedPrivateArchivedThreadsAsync(channelId, query, cancellationToken);
            Succeeded(nameof(GetJoinedPrivateArchivedThreadsAsync), start, LogLevel.Debug,
                $"Fetched {listing.Count} joined private archived threads in channel {channelId}");
            return listing;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetJoinedPrivateArchivedThreadsAsync), start, exception, $"channel {channelId}");
            throw;
        }
    }

    public async Task JoinThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.JoinThreadAsync(threadId, cancellationToken);
            Succeeded(nameof(JoinThreadAsync), start, LogLevel.Information, $"Joined thread {threadId}");
            Emit(new ThreadJoined(threadId));
        }
        catch (Exception exception)
        {
            Failed(nameof(JoinThreadAsync), start, exception, $"thread {threadId}");
            throw;
        }
    }

    public async Task LeaveThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.LeaveThreadAsync(threadId, cancellationToken);
            Succeeded(nameof(LeaveThreadAsync), start, LogLevel.Information, $"Left thread {threadId}");
            Emit(new ThreadLeft(threadId));
        }
        catch (Exception exception)
        {
            Failed(nameof(LeaveThreadAsync), start, exception, $"thread {threadId}");
            throw;
        }
    }

    public async Task AddThreadMemberAsync(Snowflake threadId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.AddThreadMemberAsync(threadId, userId, cancellationToken);
            Succeeded(nameof(AddThreadMemberAsync), start, LogLevel.Information,
                $"Added user {userId} to thread {threadId}");
            Emit(new ThreadMemberAdded(threadId, userId));
        }
        catch (Exception exception)
        {
            Failed(nameof(AddThreadMemberAsync), start, exception, $"user {userId} in thread {threadId}");
            throw;
        }
    }

    public async Task RemoveThreadMemberAsync(Snowflake threadId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _inner.RemoveThreadMemberAsync(threadId, userId, cancellationToken);
            Succeeded(nameof(RemoveThreadMemberAsync), start, LogLevel.Information,
                $"Removed user {userId} from thread {threadId}");
            Emit(new ThreadMemberRemoved(threadId, userId));
        }
        catch (Exception exception)
        {
            Failed(nameof(RemoveThreadMemberAsync), start, exception, $"user {userId} in thread {threadId}");
            throw;
        }
    }

    public async Task<DiscordThreadMember> GetThreadMemberAsync(Snowflake threadId, Snowflake userId,
        bool withMember = false, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var member = await _inner.GetThreadMemberAsync(threadId, userId, withMember, cancellationToken);
            Succeeded(nameof(GetThreadMemberAsync), start, LogLevel.Debug,
                $"Fetched membership of {userId} in thread {threadId}");
            return member;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetThreadMemberAsync), start, exception, $"user {userId} in thread {threadId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordThreadMember>> GetThreadMembersAsync(Snowflake threadId,
        bool withMember = false, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var members = await _inner.GetThreadMembersAsync(threadId, withMember, cancellationToken);
            Succeeded(nameof(GetThreadMembersAsync), start, LogLevel.Debug,
                $"Fetched {members.Count} members of thread {threadId}");
            return members;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetThreadMembersAsync), start, exception, $"thread {threadId}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscordCommandPermissions>> GetGuildCommandPermissionsAsync(
        Snowflake applicationId, Snowflake guildId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var permissions = await _inner.GetGuildCommandPermissionsAsync(applicationId, guildId, cancellationToken);
            Succeeded(nameof(GetGuildCommandPermissionsAsync), start, LogLevel.Debug,
                $"Fetched {permissions.Count} command permission sets for {Scope(applicationId, guildId)}");
            return permissions;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetGuildCommandPermissionsAsync), start, exception, Scope(applicationId, guildId));
            throw;
        }
    }

    public async Task<DiscordCommandPermissions> GetCommandPermissionsAsync(Snowflake applicationId,
        Snowflake guildId, Snowflake commandId, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var permissions =
                await _inner.GetCommandPermissionsAsync(applicationId, guildId, commandId, cancellationToken);
            Succeeded(nameof(GetCommandPermissionsAsync), start, LogLevel.Debug,
                $"Fetched {permissions.Permissions.Count} permissions for command {commandId} in guild {guildId}");
            return permissions;
        }
        catch (Exception exception)
        {
            Failed(nameof(GetCommandPermissionsAsync), start, exception,
                $"command {commandId} in {Scope(applicationId, guildId)}");
            throw;
        }
    }

    private static string Report(int? removed) => removed?.ToString() ?? "an unreported number of";

    private static string Describe(MessageQuery query) => query switch
    {
        { Around: { } around } => $"around {around}",
        { After: { } after } => $"after {after}",
        { Before: { } before } => $"before {before}",
        _ => "newest"
    };

    private static string Changes(GuildModifyRequest request)
    {
        var changes = new List<string>(8);

        if (request.Name is not null)
            changes.Add("name");

        if (request.Description is not null)
            changes.Add("description");

        if (request.OwnerId is not null)
            changes.Add("owner");

        if (request.AfkChannelId is not null || request.AfkTimeout is not null)
            changes.Add("afk");

        if (request.SystemChannelId is not null || request.RulesChannelId is not null ||
            request.PublicUpdatesChannelId is not null)
            changes.Add("channels");

        if (request.VerificationLevel is not null || request.DefaultMessageNotifications is not null ||
            request.ExplicitContentFilter is not null)
            changes.Add("moderation");

        if (request.PreferredLocale is not null)
            changes.Add("locale");

        if (request.IconData is not null || request.BannerData is not null || request.SplashData is not null)
            changes.Add("assets");

        return string.Join(',', changes);
    }

    private static string Changes(MemberModifyRequest request)
    {
        var changes = new List<string>(6);

        if (request.Nickname is not null)
            changes.Add("nickname");

        if (request.Roles is not null)
            changes.Add("roles");

        if (request.Mute is not null)
            changes.Add("mute");

        if (request.Deaf is not null)
            changes.Add("deaf");

        if (request.VoiceChannelId is not null)
            changes.Add("voice");

        if (request.ClearTimeout || request.CommunicationDisabledUntil is not null)
            changes.Add("timeout");

        return string.Join(',', changes);
    }

    private static string Scope(Snowflake applicationId, Snowflake? guildId) =>
        guildId is { } guild ? $"application {applicationId} in guild {guild}" : $"application {applicationId}";

    public ValueTask DisposeAsync() => _inner.DisposeAsync();

    private static string Describe(DiscordEmoji emoji) => emoji.Id is null ? emoji.Name : $"{emoji.Name}:{emoji.Id}";

    private static string Uploaded(IReadOnlyList<DiscordFile>? files) =>
        files is { Count: > 0 } ? $" with {files.Count} attachment(s)" : string.Empty;

    private static string Showing(IReadOnlyList<DiscordComponent>? components) =>
        components is { Count: > 0 } ? $" showing {components.Count} component row(s)" : string.Empty;

    private static string Because(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? string.Empty : $" (reason: {reason})";

    private void Succeeded(string operation, long start, LogLevel level, string message)
    {
        var duration = Stopwatch.GetElapsedTime(start);

        if (_logger.IsEnabled(level))
            _logger.Log(level, $"{message} in {duration.TotalMilliseconds:F0}ms");

        Emit(new RestOperationCompleted(operation, duration));
    }

    private void Failed(string operation, long start, Exception exception, string context)
    {
        var duration = Stopwatch.GetElapsedTime(start);

        if (exception is OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"{operation} canceled for {context} after {duration.TotalMilliseconds:F0}ms");
        }
        else
        {
            _logger.LogError($"{operation} failed for {context} after {duration.TotalMilliseconds:F0}ms", exception);
        }

        Emit(new RestOperationFailed(operation, exception.GetType().Name, duration));
    }

    private void Emit(TelemetryEvent telemetryEvent)
    {
        if (_telemetry.HasSubscribers)
            _telemetry.Emit(telemetryEvent);
    }
}

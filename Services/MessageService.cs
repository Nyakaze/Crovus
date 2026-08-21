using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class MessageService : DiscordService
{
    public MessageService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Message", logger, telemetry)
    {
    }

    public MessageService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task<DiscordMessage> GetAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetAsync), $"message {messageId} in channel {channelId}",
            () => Rest.GetMessageAsync(channelId, messageId, cancellationToken),
            message => $"Loaded message {message.Id} from channel {channelId}", LogLevel.Debug);

    public async Task<IReadOnlyList<DiscordMessage>> GetHistoryAsync(Snowflake channelId, int? limit = null,
        Snowflake? before = null, CancellationToken cancellationToken = default) =>
        await TrackAsync(nameof(GetHistoryAsync), $"channel {channelId}",
            async () =>
            {
                var history = new List<DiscordMessage>();

                await foreach (var message in Rest.GetMessagesAsync(channelId, before, limit, cancellationToken))
                    history.Add(message);

                return (IReadOnlyList<DiscordMessage>)history;
            },
            messages => $"Loaded {messages.Count} messages from channel {channelId}", LogLevel.Debug);

    public Task<DiscordMessage> SendAsync(Snowflake channelId, MessageCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(SendAsync), $"channel {channelId}",
            () => Rest.CreateMessageAsync(channelId, request, cancellationToken),
            message => $"Sent message {message.Id} to channel {channelId}");
    }

    public Task<DiscordMessage> SendAsync(Snowflake channelId, string content,
        CancellationToken cancellationToken = default) =>
        SendAsync(channelId, MessageFactory.Create(content).Build(), cancellationToken);

    public Task<DiscordMessage> SendAsync(Snowflake channelId, MessageFactory message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return SendAsync(channelId, message.Build(), cancellationToken);
    }

    public Task<DiscordMessage> SendAsync(Snowflake channelId, Action<MessageFactory> configure,
        CancellationToken cancellationToken = default) =>
        SendAsync(channelId, Compose(configure), cancellationToken);

    public Task<DiscordMessage> SendAsync(Snowflake channelId, DiscordFile file, string? content = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        return SendAsync(channelId, MessageFactory.Create().WithContent(content).AddFile(file).Build(),
            cancellationToken);
    }

    public Task<DiscordMessage> SendAsync(Snowflake channelId, IEnumerable<DiscordFile> files,
        string? content = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        return SendAsync(channelId, MessageFactory.Create().WithContent(content).AddFiles(files).Build(),
            cancellationToken);
    }

    public async Task<DiscordMessage> SendFileAsync(Snowflake channelId, string path, string? content = null,
        string? description = null, CancellationToken cancellationToken = default)
    {
        var file = await DiscordFile.FromPathAsync(path, description: description,
            cancellationToken: cancellationToken);

        return await SendAsync(channelId, file, content, cancellationToken);
    }

    public Task<DiscordMessage> ReplyAsync(DiscordMessage message, Action<MessageFactory> configure,
        bool failIfNotExists = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(configure);

        var reply = MessageFactory.Create();
        configure(reply);
        reply.ReplyTo(message, failIfNotExists);

        return SendAsync(message.ChannelId, reply.Build(), cancellationToken);
    }

    public Task<DiscordMessage> ReplyAsync(DiscordMessage message, string content, bool failIfNotExists = false,
        CancellationToken cancellationToken = default) =>
        ReplyAsync(message, reply => reply.WithContent(content), failIfNotExists, cancellationToken);

    public Task<DiscordMessage> ForwardAsync(DiscordMessage message, Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return SendAsync(channelId, MessageFactory.Create().Forward(message).Build(), cancellationToken);
    }

    public Task<DiscordMessage> EditAsync(Snowflake channelId, Snowflake messageId, MessageEditRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(EditAsync), $"message {messageId} in channel {channelId}",
            () => Rest.EditMessageAsync(channelId, messageId, request, cancellationToken),
            message => $"Edited message {message.Id} in channel {channelId}");
    }

    public Task<DiscordMessage> EditAsync(Snowflake channelId, Snowflake messageId, Action<MessageFactory> configure,
        CancellationToken cancellationToken = default) =>
        EditAsync(channelId, messageId, Compose(configure).BuildEdit(), cancellationToken);

    public Task<DiscordMessage> EditAsync(DiscordMessage message, Action<MessageFactory> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(configure);

        var factory = MessageFactory.From(message);
        configure(factory);

        return EditAsync(message.ChannelId, message.Id, factory.BuildEdit(), cancellationToken);
    }

    public Task DeleteAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteAsync), $"message {messageId} in channel {channelId}",
            () => Rest.DeleteMessageAsync(channelId, messageId, reason, cancellationToken),
            $"Deleted message {messageId} from channel {channelId}{Because(reason)}");

    public Task DeleteAsync(DiscordMessage message, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return DeleteAsync(message.ChannelId, message.Id, reason, cancellationToken);
    }

    public async Task<BroadcastResult> BroadcastAsync(IEnumerable<Snowflake> channelIds, MessageCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(channelIds);
        ArgumentNullException.ThrowIfNull(request);

        var targets = channelIds.Distinct().ToArray();

        if (targets.Length == 0)
            return BroadcastResult.Empty;

        var delivered = new List<DiscordMessage>(targets.Length);
        var failures = new List<BroadcastFailure>();

        var result = await TrackAsync(nameof(BroadcastAsync), $"{targets.Length} channels",
            async () =>
            {
                foreach (var channelId in targets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        delivered.Add(await Rest.CreateMessageAsync(channelId, request, cancellationToken));
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        Warn($"Broadcast to channel {channelId} failed", exception);
                        failures.Add(new BroadcastFailure(channelId, exception));
                    }
                }

                return new BroadcastResult(delivered, failures);
            },
            broadcast => $"Broadcast reached {broadcast.Delivered.Count} of {broadcast.Targets} channels");

        Emit(new MessageBroadcast(result.Targets, result.Delivered.Count, result.Failures.Count));

        return result;
    }

    public Task<BroadcastResult> BroadcastAsync(IEnumerable<Snowflake> channelIds, Action<MessageFactory> configure,
        CancellationToken cancellationToken = default) =>
        BroadcastAsync(channelIds, Compose(configure).Build(), cancellationToken);

    public Task<BroadcastResult> BroadcastAsync(IEnumerable<Snowflake> channelIds, string content,
        CancellationToken cancellationToken = default) =>
        BroadcastAsync(channelIds, MessageFactory.Create(content).Build(), cancellationToken);

    public async Task<PurgeResult> PurgeAsync(Snowflake channelId, int count,
        Func<DiscordMessage, bool>? predicate = null, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var deleted = 0;
        var failed = 0;

        var result = await TrackAsync(nameof(PurgeAsync), $"channel {channelId}",
            async () =>
            {
                var doomed = new List<Snowflake>(count);

                await foreach (var message in Rest.GetMessagesAsync(channelId, null, null, cancellationToken))
                {
                    if (predicate is not null && !predicate(message))
                        continue;

                    doomed.Add(message.Id);

                    if (doomed.Count == count)
                        break;
                }

                foreach (var messageId in doomed)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await Rest.DeleteMessageAsync(channelId, messageId, reason, cancellationToken);
                        deleted++;
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        Warn($"Purging message {messageId} from channel {channelId} failed", exception);
                        failed++;
                    }
                }

                return new PurgeResult(deleted, failed);
            },
            purge => $"Purged {purge.Deleted} of {purge.Attempted} messages from channel {channelId}{Because(reason)}");

        Emit(new MessagesPurged(channelId.Value, result.Deleted, result.Failed));

        return result;
    }

    private static MessageFactory Compose(Action<MessageFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = MessageFactory.Create();
        configure(factory);

        return factory;
    }
}

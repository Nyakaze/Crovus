using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class EmbedService : DiscordService
{
    public EmbedService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Embed", logger, telemetry)
    {
    }

    public EmbedService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task<DiscordMessage> SendAsync(Snowflake channelId, DiscordEmbed embed, string? content = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embed);

        var request = new MessageCreateRequest(content, [embed]);

        return TrackAsync(nameof(SendAsync), $"channel {channelId}",
            () => Rest.CreateMessageAsync(channelId, request, cancellationToken),
            message => $"Sent embed as message {message.Id} to channel {channelId}");
    }

    public Task<DiscordMessage> SendAsync(Snowflake channelId, Action<EmbedFactory> configure, string? content = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(channelId, Compose(configure), content, cancellationToken);

    public Task<DiscordMessage> SendAsync(Snowflake channelId, IReadOnlyList<DiscordEmbed> embeds,
        string? content = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embeds);

        if (embeds.Count == 0)
            throw new ArgumentException("An embed message needs at least one embed.", nameof(embeds));

        if (embeds.Count > DiscordLimits.MessageEmbeds)
            throw new ArgumentException(
                $"A message carries at most {DiscordLimits.MessageEmbeds} embeds but {embeds.Count} were given.",
                nameof(embeds));

        var request = new MessageCreateRequest(content, embeds);

        return TrackAsync(nameof(SendAsync), $"channel {channelId}",
            () => Rest.CreateMessageAsync(channelId, request, cancellationToken),
            message => $"Sent {embeds.Count} embeds as message {message.Id} to channel {channelId}");
    }

    public Task<DiscordMessage> ReplaceAsync(Snowflake channelId, Snowflake messageId, DiscordEmbed embed,
        string? content = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embed);

        var request = new MessageEditRequest(content, [embed]);

        return TrackAsync(nameof(ReplaceAsync), $"message {messageId} in channel {channelId}",
            () => Rest.EditMessageAsync(channelId, messageId, request, cancellationToken),
            message => $"Replaced the embed of message {message.Id} in channel {channelId}");
    }

    public Task<DiscordMessage> ReplaceAsync(Snowflake channelId, Snowflake messageId, Action<EmbedFactory> configure,
        string? content = null, CancellationToken cancellationToken = default) =>
        ReplaceAsync(channelId, messageId, Compose(configure), content, cancellationToken);

    public Task<DiscordMessage> UpdateAsync(DiscordMessage message, Action<EmbedFactory> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(configure);

        var factory = message.Embeds.Count == 0 ? EmbedFactory.Create() : EmbedFactory.From(message.Embeds[0]);
        configure(factory);

        return ReplaceAsync(message.ChannelId, message.Id, factory.Build(), message.Content, cancellationToken);
    }

    public async Task<BroadcastResult> BroadcastAsync(IEnumerable<Snowflake> channelIds, DiscordEmbed embed,
        string? content = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(channelIds);
        ArgumentNullException.ThrowIfNull(embed);

        var targets = channelIds.Distinct().ToArray();

        if (targets.Length == 0)
            return BroadcastResult.Empty;

        var request = new MessageCreateRequest(content, [embed]);
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
                        Warn($"Embed broadcast to channel {channelId} failed", exception);
                        failures.Add(new BroadcastFailure(channelId, exception));
                    }
                }

                return new BroadcastResult(delivered, failures);
            },
            broadcast => $"Embed broadcast reached {broadcast.Delivered.Count} of {broadcast.Targets} channels");

        Emit(new MessageBroadcast(result.Targets, result.Delivered.Count, result.Failures.Count));

        return result;
    }

    public Task<BroadcastResult> BroadcastAsync(IEnumerable<Snowflake> channelIds, Action<EmbedFactory> configure,
        string? content = null, CancellationToken cancellationToken = default) =>
        BroadcastAsync(channelIds, Compose(configure), content, cancellationToken);

    private static DiscordEmbed Compose(Action<EmbedFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = EmbedFactory.Create();
        configure(factory);

        return factory.Build();
    }
}

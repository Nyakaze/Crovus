using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class WebhookService : DiscordService
{
    public WebhookService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Webhook", logger, telemetry)
    {
    }

    public WebhookService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task<IReadOnlyList<DiscordWebhook>> GetForChannelAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetForChannelAsync), $"channel {channelId}",
            () => Rest.GetChannelWebhooksAsync(channelId, cancellationToken),
            webhooks => $"Loaded {webhooks.Count} webhooks of channel {channelId}", LogLevel.Debug);

    public Task<DiscordWebhook> GetAsync(Snowflake webhookId, string? token = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetAsync), $"webhook {webhookId}",
            () => Rest.GetWebhookAsync(webhookId, token, cancellationToken),
            webhook => $"Loaded webhook {webhook.Name ?? "unnamed"} ({webhook.Id})", LogLevel.Debug);

    public Task<DiscordWebhook> CreateAsync(Snowflake channelId, WebhookCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(CreateAsync), $"channel {channelId}",
            () => Rest.CreateWebhookAsync(channelId, request, reason, cancellationToken),
            webhook => $"Created webhook {webhook.Name ?? request.Name} ({webhook.Id}) in channel {channelId}" +
                       Because(reason));
    }

    public Task<DiscordWebhook> CreateAsync(Snowflake channelId, string name,
        Action<WebhookFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var factory = WebhookFactory.Create(name);
        configure?.Invoke(factory);

        return CreateAsync(channelId, factory.Build(), reason, cancellationToken);
    }

    public async Task<DiscordWebhook> GetOrCreateAsync(Snowflake channelId, string name,
        Action<WebhookFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        WebhookFactory.ValidateName(name);

        var created = false;

        var webhook = await TrackAsync(nameof(GetOrCreateAsync), $"webhook {name} in channel {channelId}",
            async () =>
            {
                var existing = await Rest.GetChannelWebhooksAsync(channelId, cancellationToken);

                foreach (var candidate in existing)
                {
                    if (candidate.CanExecute && string.Equals(candidate.Name, name, StringComparison.Ordinal))
                        return candidate;
                }

                var factory = WebhookFactory.Create(name);
                configure?.Invoke(factory);
                created = true;

                return await Rest.CreateWebhookAsync(channelId, factory.Build(), reason, cancellationToken);
            },
            resolved => $"{(created ? "Created" : "Reused")} webhook {name} ({resolved.Id}) in channel {channelId}");

        Emit(new WebhookResolved(channelId.Value, webhook.Id.Value, created));

        return webhook;
    }

    public Task<DiscordWebhook> ModifyAsync(Snowflake webhookId, WebhookModifyRequest request, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(ModifyAsync), $"webhook {webhookId}",
            () => Rest.ModifyWebhookAsync(webhookId, request, reason, cancellationToken),
            webhook => $"Modified webhook {webhook.Name ?? "unnamed"} ({webhook.Id}){Because(reason)}");
    }

    public Task<DiscordWebhook> ModifyAsync(Snowflake webhookId, Action<WebhookFactory> configure,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = WebhookFactory.Modify();
        configure(factory);

        return ModifyAsync(webhookId, factory.BuildModify(), reason, cancellationToken);
    }

    public Task<DiscordWebhook> RenameAsync(Snowflake webhookId, string name, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(webhookId, webhook => webhook.WithName(name), reason, cancellationToken);

    public Task<DiscordWebhook> MoveAsync(Snowflake webhookId, Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(webhookId, webhook => webhook.MoveTo(channelId), reason, cancellationToken);

    public Task DeleteAsync(Snowflake webhookId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteAsync), $"webhook {webhookId}",
            () => Rest.DeleteWebhookAsync(webhookId, reason, cancellationToken),
            $"Deleted webhook {webhookId}{Because(reason)}");

    public Task<DiscordMessage?> SendAsync(DiscordWebhook webhook, WebhookExecuteRequest request,
        Snowflake? threadId = null, bool wait = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhook);
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(SendAsync), $"webhook {webhook.Id}",
            () => Rest.ExecuteWebhookAsync(webhook, request, threadId, wait, cancellationToken),
            message => message is null
                ? $"Executed webhook {webhook.Id} without awaiting the message"
                : $"Executed webhook {webhook.Id} producing message {message.Id}");
    }

    public Task<DiscordMessage?> SendAsync(DiscordWebhook webhook, Action<WebhookMessageFactory> configure,
        Snowflake? threadId = null, bool wait = false, CancellationToken cancellationToken = default) =>
        SendAsync(webhook, Compose(configure), threadId, wait, cancellationToken);

    public Task<DiscordMessage?> SendAsync(DiscordWebhook webhook, string content, Snowflake? threadId = null,
        bool wait = false, CancellationToken cancellationToken = default) =>
        SendAsync(webhook, WebhookMessageFactory.Create(content).Build(), threadId, wait, cancellationToken);

    public Task<DiscordMessage> EditMessageAsync(DiscordWebhook webhook, Snowflake messageId,
        MessageEditRequest request, Snowflake? threadId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhook);
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(EditMessageAsync), $"message {messageId} of webhook {webhook.Id}",
            () => Rest.EditWebhookMessageAsync(webhook, messageId, request, threadId, cancellationToken),
            message => $"Edited webhook message {message.Id}");
    }

    public Task<DiscordMessage> EditMessageAsync(DiscordWebhook webhook, Snowflake messageId,
        Action<MessageFactory> configure, Snowflake? threadId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = MessageFactory.Create();
        configure(factory);

        return EditMessageAsync(webhook, messageId, factory.BuildEdit(), threadId, cancellationToken);
    }

    public Task DeleteMessageAsync(DiscordWebhook webhook, Snowflake messageId, Snowflake? threadId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhook);

        return TrackAsync(nameof(DeleteMessageAsync), $"message {messageId} of webhook {webhook.Id}",
            () => Rest.DeleteWebhookMessageAsync(webhook, messageId, threadId, cancellationToken),
            $"Deleted webhook message {messageId}");
    }

    public async Task<DiscordMessage?> SendAsAsync(Snowflake channelId, string webhookName,
        Action<WebhookMessageFactory> configure, Snowflake? threadId = null, bool wait = false,
        CancellationToken cancellationToken = default)
    {
        var webhook = await GetOrCreateAsync(channelId, webhookName, cancellationToken: cancellationToken);

        return await SendAsync(webhook, Compose(configure), threadId, wait, cancellationToken);
    }

    public async Task<DiscordMessage?> ImpersonateAsync(Snowflake channelId, DiscordUser user,
        Action<WebhookMessageFactory> configure, string webhookName = "Crovus", Snowflake? threadId = null,
        bool wait = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(configure);

        var webhook = await GetOrCreateAsync(channelId, webhookName, cancellationToken: cancellationToken);
        var message = WebhookMessageFactory.Impersonating(user);
        configure(message);

        return await SendAsync(webhook, message.Build(), threadId, wait, cancellationToken);
    }

    private static WebhookExecuteRequest Compose(Action<WebhookMessageFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = WebhookMessageFactory.Create();
        configure(factory);

        return factory.Build();
    }
}

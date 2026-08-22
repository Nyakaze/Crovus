using Crovus.Factory;

namespace Crovus.Models;

public static class WebhookFluent
{
    public static Task<DiscordMessage?> SendAsync(this DiscordWebhook webhook, string content,
        Snowflake? threadId = null, bool wait = false, CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.SendAsync(webhook, content, threadId, wait, cancellationToken);

    public static Task<DiscordMessage?> SendAsync(this DiscordWebhook webhook, string content,
        DiscordChannel thread, bool wait = false, CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.SendAsync(webhook, content, thread.ThreadId, wait, cancellationToken);

    public static Task<DiscordMessage?> SendAsync(this DiscordWebhook webhook,
        Action<WebhookMessageFactory> configure, Snowflake? threadId = null, bool wait = false,
        CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.SendAsync(webhook, configure, threadId, wait, cancellationToken);

    public static Task<DiscordMessage?> SendAsync(this DiscordWebhook webhook,
        Action<WebhookMessageFactory> configure, DiscordChannel thread, bool wait = false,
        CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.SendAsync(webhook, configure, thread.ThreadId, wait, cancellationToken);

    public static Task<DiscordMessage?> SendAsync(this DiscordWebhook webhook, WebhookExecuteRequest request,
        Snowflake? threadId = null, bool wait = false, CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.SendAsync(webhook, request, threadId, wait, cancellationToken);

    public static Task<DiscordWebhook> ModifyAsync(this DiscordWebhook webhook, WebhookModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.ModifyAsync(webhook.Id, request, reason, cancellationToken);

    public static Task<DiscordWebhook> ModifyAsync(this DiscordWebhook webhook, Action<WebhookFactory> configure,
        string? reason = null, CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.ModifyAsync(webhook.Id, configure, reason, cancellationToken);

    public static Task<DiscordWebhook> RenameAsync(this DiscordWebhook webhook, string name, string? reason = null,
        CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.RenameAsync(webhook.Id, name, reason, cancellationToken);

    public static Task<DiscordWebhook> MoveAsync(this DiscordWebhook webhook, Snowflake channelId,
        string? reason = null, CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.MoveAsync(webhook.Id, channelId, reason, cancellationToken);

    public static Task<DiscordWebhook> MoveAsync(this DiscordWebhook webhook, DiscordChannel channel,
        string? reason = null, CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.MoveAsync(webhook.Id, channel.WebhookChannelId, reason, cancellationToken);

    public static Task DeleteAsync(this DiscordWebhook webhook, string? reason = null,
        CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.DeleteAsync(webhook.Id, reason, cancellationToken);

    public static Task<DiscordChannel> GetChannelAsync(this DiscordWebhook webhook,
        CancellationToken cancellationToken = default) =>
        webhook.Rest().GetChannelAsync(webhook.ChannelId, cancellationToken);

    public static async Task<DiscordGuild?> GetGuildAsync(this DiscordWebhook webhook,
        CancellationToken cancellationToken = default) =>
        webhook.GuildId is { } guildId
            ? await webhook.Services().Guilds.GetAsync(guildId, cancellationToken: cancellationToken)
            : null;

    public static Task<DiscordWebhook> RefreshAsync(this DiscordWebhook webhook,
        CancellationToken cancellationToken = default) =>
        webhook.Services().Webhooks.GetAsync(webhook.Id, webhook.Token, cancellationToken);
}

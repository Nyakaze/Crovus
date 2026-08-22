using Crovus.Factory;
using Crovus.Services;

namespace Crovus.Models;

public static class ChannelFluent
{
    public static Task<DiscordMessage> SendAsync(this DiscordChannel channel, string content,
        CancellationToken cancellationToken = default) =>
        channel.Services().Messages.SendAsync(channel.Id, content, cancellationToken);

    public static Task<DiscordMessage> SendAsync(this DiscordChannel channel, Action<MessageFactory> configure,
        CancellationToken cancellationToken = default) =>
        channel.Services().Messages.SendAsync(channel.Id, configure, cancellationToken);

    public static Task<DiscordMessage> SendAsync(this DiscordChannel channel, MessageFactory message,
        CancellationToken cancellationToken = default) =>
        channel.Services().Messages.SendAsync(channel.Id, message, cancellationToken);

    public static Task<DiscordMessage> SendAsync(this DiscordChannel channel, MessageCreateRequest request,
        CancellationToken cancellationToken = default) =>
        channel.Services().Messages.SendAsync(channel.Id, request, cancellationToken);

    public static Task<DiscordMessage> SendAsync(this DiscordChannel channel, DiscordEmbed embed,
        string? content = null, CancellationToken cancellationToken = default) =>
        channel.Services().Messages.SendAsync(channel.Id,
            message => message.WithContent(content).AddEmbed(embed), cancellationToken);

    public static Task<DiscordMessage> SendAsync(this DiscordChannel channel, DiscordFile file,
        string? content = null, CancellationToken cancellationToken = default) =>
        channel.Services().Messages.SendAsync(channel.Id, file, content, cancellationToken);

    public static Task<DiscordMessage> SendAsync(this DiscordChannel channel, IEnumerable<DiscordFile> files,
        string? content = null, CancellationToken cancellationToken = default) =>
        channel.Services().Messages.SendAsync(channel.Id, files, content, cancellationToken);

    public static Task<DiscordMessage> SendFileAsync(this DiscordChannel channel, string path,
        string? content = null, string? description = null, CancellationToken cancellationToken = default) =>
        channel.Services().Messages.SendFileAsync(channel.Id, path, content, description, cancellationToken);

    public static Task<DiscordMessage> GetMessageAsync(this DiscordChannel channel, Snowflake messageId,
        CancellationToken cancellationToken = default) =>
        channel.Services().Messages.GetAsync(channel.Id, messageId, cancellationToken);

    public static Task<IReadOnlyList<DiscordMessage>> GetHistoryAsync(this DiscordChannel channel,
        int? limit = null, Snowflake? before = null, CancellationToken cancellationToken = default) =>
        channel.Services().Messages.GetHistoryAsync(channel.Id, limit, before, cancellationToken);

    public static IAsyncEnumerable<DiscordMessage> GetMessagesAsync(this DiscordChannel channel,
        Snowflake? before = null, int? limit = null, CancellationToken cancellationToken = default) =>
        channel.Rest().GetMessagesAsync(channel.Id, before, limit, cancellationToken);

    public static Task<IReadOnlyList<DiscordMessage>> GetPinnedMessagesAsync(this DiscordChannel channel,
        CancellationToken cancellationToken = default) =>
        channel.Rest().GetPinnedMessagesAsync(channel.Id, cancellationToken);

    public static Task<PurgeResult> PurgeAsync(this DiscordChannel channel, int count,
        Func<DiscordMessage, bool>? predicate = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        channel.Services().Messages.PurgeAsync(channel.Id, count, predicate, reason, cancellationToken);

    public static Task TypingAsync(this DiscordChannel channel, CancellationToken cancellationToken = default) =>
        channel.Rest().TriggerTypingAsync(channel.Id, cancellationToken);

    public static Task<DiscordChannel> RefreshAsync(this DiscordChannel channel,
        CancellationToken cancellationToken = default) =>
        channel.Rest().GetChannelAsync(channel.Id, cancellationToken);

    public static Task<DiscordChannel> ModifyAsync(this DiscordChannel channel, ChannelModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        channel.Services().Channels.ModifyAsync(channel.Id, request, reason, cancellationToken);

    public static Task<DiscordChannel> ModifyAsync(this DiscordChannel channel, Action<ChannelFactory> configure,
        string? reason = null, CancellationToken cancellationToken = default) =>
        channel.Services().Channels.ModifyAsync(channel.Id, configure, reason, cancellationToken);

    public static Task<DiscordChannel> RenameAsync(this DiscordChannel channel, string name, string? reason = null,
        CancellationToken cancellationToken = default) =>
        channel.Services().Channels.RenameAsync(channel.Id, name, reason, cancellationToken);

    public static Task<DiscordChannel> MoveAsync(this DiscordChannel channel, Snowflake? categoryId,
        string? reason = null, CancellationToken cancellationToken = default) =>
        channel.Services().Channels.MoveAsync(channel.Id, categoryId, reason, cancellationToken);

    public static Task<DiscordChannel> ReorderAsync(this DiscordChannel channel, int position,
        string? reason = null, CancellationToken cancellationToken = default) =>
        channel.Services().Channels.ReorderAsync(channel.Id, position, reason, cancellationToken);

    public static Task<DiscordChannel> SetTopicAsync(this DiscordChannel channel, string? topic,
        string? reason = null, CancellationToken cancellationToken = default) =>
        channel.Services().Channels.SetTopicAsync(channel.Id, topic, reason, cancellationToken);

    public static Task<DiscordChannel> SetSlowmodeAsync(this DiscordChannel channel, TimeSpan slowmode,
        string? reason = null, CancellationToken cancellationToken = default) =>
        channel.IsThread
            ? channel.Services().Threads.SetSlowmodeAsync(channel.Id, slowmode, reason, cancellationToken)
            : channel.Services().Channels.SetSlowmodeAsync(channel.Id, slowmode, reason, cancellationToken);

    public static Task<DiscordChannel> SetNsfwAsync(this DiscordChannel channel, bool nsfw = true,
        string? reason = null, CancellationToken cancellationToken = default) =>
        channel.Services().Channels.SetNsfwAsync(channel.Id, nsfw, reason, cancellationToken);

    public static Task<DiscordChannel> GrantAsync(this DiscordChannel channel, Snowflake roleId,
        DiscordPermissions permissions, string? reason = null, CancellationToken cancellationToken = default) =>
        channel.Services().Channels.GrantAsync(channel.Id, roleId, permissions, reason, cancellationToken);

    public static Task<DiscordChannel> RevokeAsync(this DiscordChannel channel, Snowflake roleId,
        DiscordPermissions permissions, string? reason = null, CancellationToken cancellationToken = default) =>
        channel.Services().Channels.RevokeAsync(channel.Id, roleId, permissions, reason, cancellationToken);

    public static Task DeleteAsync(this DiscordChannel channel, string? reason = null,
        CancellationToken cancellationToken = default) =>
        channel.Services().Channels.DeleteAsync(channel.Id, reason, cancellationToken);

    public static Task<IReadOnlyList<DiscordWebhook>> GetWebhooksAsync(this DiscordChannel channel,
        CancellationToken cancellationToken = default) =>
        channel.Services().Webhooks.GetForChannelAsync(channel.WebhookChannelId, cancellationToken);

    public static Task<DiscordWebhook> CreateWebhookAsync(this DiscordChannel channel, string name,
        Action<WebhookFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        channel.Services().Webhooks.CreateAsync(channel.WebhookChannelId, name, configure, reason,
            cancellationToken);

    public static Task<DiscordWebhook> GetOrCreateWebhookAsync(this DiscordChannel channel, string name,
        Action<WebhookFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        channel.Services().Webhooks.GetOrCreateAsync(channel.WebhookChannelId, name, configure, reason,
            cancellationToken);

    public static Task<DiscordMessage?> SendAsWebhookAsync(this DiscordChannel channel, string webhookName,
        Action<WebhookMessageFactory> configure, bool wait = false,
        CancellationToken cancellationToken = default) =>
        channel.Services().Webhooks.SendAsAsync(channel.WebhookChannelId, webhookName, configure,
            channel.ThreadId, wait, cancellationToken);

    public static Task<DiscordMessage?> SendAsWebhookAsync(this DiscordChannel channel, string webhookName,
        string content, bool wait = false, CancellationToken cancellationToken = default) =>
        channel.SendAsWebhookAsync(webhookName, message => message.WithContent(content), wait,
            cancellationToken);

    public static Task<DiscordMessage?> ImpersonateAsync(this DiscordChannel channel, DiscordUser user,
        Action<WebhookMessageFactory> configure, string webhookName = "Crovus", bool wait = false,
        CancellationToken cancellationToken = default) =>
        channel.Services().Webhooks.ImpersonateAsync(channel.WebhookChannelId, user, configure, webhookName,
            channel.ThreadId, wait, cancellationToken);

    public static Task<DiscordMessage?> ImpersonateAsync(this DiscordChannel channel, DiscordUser user,
        string content, string webhookName = "Crovus", bool wait = false,
        CancellationToken cancellationToken = default) =>
        channel.ImpersonateAsync(user, message => message.WithContent(content), webhookName, wait,
            cancellationToken);

    public static Task<DiscordChannel> StartThreadAsync(this DiscordChannel channel, string name,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        channel.Services().Threads.StartPublicAsync(channel.Id, name, configure, reason, cancellationToken);

    public static Task<DiscordChannel> StartPrivateThreadAsync(this DiscordChannel channel, string name,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        channel.Services().Threads.StartPrivateAsync(channel.Id, name, configure, reason, cancellationToken);

    public static Task<DiscordChannel> StartAnnouncementThreadAsync(this DiscordChannel channel, string name,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        channel.Services().Threads.StartAnnouncementAsync(channel.Id, name, configure, reason,
            cancellationToken);

    public static Task<DiscordChannel> CreatePostAsync(this DiscordChannel forum, string name, string content,
        Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        forum.Services().Threads.CreatePostAsync(forum.Id, name, content, configure, reason, cancellationToken);

    public static Task<DiscordChannel> CreatePostAsync(this DiscordChannel forum, string name,
        Action<MessageFactory> content, Action<ThreadFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        forum.Services().Threads.CreatePostAsync(forum.Id, name, content, configure, reason, cancellationToken);

    public static Task<IReadOnlyList<DiscordChannel>> GetPostsAsync(this DiscordChannel forum,
        bool includeArchived = true, int? archivedLimit = null,
        CancellationToken cancellationToken = default) =>
        forum.Services().Threads.GetPostsAsync(forum.RequireGuildId(), forum.Id, includeArchived, archivedLimit,
            cancellationToken);

    public static Task<ThreadListing> GetActiveThreadsAsync(this DiscordChannel channel,
        CancellationToken cancellationToken = default) =>
        channel.Services().Threads.GetActiveAsync(channel.RequireGuildId(), channel.Id, cancellationToken);

    public static Task<ThreadListing> GetArchivedThreadsAsync(this DiscordChannel channel,
        ArchivedThreadQuery? query = null, bool includePrivate = false,
        CancellationToken cancellationToken = default) =>
        includePrivate
            ? channel.Services().Threads.GetPrivateArchivedAsync(channel.Id, query, cancellationToken)
            : channel.Services().Threads.GetPublicArchivedAsync(channel.Id, query, cancellationToken);

    public static Task<IReadOnlyList<DiscordInvite>> GetInvitesAsync(this DiscordChannel channel,
        CancellationToken cancellationToken = default) =>
        channel.Rest().GetChannelInvitesAsync(channel.Id, cancellationToken);

    public static Task<DiscordInvite> CreateInviteAsync(this DiscordChannel channel,
        InviteCreateRequest? request = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        channel.Rest().CreateChannelInviteAsync(channel.Id, request, reason, cancellationToken);

    public static Task<DiscordGuild> GetGuildAsync(this DiscordChannel channel,
        CancellationToken cancellationToken = default) =>
        channel.Services().Guilds.GetAsync(channel.RequireGuildId(), cancellationToken: cancellationToken);

    public static async Task<DiscordChannel?> GetParentAsync(this DiscordChannel channel,
        CancellationToken cancellationToken = default) =>
        channel.ParentId is { } parentId
            ? await channel.Rest().GetChannelAsync(parentId, cancellationToken)
            : null;

    internal static Snowflake RequireGuildId(this DiscordChannel channel) =>
        channel.GuildId ?? throw new InvalidOperationException(
            $"Channel {channel.Id} has no guild id, so this call cannot be routed. " +
            "Load the channel through the client first, or use the guild-scoped service method.");
}

using System.Runtime.CompilerServices;
using Crovus.Client;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Cache;

public sealed class CachedDiscordRest : IDiscordRest, IContextAware
{
    private const string LogCategory = "Cache.Rest";

    private readonly IDiscordRest _inner;
    private readonly IDiscordCache _cache;
    private readonly ILogger _logger;

    public CachedDiscordRest(IDiscordRest inner, IDiscordCache cache, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);

        _inner = inner;
        _cache = cache;
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
    }

    public CachedDiscordRest(IDiscordRest inner, IDiscordCache cache, DiagnosticsHub diagnostics)
        : this(inner, cache, (ILogger)diagnostics)
    {
    }

    public ICrovusContext? Context
    {
        get => (_inner as IContextAware)?.Context;
        set
        {
            if (_inner is IContextAware aware)
                aware.Context = value;

            if (_cache is IContextAware cache)
                cache.Context = value;
        }
    }

    public async Task<DiscordChannel> GetChannelAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        if (await _cache.GetChannelAsync(channelId, cancellationToken) is { } cached)
            return cached;

        var channel = await _inner.GetChannelAsync(channelId, cancellationToken);
        await _cache.SetChannelAsync(channel, cancellationToken);

        return channel;
    }

    public async Task<DiscordMessage> GetMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        if (await _cache.GetMessageAsync(messageId, cancellationToken) is { } cached)
            return cached;

        var message = await _inner.GetMessageAsync(channelId, messageId, cancellationToken);
        await _cache.SetMessageAsync(message, cancellationToken);

        return message;
    }

    public async IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, Snowflake? before = null,
        int? limit = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _inner.GetMessagesAsync(channelId, before, limit, cancellationToken))
        {
            await _cache.SetMessageAsync(message, cancellationToken);
            yield return message;
        }
    }

    public async Task<DiscordMessage> CreateMessageAsync(Snowflake channelId, MessageCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var message = await _inner.CreateMessageAsync(channelId, request, cancellationToken);
        await _cache.SetMessageAsync(message, cancellationToken);

        return message;
    }

    public async Task<DiscordMessage> EditMessageAsync(Snowflake channelId, Snowflake messageId,
        MessageEditRequest request, CancellationToken cancellationToken = default)
    {
        var message = await _inner.EditMessageAsync(channelId, messageId, request, cancellationToken);
        await _cache.SetMessageAsync(message, cancellationToken);

        return message;
    }

    public async Task DeleteMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await _inner.DeleteMessageAsync(channelId, messageId, reason, cancellationToken);
        await _cache.RemoveMessageAsync(messageId, cancellationToken);
    }

    public async Task CreateReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        await _inner.CreateReactionAsync(channelId, messageId, emoji, cancellationToken);
        await _cache.RemoveReactionsAsync(messageId, emoji, cancellationToken);
    }

    public async Task DeleteOwnReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        await _inner.DeleteOwnReactionAsync(channelId, messageId, emoji, cancellationToken);
        await _cache.RemoveReactionsAsync(messageId, emoji, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordWebhook>> GetChannelWebhooksAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        if (await _cache.GetChannelWebhooksAsync(channelId, cancellationToken) is { } cached)
            return cached;

        var webhooks = await _inner.GetChannelWebhooksAsync(channelId, cancellationToken);
        await _cache.SetChannelWebhooksAsync(channelId, webhooks, cancellationToken);

        return webhooks;
    }

    public async Task<DiscordWebhook> GetWebhookAsync(Snowflake webhookId, string? token = null,
        CancellationToken cancellationToken = default)
    {
        if (token is null && await _cache.GetWebhookAsync(webhookId, cancellationToken) is { } cached)
            return cached;

        var webhook = await _inner.GetWebhookAsync(webhookId, token, cancellationToken);
        await _cache.SetWebhookAsync(webhook, cancellationToken);

        return webhook;
    }

    public async Task<DiscordWebhook> CreateWebhookAsync(Snowflake channelId, WebhookCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var webhook = await _inner.CreateWebhookAsync(channelId, request, reason, cancellationToken);

        await _cache.SetWebhookAsync(webhook, cancellationToken);
        await _cache.RemoveChannelWebhooksAsync(channelId, cancellationToken);

        return webhook;
    }

    public async Task<DiscordWebhook> ModifyWebhookAsync(Snowflake webhookId, WebhookModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var previous = await _cache.GetWebhookAsync(webhookId, cancellationToken);
        var webhook = await _inner.ModifyWebhookAsync(webhookId, request, reason, cancellationToken);

        await _cache.SetWebhookAsync(webhook, cancellationToken);
        await _cache.RemoveChannelWebhooksAsync(webhook.ChannelId, cancellationToken);

        if (previous is not null && previous.ChannelId != webhook.ChannelId)
            await _cache.RemoveChannelWebhooksAsync(previous.ChannelId, cancellationToken);

        return webhook;
    }

    public async Task DeleteWebhookAsync(Snowflake webhookId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await _inner.DeleteWebhookAsync(webhookId, reason, cancellationToken);
        await _cache.RemoveWebhookAsync(webhookId, cancellationToken);
    }

    public async Task<DiscordMessage?> ExecuteWebhookAsync(DiscordWebhook webhook, WebhookExecuteRequest request,
        Snowflake? threadId = null, bool wait = false, CancellationToken cancellationToken = default)
    {
        var message = await _inner.ExecuteWebhookAsync(webhook, request, threadId, wait, cancellationToken);

        if (message is not null)
            await _cache.SetMessageAsync(message, cancellationToken);

        return message;
    }

    public async Task<DiscordChannel> CreateChannelAsync(Snowflake guildId, ChannelCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var channel = await _inner.CreateChannelAsync(guildId, request, reason, cancellationToken);
        await _cache.SetChannelAsync(channel, cancellationToken);

        return channel;
    }

    public async Task<DiscordChannel> ModifyChannelAsync(Snowflake channelId, ChannelModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var channel = await _inner.ModifyChannelAsync(channelId, request, reason, cancellationToken);
        await _cache.SetChannelAsync(channel, cancellationToken);

        if (request.ParentId is not null)
            await _cache.RemoveChannelWebhooksAsync(channel.WebhookChannelId, cancellationToken);

        return channel;
    }

    public async Task DeleteChannelAsync(Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await _inner.DeleteChannelAsync(channelId, reason, cancellationToken);

        await _cache.RemoveChannelAsync(channelId, cancellationToken);
        await _cache.RemoveChannelWebhooksAsync(channelId, cancellationToken);
    }

    public async Task<DiscordChannel> StartThreadAsync(Snowflake channelId, ThreadCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var thread = await _inner.StartThreadAsync(channelId, request, reason, cancellationToken);
        await _cache.SetChannelAsync(thread, cancellationToken);

        return thread;
    }

    public async Task<DiscordChannel> StartThreadFromMessageAsync(Snowflake channelId, Snowflake messageId,
        ThreadFromMessageRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        var thread = await _inner.StartThreadFromMessageAsync(channelId, messageId, request, reason,
            cancellationToken);
        await _cache.SetChannelAsync(thread, cancellationToken);

        return thread;
    }

    public Task<IReadOnlyList<DiscordGuildEmoji>> GetGuildEmojisAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        _inner.GetGuildEmojisAsync(guildId, cancellationToken);

    public Task<DiscordGuildEmoji> GetGuildEmojiAsync(Snowflake guildId, Snowflake emojiId,
        CancellationToken cancellationToken = default) =>
        _inner.GetGuildEmojiAsync(guildId, emojiId, cancellationToken);

    public Task<DiscordGuildEmoji> CreateGuildEmojiAsync(Snowflake guildId, EmojiCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        _inner.CreateGuildEmojiAsync(guildId, request, reason, cancellationToken);

    public Task<DiscordGuildEmoji> ModifyGuildEmojiAsync(Snowflake guildId, Snowflake emojiId,
        EmojiModifyRequest request, string? reason = null, CancellationToken cancellationToken = default) =>
        _inner.ModifyGuildEmojiAsync(guildId, emojiId, request, reason, cancellationToken);

    public Task DeleteGuildEmojiAsync(Snowflake guildId, Snowflake emojiId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        _inner.DeleteGuildEmojiAsync(guildId, emojiId, reason, cancellationToken);

    public Task<IReadOnlyList<DiscordApplicationCommand>> GetApplicationCommandsAsync(Snowflake applicationId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default) =>
        _inner.GetApplicationCommandsAsync(applicationId, guildId, cancellationToken);

    public Task<DiscordApplicationCommand> CreateApplicationCommandAsync(Snowflake applicationId,
        ApplicationCommandRequest request, Snowflake? guildId = null,
        CancellationToken cancellationToken = default) =>
        _inner.CreateApplicationCommandAsync(applicationId, request, guildId, cancellationToken);

    public Task<DiscordApplicationCommand> EditApplicationCommandAsync(Snowflake applicationId, Snowflake commandId,
        ApplicationCommandRequest request, Snowflake? guildId = null,
        CancellationToken cancellationToken = default) =>
        _inner.EditApplicationCommandAsync(applicationId, commandId, request, guildId, cancellationToken);

    public Task DeleteApplicationCommandAsync(Snowflake applicationId, Snowflake commandId, Snowflake? guildId = null,
        CancellationToken cancellationToken = default) =>
        _inner.DeleteApplicationCommandAsync(applicationId, commandId, guildId, cancellationToken);

    public Task<IReadOnlyList<DiscordApplicationCommand>> SetApplicationCommandsAsync(Snowflake applicationId,
        IReadOnlyList<ApplicationCommandRequest> requests, Snowflake? guildId = null,
        CancellationToken cancellationToken = default) =>
        _inner.SetApplicationCommandsAsync(applicationId, requests, guildId, cancellationToken);

    public Task CreateInteractionResponseAsync(Snowflake interactionId, string interactionToken,
        InteractionResponseRequest request, CancellationToken cancellationToken = default) =>
        _inner.CreateInteractionResponseAsync(interactionId, interactionToken, request, cancellationToken);

    public Task<DiscordMessage> GetOriginalInteractionResponseAsync(Snowflake applicationId, string interactionToken,
        CancellationToken cancellationToken = default) =>
        _inner.GetOriginalInteractionResponseAsync(applicationId, interactionToken, cancellationToken);

    public async Task<DiscordMessage> EditOriginalInteractionResponseAsync(Snowflake applicationId,
        string interactionToken, InteractionMessageRequest request, CancellationToken cancellationToken = default)
    {
        var message = await _inner.EditOriginalInteractionResponseAsync(applicationId, interactionToken, request,
            cancellationToken);

        await _cache.SetMessageAsync(message, cancellationToken);

        return message;
    }

    public Task DeleteOriginalInteractionResponseAsync(Snowflake applicationId, string interactionToken,
        CancellationToken cancellationToken = default) =>
        _inner.DeleteOriginalInteractionResponseAsync(applicationId, interactionToken, cancellationToken);

    public async Task<DiscordMessage> CreateFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        InteractionMessageRequest request, CancellationToken cancellationToken = default)
    {
        var message = await _inner.CreateFollowupMessageAsync(applicationId, interactionToken, request,
            cancellationToken);

        await _cache.SetMessageAsync(message, cancellationToken);

        return message;
    }

    public async Task<DiscordMessage> EditFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        Snowflake messageId, InteractionMessageRequest request, CancellationToken cancellationToken = default)
    {
        var message = await _inner.EditFollowupMessageAsync(applicationId, interactionToken, messageId, request,
            cancellationToken);

        await _cache.SetMessageAsync(message, cancellationToken);

        return message;
    }

    public async Task DeleteFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        Snowflake messageId, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteFollowupMessageAsync(applicationId, interactionToken, messageId, cancellationToken);
        await _cache.RemoveMessageAsync(messageId, cancellationToken);
    }

    public async Task<DiscordGuild> GetGuildAsync(Snowflake guildId, bool withCounts = false,
        CancellationToken cancellationToken = default)
    {
        if (!withCounts && await _cache.GetGuildAsync(guildId, cancellationToken) is { } cached)
            return cached;

        var guild = await _inner.GetGuildAsync(guildId, withCounts, cancellationToken);

        await _cache.SetGuildAsync(guild, cancellationToken);

        return guild;
    }

    public async Task<IReadOnlyList<DiscordChannel>> GetGuildChannelsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var channels = await _inner.GetGuildChannelsAsync(guildId, cancellationToken);

        foreach (var channel in channels)
            await _cache.SetChannelAsync(channel, cancellationToken);

        return channels;
    }

    public async Task<DiscordMember> GetGuildMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        if (await _cache.GetMemberAsync(guildId, userId, cancellationToken) is { } cached)
            return cached;

        var member = await _inner.GetGuildMemberAsync(guildId, userId, cancellationToken);

        await _cache.SetMemberAsync(guildId, member, cancellationToken);

        return member;
    }

    public async Task<IReadOnlyList<DiscordMember>> GetGuildMembersAsync(Snowflake guildId, MemberQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var members = await _inner.GetGuildMembersAsync(guildId, query, cancellationToken);

        foreach (var member in members)
            await _cache.SetMemberAsync(guildId, member, cancellationToken);

        return members;
    }

    public async Task<IReadOnlyList<DiscordMember>> SearchGuildMembersAsync(Snowflake guildId, string search,
        int limit = 1, CancellationToken cancellationToken = default)
    {
        var members = await _inner.SearchGuildMembersAsync(guildId, search, limit, cancellationToken);

        foreach (var member in members)
            await _cache.SetMemberAsync(guildId, member, cancellationToken);

        return members;
    }

    public async Task<DiscordMember> ModifyGuildMemberAsync(Snowflake guildId, Snowflake userId,
        MemberModifyRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        var member = await _inner.ModifyGuildMemberAsync(guildId, userId, request, reason, cancellationToken);

        await _cache.SetMemberAsync(guildId, member, cancellationToken);

        return member;
    }

    public async Task AddGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        await _inner.AddGuildMemberRoleAsync(guildId, userId, roleId, reason, cancellationToken);
        await _cache.RemoveMemberAsync(guildId, userId, cancellationToken);
    }

    public async Task RemoveGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        await _inner.RemoveGuildMemberRoleAsync(guildId, userId, roleId, reason, cancellationToken);
        await _cache.RemoveMemberAsync(guildId, userId, cancellationToken);
    }

    public async Task RemoveGuildMemberAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await _inner.RemoveGuildMemberAsync(guildId, userId, reason, cancellationToken);
        await _cache.RemoveMemberAsync(guildId, userId, cancellationToken);
    }

    public Task<IReadOnlyList<DiscordBan>> GetGuildBansAsync(Snowflake guildId, BanQuery? query = null,
        CancellationToken cancellationToken = default) =>
        _inner.GetGuildBansAsync(guildId, query, cancellationToken);

    public Task<DiscordBan?> GetGuildBanAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        _inner.GetGuildBanAsync(guildId, userId, cancellationToken);

    public async Task CreateGuildBanAsync(Snowflake guildId, Snowflake userId, BanCreateRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        await _inner.CreateGuildBanAsync(guildId, userId, request, reason, cancellationToken);
        await _cache.RemoveMemberAsync(guildId, userId, cancellationToken);
    }

    public Task RemoveGuildBanAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        _inner.RemoveGuildBanAsync(guildId, userId, reason, cancellationToken);

    public async Task<IReadOnlyList<DiscordRole>> GetGuildRolesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        if (await _cache.GetGuildRolesAsync(guildId, cancellationToken) is { } cached)
            return cached;

        var roles = await _inner.GetGuildRolesAsync(guildId, cancellationToken);

        await _cache.SetGuildRolesAsync(guildId, roles, cancellationToken);

        return roles;
    }

    public async Task<DiscordRole> CreateGuildRoleAsync(Snowflake guildId, RoleCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var role = await _inner.CreateGuildRoleAsync(guildId, request, reason, cancellationToken);

        await _cache.RemoveGuildRolesAsync(guildId, cancellationToken);

        return role;
    }

    public async Task<DiscordRole> ModifyGuildRoleAsync(Snowflake guildId, Snowflake roleId,
        RoleModifyRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        var role = await _inner.ModifyGuildRoleAsync(guildId, roleId, request, reason, cancellationToken);

        await _cache.RemoveGuildRolesAsync(guildId, cancellationToken);

        return role;
    }

    public async Task DeleteGuildRoleAsync(Snowflake guildId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await _inner.DeleteGuildRoleAsync(guildId, roleId, reason, cancellationToken);
        await _cache.RemoveGuildRolesAsync(guildId, cancellationToken);
    }

    public async IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, MessageQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _inner.GetMessagesAsync(channelId, query, cancellationToken))
        {
            await _cache.SetMessageAsync(message, cancellationToken);
            yield return message;
        }
    }

    public async Task BulkDeleteMessagesAsync(Snowflake channelId, IReadOnlyList<Snowflake> messageIds,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        await _inner.BulkDeleteMessagesAsync(channelId, messageIds, reason, cancellationToken);

        foreach (var messageId in messageIds)
        {
            await _cache.RemoveMessageAsync(messageId, cancellationToken);
            await _cache.ClearReactionsAsync(messageId, cancellationToken);
        }
    }

    public async Task<DiscordMessage> CrosspostMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await _inner.CrosspostMessageAsync(channelId, messageId, cancellationToken);
        await _cache.SetMessageAsync(message, cancellationToken);

        return message;
    }

    public async Task<IReadOnlyList<DiscordMessage>> GetPinnedMessagesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        var messages = await _inner.GetPinnedMessagesAsync(channelId, cancellationToken);

        foreach (var message in messages)
            await _cache.SetMessageAsync(message, cancellationToken);

        return messages;
    }

    public Task PinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        _inner.PinMessageAsync(channelId, messageId, reason, cancellationToken);

    public Task UnpinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        _inner.UnpinMessageAsync(channelId, messageId, reason, cancellationToken);

    public Task TriggerTypingAsync(Snowflake channelId, CancellationToken cancellationToken = default) =>
        _inner.TriggerTypingAsync(channelId, cancellationToken);

    public async Task<IReadOnlyList<DiscordUser>> GetReactionsAsync(Snowflake channelId, Snowflake messageId,
        DiscordEmoji emoji, ReactionQuery? query = null, CancellationToken cancellationToken = default)
    {
        var users = await _inner.GetReactionsAsync(channelId, messageId, emoji, query, cancellationToken);

        foreach (var user in users)
        {
            await _cache.SetUserAsync(user, cancellationToken);
            await _cache.AddReactionAsync(messageId, emoji, user.Id, cancellationToken);
        }

        return users;
    }

    public async Task DeleteUserReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        Snowflake userId, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteUserReactionAsync(channelId, messageId, emoji, userId, cancellationToken);
        await _cache.RemoveReactionAsync(messageId, emoji, userId, cancellationToken);
    }

    public async Task DeleteAllReactionsAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAllReactionsAsync(channelId, messageId, cancellationToken);
        await _cache.ClearReactionsAsync(messageId, cancellationToken);
    }

    public async Task DeleteEmojiReactionsAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        await _inner.DeleteEmojiReactionsAsync(channelId, messageId, emoji, cancellationToken);
        await _cache.RemoveReactionsAsync(messageId, emoji, cancellationToken);
    }

    public async Task<DiscordUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await _inner.GetCurrentUserAsync(cancellationToken);
        await _cache.SetUserAsync(user, cancellationToken);

        return user;
    }

    public async Task<DiscordUser> GetUserAsync(Snowflake userId, CancellationToken cancellationToken = default)
    {
        if (await _cache.GetUserAsync(userId, cancellationToken) is { } cached)
            return cached;

        var user = await _inner.GetUserAsync(userId, cancellationToken);
        await _cache.SetUserAsync(user, cancellationToken);

        return user;
    }

    public async Task<DiscordChannel> CreateDirectMessageChannelAsync(Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var channel = await _inner.CreateDirectMessageChannelAsync(userId, cancellationToken);
        await _cache.SetChannelAsync(channel, cancellationToken);

        return channel;
    }

    public async Task LeaveGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default)
    {
        await _inner.LeaveGuildAsync(guildId, cancellationToken);
        await _cache.RemoveGuildAsync(guildId, cancellationToken);
        await _cache.RemoveGuildRolesAsync(guildId, cancellationToken);
    }

    public Task<GatewayBotInfo> GetGatewayBotAsync(CancellationToken cancellationToken = default) =>
        _inner.GetGatewayBotAsync(cancellationToken);

    public Task<IReadOnlyList<DiscordInvite>> GetChannelInvitesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        _inner.GetChannelInvitesAsync(channelId, cancellationToken);

    public Task<IReadOnlyList<DiscordInvite>> GetGuildInvitesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        _inner.GetGuildInvitesAsync(guildId, cancellationToken);

    public Task<DiscordInvite> CreateChannelInviteAsync(Snowflake channelId, InviteCreateRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default) =>
        _inner.CreateChannelInviteAsync(channelId, request, reason, cancellationToken);

    public Task<DiscordInvite> GetInviteAsync(string code, bool withCounts = false,
        CancellationToken cancellationToken = default) =>
        _inner.GetInviteAsync(code, withCounts, cancellationToken);

    public Task DeleteInviteAsync(string code, string? reason = null,
        CancellationToken cancellationToken = default) =>
        _inner.DeleteInviteAsync(code, reason, cancellationToken);

    public async Task<DiscordAuditLog> GetGuildAuditLogAsync(Snowflake guildId, AuditLogQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var log = await _inner.GetGuildAuditLogAsync(guildId, query, cancellationToken);

        foreach (var user in log.Users)
            await _cache.SetUserAsync(user, cancellationToken);

        return log;
    }

    public async Task<DiscordGuild> ModifyGuildAsync(Snowflake guildId, GuildModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var guild = await _inner.ModifyGuildAsync(guildId, request, reason, cancellationToken);
        await _cache.SetGuildAsync(guild, cancellationToken);

        return guild;
    }

    public Task<int> GetGuildPruneCountAsync(Snowflake guildId, PruneRequest? request = null,
        CancellationToken cancellationToken = default) =>
        _inner.GetGuildPruneCountAsync(guildId, request, cancellationToken);

    public Task<int?> BeginGuildPruneAsync(Snowflake guildId, PruneRequest? request = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        _inner.BeginGuildPruneAsync(guildId, request, reason, cancellationToken);

    public async Task<ThreadListing> GetActiveThreadsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        await Remember(await _inner.GetActiveThreadsAsync(guildId, cancellationToken), cancellationToken);

    public async Task<ThreadListing> GetPublicArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default) =>
        await Remember(await _inner.GetPublicArchivedThreadsAsync(channelId, query, cancellationToken),
            cancellationToken);

    public async Task<ThreadListing> GetPrivateArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default) =>
        await Remember(await _inner.GetPrivateArchivedThreadsAsync(channelId, query, cancellationToken),
            cancellationToken);

    public async Task<ThreadListing> GetJoinedPrivateArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default) =>
        await Remember(await _inner.GetJoinedPrivateArchivedThreadsAsync(channelId, query, cancellationToken),
            cancellationToken);

    public Task JoinThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default) =>
        _inner.JoinThreadAsync(threadId, cancellationToken);

    public Task LeaveThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default) =>
        _inner.LeaveThreadAsync(threadId, cancellationToken);

    public Task AddThreadMemberAsync(Snowflake threadId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        _inner.AddThreadMemberAsync(threadId, userId, cancellationToken);

    public Task RemoveThreadMemberAsync(Snowflake threadId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        _inner.RemoveThreadMemberAsync(threadId, userId, cancellationToken);

    public Task<DiscordThreadMember> GetThreadMemberAsync(Snowflake threadId, Snowflake userId,
        bool withMember = false, CancellationToken cancellationToken = default) =>
        _inner.GetThreadMemberAsync(threadId, userId, withMember, cancellationToken);

    public Task<IReadOnlyList<DiscordThreadMember>> GetThreadMembersAsync(Snowflake threadId,
        bool withMember = false, CancellationToken cancellationToken = default) =>
        _inner.GetThreadMembersAsync(threadId, withMember, cancellationToken);

    public Task<IReadOnlyList<DiscordCommandPermissions>> GetGuildCommandPermissionsAsync(Snowflake applicationId,
        Snowflake guildId, CancellationToken cancellationToken = default) =>
        _inner.GetGuildCommandPermissionsAsync(applicationId, guildId, cancellationToken);

    public Task<DiscordCommandPermissions> GetCommandPermissionsAsync(Snowflake applicationId, Snowflake guildId,
        Snowflake commandId, CancellationToken cancellationToken = default) =>
        _inner.GetCommandPermissionsAsync(applicationId, guildId, commandId, cancellationToken);

    private async Task<ThreadListing> Remember(ThreadListing listing, CancellationToken cancellationToken)
    {
        foreach (var thread in listing.Threads)
            await _cache.SetChannelAsync(thread, cancellationToken);

        return listing;
    }

    public ValueTask DisposeAsync() => _inner.DisposeAsync();
}

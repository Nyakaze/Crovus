using Crovus.Models;

namespace Crovus.Rest;

public interface IDiscordRest : IAsyncDisposable
{
    Task<DiscordChannel> GetChannelAsync(Snowflake channelId, CancellationToken cancellationToken = default);

    Task<DiscordMessage> GetMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, Snowflake? before = null,
        int? limit = null, CancellationToken cancellationToken = default);

    Task<DiscordMessage> CreateMessageAsync(Snowflake channelId, MessageCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<DiscordMessage> EditMessageAsync(Snowflake channelId, Snowflake messageId, MessageEditRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task CreateReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default);

    Task DeleteOwnReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordWebhook>> GetChannelWebhooksAsync(Snowflake channelId,
        CancellationToken cancellationToken = default);

    Task<DiscordWebhook> GetWebhookAsync(Snowflake webhookId, string? token = null,
        CancellationToken cancellationToken = default);

    Task<DiscordWebhook> CreateWebhookAsync(Snowflake channelId, WebhookCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<DiscordWebhook> ModifyWebhookAsync(Snowflake webhookId, WebhookModifyRequest request, string? reason = null,
        CancellationToken cancellationToken = default);

    Task DeleteWebhookAsync(Snowflake webhookId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<DiscordMessage?> ExecuteWebhookAsync(DiscordWebhook webhook, WebhookExecuteRequest request,
        Snowflake? threadId = null, bool wait = false, CancellationToken cancellationToken = default);

    Task<DiscordMessage> EditWebhookMessageAsync(DiscordWebhook webhook, Snowflake messageId,
        MessageEditRequest request, Snowflake? threadId = null, CancellationToken cancellationToken = default);

    Task DeleteWebhookMessageAsync(DiscordWebhook webhook, Snowflake messageId, Snowflake? threadId = null,
        CancellationToken cancellationToken = default);

    Task<DiscordChannel> CreateChannelAsync(Snowflake guildId, ChannelCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<DiscordChannel> ModifyChannelAsync(Snowflake channelId, ChannelModifyRequest request, string? reason = null,
        CancellationToken cancellationToken = default);

    Task DeleteChannelAsync(Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<DiscordChannel> StartThreadAsync(Snowflake channelId, ThreadCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<DiscordChannel> StartThreadFromMessageAsync(Snowflake channelId, Snowflake messageId,
        ThreadFromMessageRequest request, string? reason = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordGuildEmoji>> GetGuildEmojisAsync(Snowflake guildId,
        CancellationToken cancellationToken = default);

    Task<DiscordGuildEmoji> GetGuildEmojiAsync(Snowflake guildId, Snowflake emojiId,
        CancellationToken cancellationToken = default);

    Task<DiscordGuildEmoji> CreateGuildEmojiAsync(Snowflake guildId, EmojiCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<DiscordGuildEmoji> ModifyGuildEmojiAsync(Snowflake guildId, Snowflake emojiId, EmojiModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default);

    Task DeleteGuildEmojiAsync(Snowflake guildId, Snowflake emojiId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordApplicationCommand>> GetApplicationCommandsAsync(Snowflake applicationId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default);

    Task<DiscordApplicationCommand> CreateApplicationCommandAsync(Snowflake applicationId,
        ApplicationCommandRequest request, Snowflake? guildId = null, CancellationToken cancellationToken = default);

    Task<DiscordApplicationCommand> EditApplicationCommandAsync(Snowflake applicationId, Snowflake commandId,
        ApplicationCommandRequest request, Snowflake? guildId = null, CancellationToken cancellationToken = default);

    Task DeleteApplicationCommandAsync(Snowflake applicationId, Snowflake commandId, Snowflake? guildId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordApplicationCommand>> SetApplicationCommandsAsync(Snowflake applicationId,
        IReadOnlyList<ApplicationCommandRequest> requests, Snowflake? guildId = null,
        CancellationToken cancellationToken = default);

    Task CreateInteractionResponseAsync(Snowflake interactionId, string interactionToken,
        InteractionResponseRequest request, CancellationToken cancellationToken = default);

    Task<DiscordMessage> GetOriginalInteractionResponseAsync(Snowflake applicationId, string interactionToken,
        CancellationToken cancellationToken = default);

    Task<DiscordMessage> EditOriginalInteractionResponseAsync(Snowflake applicationId, string interactionToken,
        InteractionMessageRequest request, CancellationToken cancellationToken = default);

    Task DeleteOriginalInteractionResponseAsync(Snowflake applicationId, string interactionToken,
        CancellationToken cancellationToken = default);

    Task<DiscordMessage> CreateFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        InteractionMessageRequest request, CancellationToken cancellationToken = default);

    Task<DiscordMessage> EditFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        Snowflake messageId, InteractionMessageRequest request, CancellationToken cancellationToken = default);

    Task DeleteFollowupMessageAsync(Snowflake applicationId, string interactionToken, Snowflake messageId,
        CancellationToken cancellationToken = default);

    Task<DiscordGuild> GetGuildAsync(Snowflake guildId, bool withCounts = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordChannel>> GetGuildChannelsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default);

    Task<DiscordMember> GetGuildMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordMember>> GetGuildMembersAsync(Snowflake guildId, MemberQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordMember>> SearchGuildMembersAsync(Snowflake guildId, string search, int limit = 1,
        CancellationToken cancellationToken = default);

    Task<DiscordMember> ModifyGuildMemberAsync(Snowflake guildId, Snowflake userId, MemberModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default);

    Task AddGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task RemoveGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task RemoveGuildMemberAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordBan>> GetGuildBansAsync(Snowflake guildId, BanQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<DiscordBan?> GetGuildBanAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default);

    Task CreateGuildBanAsync(Snowflake guildId, Snowflake userId, BanCreateRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default);

    Task RemoveGuildBanAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordRole>> GetGuildRolesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default);

    Task<DiscordRole> CreateGuildRoleAsync(Snowflake guildId, RoleCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<DiscordRole> ModifyGuildRoleAsync(Snowflake guildId, Snowflake roleId, RoleModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default);

    Task DeleteGuildRoleAsync(Snowflake guildId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, MessageQuery query,
        CancellationToken cancellationToken = default);

    Task BulkDeleteMessagesAsync(Snowflake channelId, IReadOnlyList<Snowflake> messageIds, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<DiscordMessage> CrosspostMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordMessage>> GetPinnedMessagesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default);

    Task PinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task UnpinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default);

    Task TriggerTypingAsync(Snowflake channelId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordUser>> GetReactionsAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        ReactionQuery? query = null, CancellationToken cancellationToken = default);

    Task DeleteUserReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji, Snowflake userId,
        CancellationToken cancellationToken = default);

    Task DeleteAllReactionsAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default);

    Task DeleteEmojiReactionsAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default);

    Task<DiscordUser> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<DiscordUser> GetUserAsync(Snowflake userId, CancellationToken cancellationToken = default);

    Task<DiscordChannel> CreateDirectMessageChannelAsync(Snowflake userId,
        CancellationToken cancellationToken = default);

    Task LeaveGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default);

    Task<GatewayBotInfo> GetGatewayBotAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordInvite>> GetChannelInvitesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordInvite>> GetGuildInvitesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default);

    Task<DiscordInvite> CreateChannelInviteAsync(Snowflake channelId, InviteCreateRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default);

    Task<DiscordInvite> GetInviteAsync(string code, bool withCounts = false,
        CancellationToken cancellationToken = default);

    Task DeleteInviteAsync(string code, string? reason = null, CancellationToken cancellationToken = default);

    Task<DiscordAuditLog> GetGuildAuditLogAsync(Snowflake guildId, AuditLogQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<DiscordGuild> ModifyGuildAsync(Snowflake guildId, GuildModifyRequest request, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<int> GetGuildPruneCountAsync(Snowflake guildId, PruneRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<int?> BeginGuildPruneAsync(Snowflake guildId, PruneRequest? request = null, string? reason = null,
        CancellationToken cancellationToken = default);

    Task<ThreadListing> GetActiveThreadsAsync(Snowflake guildId, CancellationToken cancellationToken = default);

    Task<ThreadListing> GetPublicArchivedThreadsAsync(Snowflake channelId, ArchivedThreadQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<ThreadListing> GetPrivateArchivedThreadsAsync(Snowflake channelId, ArchivedThreadQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<ThreadListing> GetJoinedPrivateArchivedThreadsAsync(Snowflake channelId, ArchivedThreadQuery? query = null,
        CancellationToken cancellationToken = default);

    Task JoinThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default);

    Task LeaveThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default);

    Task AddThreadMemberAsync(Snowflake threadId, Snowflake userId, CancellationToken cancellationToken = default);

    Task RemoveThreadMemberAsync(Snowflake threadId, Snowflake userId,
        CancellationToken cancellationToken = default);

    Task<DiscordThreadMember> GetThreadMemberAsync(Snowflake threadId, Snowflake userId, bool withMember = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordThreadMember>> GetThreadMembersAsync(Snowflake threadId, bool withMember = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordCommandPermissions>> GetGuildCommandPermissionsAsync(Snowflake applicationId,
        Snowflake guildId, CancellationToken cancellationToken = default);

    Task<DiscordCommandPermissions> GetCommandPermissionsAsync(Snowflake applicationId, Snowflake guildId,
        Snowflake commandId, CancellationToken cancellationToken = default);
}

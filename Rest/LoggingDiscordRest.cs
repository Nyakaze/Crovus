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

    public Task<DiscordChannel> GetChannelAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetChannelAsync), LogLevel.Debug,
            () => _inner.GetChannelAsync(channelId, cancellationToken),
            _ => $"Fetched channel {channelId}",
            () => $"channel {channelId}");

    public Task<IReadOnlyList<DiscordAttachmentRefresh>> RefreshAttachmentUrlsAsync(
        IReadOnlyList<string> attachmentUrls, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(RefreshAttachmentUrlsAsync), LogLevel.Debug,
            () => _inner.RefreshAttachmentUrlsAsync(attachmentUrls, cancellationToken),
            result => $"Refreshed {result.Count} attachment url(s)",
            () => $"{attachmentUrls.Count} attachment url(s)");

    public Task<DiscordMessage> GetMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetMessageAsync), LogLevel.Debug,
            () => _inner.GetMessageAsync(channelId, messageId, cancellationToken),
            _ => $"Fetched message {messageId} from channel {channelId}",
            () => $"message {messageId} in channel {channelId}");

    public IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, Snowflake? before = null,
        int? limit = null, CancellationToken cancellationToken = default) =>
        TrackMessagesAsync(_inner.GetMessagesAsync(channelId, before, limit, cancellationToken), channelId,
            count => $"Read {count} messages from channel {channelId}", cancellationToken);

    public Task<DiscordMessage> CreateMessageAsync(Snowflake channelId, MessageCreateRequest request,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateMessageAsync), LogLevel.Information,
            () => _inner.CreateMessageAsync(channelId, request, cancellationToken),
            message =>
                $"Created message {message.Id} in channel {channelId}{Uploaded(request.Files)}{Showing(request.Components)}",
            () => $"channel {channelId}",
            message => Emit(new MessageCreated(channelId.Value, message.Id.Value)));

    public Task<DiscordMessage> EditMessageAsync(Snowflake channelId, Snowflake messageId,
        MessageEditRequest request, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(EditMessageAsync), LogLevel.Information,
            () => _inner.EditMessageAsync(channelId, messageId, request, cancellationToken),
            _ =>
                $"Edited message {messageId} in channel {channelId}{Uploaded(request.Files)}{Showing(request.Components)}",
            () => $"message {messageId} in channel {channelId}",
            _ => Emit(new MessageEdited(channelId.Value, messageId.Value)));

    public Task DeleteMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteMessageAsync), LogLevel.Information,
            () => _inner.DeleteMessageAsync(channelId, messageId, reason, cancellationToken),
            () => $"Deleted message {messageId} in channel {channelId}{Because(reason)}",
            () => $"message {messageId} in channel {channelId}",
            () => Emit(new MessageDeleted(channelId.Value, messageId.Value, reason)));

    public Task CreateReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateReactionAsync), LogLevel.Debug,
            () => _inner.CreateReactionAsync(channelId, messageId, emoji, cancellationToken),
            () => $"Added reaction {Describe(emoji)} to message {messageId} in channel {channelId}",
            () => $"reaction {Describe(emoji)} on message {messageId}",
            () => Emit(new ReactionAdded(channelId.Value, messageId.Value, Describe(emoji))));

    public Task DeleteOwnReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteOwnReactionAsync), LogLevel.Debug,
            () => _inner.DeleteOwnReactionAsync(channelId, messageId, emoji, cancellationToken),
            () => $"Removed reaction {Describe(emoji)} from message {messageId} in channel {channelId}",
            () => $"reaction {Describe(emoji)} on message {messageId}",
            () => Emit(new ReactionRemoved(channelId.Value, messageId.Value, Describe(emoji))));

    public Task<IReadOnlyList<DiscordWebhook>> GetChannelWebhooksAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetChannelWebhooksAsync), LogLevel.Debug,
            () => _inner.GetChannelWebhooksAsync(channelId, cancellationToken),
            webhooks => $"Fetched {webhooks.Count} webhooks for channel {channelId}",
            () => $"channel {channelId}");

    public Task<DiscordWebhook> GetWebhookAsync(Snowflake webhookId, string? token = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetWebhookAsync), LogLevel.Debug,
            () => _inner.GetWebhookAsync(webhookId, token, cancellationToken),
            _ => $"Fetched webhook {webhookId}",
            () => $"webhook {webhookId}");

    public Task<DiscordWebhook> CreateWebhookAsync(Snowflake channelId, WebhookCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateWebhookAsync), LogLevel.Information,
            () => _inner.CreateWebhookAsync(channelId, request, reason, cancellationToken),
            webhook =>
                $"Created webhook {webhook.Id} named '{request.Name}' in channel {channelId}{Because(reason)}",
            () => $"channel {channelId}",
            webhook => Emit(new WebhookCreated(webhook.Id.Value, channelId.Value, request.Name)));

    public Task<DiscordWebhook> ModifyWebhookAsync(Snowflake webhookId, WebhookModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(ModifyWebhookAsync), LogLevel.Information,
            () => _inner.ModifyWebhookAsync(webhookId, request, reason, cancellationToken),
            _ => $"Modified webhook {webhookId}{Because(reason)}",
            () => $"webhook {webhookId}",
            _ => Emit(new WebhookModified(webhookId.Value)));

    public Task DeleteWebhookAsync(Snowflake webhookId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteWebhookAsync), LogLevel.Information,
            () => _inner.DeleteWebhookAsync(webhookId, reason, cancellationToken),
            () => $"Deleted webhook {webhookId}{Because(reason)}",
            () => $"webhook {webhookId}",
            () => Emit(new WebhookDeleted(webhookId.Value)));

    public Task<DiscordMessage?> ExecuteWebhookAsync(DiscordWebhook webhook, WebhookExecuteRequest request,
        Snowflake? threadId = null, bool wait = false, CancellationToken cancellationToken = default)
    {
        var target = threadId is { } thread ? $"thread {thread}" : $"channel {webhook.ChannelId}";

        return TrackAsync(nameof(ExecuteWebhookAsync), LogLevel.Information,
            () => _inner.ExecuteWebhookAsync(webhook, request, threadId, wait, cancellationToken),
            message =>
                $"Executed webhook {webhook.Id} into {target}{(message is null ? string.Empty : $", message {message.Id}")}{Uploaded(request.Files)}{Showing(request.Components)}",
            () => $"webhook {webhook.Id} into {target}",
            _ => Emit(new WebhookExecuted(webhook.Id.Value, webhook.ChannelId.Value, threadId?.Value, wait)));
    }

    public async Task<DiscordMessage> EditWebhookMessageAsync(DiscordWebhook webhook, Snowflake messageId,
        MessageEditRequest request, Snowflake? threadId = null, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var message = await _inner.EditWebhookMessageAsync(webhook, messageId, request, threadId,
                cancellationToken);
            Succeeded(nameof(EditWebhookMessageAsync), start, LogLevel.Information,
                $"Edited webhook message {messageId} of webhook {webhook.Id}");
            return message;
        }
        catch (Exception exception)
        {
            Failed(nameof(EditWebhookMessageAsync), start, exception,
                $"webhook message {messageId} of webhook {webhook.Id}");
            throw;
        }
    }

    public Task DeleteWebhookMessageAsync(DiscordWebhook webhook, Snowflake messageId,
        Snowflake? threadId = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteWebhookMessageAsync), LogLevel.Information,
            () => _inner.DeleteWebhookMessageAsync(webhook, messageId, threadId, cancellationToken),
            () => $"Deleted webhook message {messageId} of webhook {webhook.Id}",
            () => $"webhook message {messageId} of webhook {webhook.Id}");

    public Task<DiscordChannel> CreateChannelAsync(Snowflake guildId, ChannelCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateChannelAsync), LogLevel.Information,
            () => _inner.CreateChannelAsync(guildId, request, reason, cancellationToken),
            channel =>
                $"Created {channel.Type} channel {channel.Name} ({channel.Id}) in guild {guildId}{Because(reason)}",
            () => $"guild {guildId}",
            channel => Emit(new ChannelCreated(guildId, channel.Id, channel.Type.ToString(), channel.Name)));

    public Task<DiscordChannel> ModifyChannelAsync(Snowflake channelId, ChannelModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(ModifyChannelAsync), LogLevel.Information,
            () => _inner.ModifyChannelAsync(channelId, request, reason, cancellationToken),
            _ => $"Modified channel {channelId}{Because(reason)}",
            () => $"channel {channelId}",
            _ => Emit(new ChannelModified(channelId)));

    public Task DeleteChannelAsync(Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteChannelAsync), LogLevel.Information,
            () => _inner.DeleteChannelAsync(channelId, reason, cancellationToken),
            () => $"Deleted channel {channelId}{Because(reason)}",
            () => $"channel {channelId}",
            () => Emit(new ChannelDeleted(channelId, reason)));

    public Task<DiscordChannel> StartThreadAsync(Snowflake channelId, ThreadCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(StartThreadAsync), LogLevel.Information,
            () => _inner.StartThreadAsync(channelId, request, reason, cancellationToken),
            thread => $"Started {thread.Type} thread {thread.Name} ({thread.Id}) in channel {channelId}" +
                      $"{Uploaded(request.Message?.Files)}{Because(reason)}",
            () => $"channel {channelId}",
            thread => Emit(new ThreadCreated(channelId, thread.Id, thread.Type.ToString(), thread.Name)));

    public Task<DiscordChannel> StartThreadFromMessageAsync(Snowflake channelId, Snowflake messageId,
        ThreadFromMessageRequest request, string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(StartThreadFromMessageAsync), LogLevel.Information,
            () => _inner.StartThreadFromMessageAsync(channelId, messageId, request, reason, cancellationToken),
            thread =>
                $"Started thread {thread.Name} ({thread.Id}) from message {messageId} in channel {channelId}{Because(reason)}",
            () => $"message {messageId} in channel {channelId}",
            thread => Emit(new ThreadCreated(channelId, thread.Id, thread.Type.ToString(), thread.Name)));

    public Task<IReadOnlyList<DiscordGuildEmoji>> GetGuildEmojisAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildEmojisAsync), LogLevel.Debug,
            () => _inner.GetGuildEmojisAsync(guildId, cancellationToken),
            emojis => $"Fetched {emojis.Count} emojis from guild {guildId}",
            () => $"guild {guildId}");

    public Task<DiscordGuildEmoji> GetGuildEmojiAsync(Snowflake guildId, Snowflake emojiId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildEmojiAsync), LogLevel.Debug,
            () => _inner.GetGuildEmojiAsync(guildId, emojiId, cancellationToken),
            _ => $"Fetched emoji {emojiId} from guild {guildId}",
            () => $"emoji {emojiId} in guild {guildId}");

    public Task<DiscordGuildEmoji> CreateGuildEmojiAsync(Snowflake guildId, EmojiCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateGuildEmojiAsync), LogLevel.Information,
            () => _inner.CreateGuildEmojiAsync(guildId, request, reason, cancellationToken),
            emoji => $"Created emoji {emoji.Name} ({emoji.Id}) in guild {guildId}{Because(reason)}",
            () => $"guild {guildId}",
            emoji => Emit(new EmojiCreated(guildId, emoji.Id, emoji.Name)));

    public Task<DiscordGuildEmoji> ModifyGuildEmojiAsync(Snowflake guildId, Snowflake emojiId,
        EmojiModifyRequest request, string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(ModifyGuildEmojiAsync), LogLevel.Information,
            () => _inner.ModifyGuildEmojiAsync(guildId, emojiId, request, reason, cancellationToken),
            _ => $"Modified emoji {emojiId} in guild {guildId}{Because(reason)}",
            () => $"emoji {emojiId} in guild {guildId}",
            _ => Emit(new EmojiModified(guildId, emojiId)));

    public Task DeleteGuildEmojiAsync(Snowflake guildId, Snowflake emojiId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteGuildEmojiAsync), LogLevel.Information,
            () => _inner.DeleteGuildEmojiAsync(guildId, emojiId, reason, cancellationToken),
            () => $"Deleted emoji {emojiId} from guild {guildId}{Because(reason)}",
            () => $"emoji {emojiId} in guild {guildId}",
            () => Emit(new EmojiDeleted(guildId, emojiId, reason)));

    public Task<IReadOnlyList<DiscordApplicationCommand>> GetApplicationCommandsAsync(Snowflake applicationId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetApplicationCommandsAsync), LogLevel.Debug,
            () => _inner.GetApplicationCommandsAsync(applicationId, guildId, cancellationToken),
            commands => $"Fetched {commands.Count} commands for {Scope(applicationId, guildId)}",
            () => Scope(applicationId, guildId));

    public Task<DiscordApplicationCommand> CreateApplicationCommandAsync(Snowflake applicationId,
        ApplicationCommandRequest request, Snowflake? guildId = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateApplicationCommandAsync), LogLevel.Information,
            () => _inner.CreateApplicationCommandAsync(applicationId, request, guildId, cancellationToken),
            command => $"Registered command {command.Name} ({command.Id}) for {Scope(applicationId, guildId)}",
            () => Scope(applicationId, guildId),
            command => Emit(new ApplicationCommandCreated(applicationId, command.Id, command.Name, guildId?.Value)));

    public Task<DiscordApplicationCommand> EditApplicationCommandAsync(Snowflake applicationId,
        Snowflake commandId, ApplicationCommandRequest request, Snowflake? guildId = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(EditApplicationCommandAsync), LogLevel.Information,
            () => _inner.EditApplicationCommandAsync(applicationId, commandId, request, guildId, cancellationToken),
            _ => $"Edited command {commandId} for {Scope(applicationId, guildId)}",
            () => $"command {commandId} for {Scope(applicationId, guildId)}",
            _ => Emit(new ApplicationCommandEdited(applicationId, commandId, guildId?.Value)));

    public Task DeleteApplicationCommandAsync(Snowflake applicationId, Snowflake commandId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteApplicationCommandAsync), LogLevel.Information,
            () => _inner.DeleteApplicationCommandAsync(applicationId, commandId, guildId, cancellationToken),
            () => $"Deleted command {commandId} for {Scope(applicationId, guildId)}",
            () => $"command {commandId} for {Scope(applicationId, guildId)}",
            () => Emit(new ApplicationCommandDeleted(applicationId, commandId, guildId?.Value)));

    public Task<IReadOnlyList<DiscordApplicationCommand>> SetApplicationCommandsAsync(Snowflake applicationId,
        IReadOnlyList<ApplicationCommandRequest> requests, Snowflake? guildId = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(SetApplicationCommandsAsync), LogLevel.Information,
            () => _inner.SetApplicationCommandsAsync(applicationId, requests, guildId, cancellationToken),
            commands => $"Overwrote {Scope(applicationId, guildId)} with {commands.Count} commands",
            () => Scope(applicationId, guildId),
            commands => Emit(new ApplicationCommandsOverwritten(applicationId, commands.Count, guildId?.Value)));

    public Task CreateInteractionResponseAsync(Snowflake interactionId, string interactionToken,
        InteractionResponseRequest request, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateInteractionResponseAsync), LogLevel.Debug,
            () => _inner.CreateInteractionResponseAsync(interactionId, interactionToken, request, cancellationToken),
            () =>
                $"Answered interaction {interactionId} with {request.Type}{Uploaded(request.Message?.Files)}{Showing(request.Message?.Components)}",
            () => $"interaction {interactionId}",
            () => Emit(new InteractionResponded(interactionId, request.Type.ToString(),
                request.Message?.EffectiveFlags.HasFlag(MessageFlags.Ephemeral) ?? false)));

    public Task<DiscordMessage> GetOriginalInteractionResponseAsync(Snowflake applicationId,
        string interactionToken, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetOriginalInteractionResponseAsync), LogLevel.Debug,
            () => _inner.GetOriginalInteractionResponseAsync(applicationId, interactionToken, cancellationToken),
            message => $"Fetched the original response {message.Id} of application {applicationId}",
            () => $"application {applicationId}");

    public Task<DiscordMessage> EditOriginalInteractionResponseAsync(Snowflake applicationId,
        string interactionToken, InteractionMessageRequest request, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(EditOriginalInteractionResponseAsync), LogLevel.Information,
            () => _inner.EditOriginalInteractionResponseAsync(applicationId, interactionToken, request,
                cancellationToken),
            message =>
                $"Edited the original response {message.Id} of application {applicationId}{Uploaded(request.Files)}{Showing(request.Components)}",
            () => $"application {applicationId}",
            message => Emit(new MessageEdited(message.ChannelId, message.Id)));

    public Task DeleteOriginalInteractionResponseAsync(Snowflake applicationId, string interactionToken,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteOriginalInteractionResponseAsync), LogLevel.Information,
            () => _inner.DeleteOriginalInteractionResponseAsync(applicationId, interactionToken, cancellationToken),
            () => $"Deleted the original response of application {applicationId}",
            () => $"application {applicationId}");

    public Task<DiscordMessage> CreateFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        InteractionMessageRequest request, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateFollowupMessageAsync), LogLevel.Information,
            () => _inner.CreateFollowupMessageAsync(applicationId, interactionToken, request, cancellationToken),
            message =>
                $"Sent follow-up {message.Id} for application {applicationId}{Uploaded(request.Files)}{Showing(request.Components)}",
            () => $"application {applicationId}",
            message => Emit(new InteractionFollowedUp(applicationId, message.Id,
                request.EffectiveFlags.HasFlag(MessageFlags.Ephemeral))));

    public Task<DiscordMessage> EditFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        Snowflake messageId, InteractionMessageRequest request, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(EditFollowupMessageAsync), LogLevel.Information,
            () => _inner.EditFollowupMessageAsync(applicationId, interactionToken, messageId, request,
                cancellationToken),
            _ =>
                $"Edited follow-up {messageId} of application {applicationId}{Uploaded(request.Files)}{Showing(request.Components)}",
            () => $"follow-up {messageId} of application {applicationId}",
            message => Emit(new MessageEdited(message.ChannelId, message.Id)));

    public Task DeleteFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        Snowflake messageId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteFollowupMessageAsync), LogLevel.Information,
            () => _inner.DeleteFollowupMessageAsync(applicationId, interactionToken, messageId, cancellationToken),
            () => $"Deleted follow-up {messageId} of application {applicationId}",
            () => $"follow-up {messageId} of application {applicationId}");

    public Task<DiscordGuild> GetGuildAsync(Snowflake guildId, bool withCounts = false,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildAsync), LogLevel.Debug,
            () => _inner.GetGuildAsync(guildId, withCounts, cancellationToken),
            _ => $"Fetched guild {guildId}",
            () => $"guild {guildId}");

    public Task<IReadOnlyList<DiscordChannel>> GetGuildChannelsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildChannelsAsync), LogLevel.Debug,
            () => _inner.GetGuildChannelsAsync(guildId, cancellationToken),
            channels => $"Fetched {channels.Count} channels of guild {guildId}",
            () => $"channels of guild {guildId}");

    public Task<DiscordMember> GetGuildMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildMemberAsync), LogLevel.Debug,
            () => _inner.GetGuildMemberAsync(guildId, userId, cancellationToken),
            _ => $"Fetched member {userId} of guild {guildId}",
            () => $"member {userId} of guild {guildId}");

    public Task<IReadOnlyList<DiscordMember>> GetGuildMembersAsync(Snowflake guildId, MemberQuery? query = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildMembersAsync), LogLevel.Debug,
            () => _inner.GetGuildMembersAsync(guildId, query, cancellationToken),
            members => $"Fetched {members.Count} members of guild {guildId}",
            () => $"members of guild {guildId}",
            members => Emit(new MembersFetched(guildId, members.Count, query?.After?.Value)));

    public Task<IReadOnlyList<DiscordMember>> SearchGuildMembersAsync(Snowflake guildId, string search,
        int limit = 1, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(SearchGuildMembersAsync), LogLevel.Debug,
            () => _inner.SearchGuildMembersAsync(guildId, search, limit, cancellationToken),
            members => $"Found {members.Count} members matching '{search}' in guild {guildId}",
            () => $"member search '{search}' in guild {guildId}");

    public Task<DiscordMember> ModifyGuildMemberAsync(Snowflake guildId, Snowflake userId,
        MemberModifyRequest request, string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(ModifyGuildMemberAsync), LogLevel.Information,
            () => _inner.ModifyGuildMemberAsync(guildId, userId, request, reason, cancellationToken),
            _ => $"Modified member {userId} of guild {guildId}{Because(reason)}",
            () => $"member {userId} of guild {guildId}",
            _ => Emit(new MemberModified(guildId, userId, Changes(request))));

    public Task AddGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(AddGuildMemberRoleAsync), LogLevel.Information,
            () => _inner.AddGuildMemberRoleAsync(guildId, userId, roleId, reason, cancellationToken),
            () => $"Granted role {roleId} to member {userId} of guild {guildId}{Because(reason)}",
            () => $"role {roleId} for member {userId} of guild {guildId}",
            () => Emit(new MemberRoleChanged(guildId, userId, roleId, true)));

    public Task RemoveGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(RemoveGuildMemberRoleAsync), LogLevel.Information,
            () => _inner.RemoveGuildMemberRoleAsync(guildId, userId, roleId, reason, cancellationToken),
            () => $"Revoked role {roleId} from member {userId} of guild {guildId}{Because(reason)}",
            () => $"role {roleId} for member {userId} of guild {guildId}",
            () => Emit(new MemberRoleChanged(guildId, userId, roleId, false)));

    public Task RemoveGuildMemberAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(RemoveGuildMemberAsync), LogLevel.Warning,
            () => _inner.RemoveGuildMemberAsync(guildId, userId, reason, cancellationToken),
            () => $"Kicked member {userId} from guild {guildId}{Because(reason)}",
            () => $"member {userId} of guild {guildId}",
            () => Emit(new MemberKicked(guildId, userId, reason)));

    public Task<IReadOnlyList<DiscordBan>> GetGuildBansAsync(Snowflake guildId, BanQuery? query = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildBansAsync), LogLevel.Debug,
            () => _inner.GetGuildBansAsync(guildId, query, cancellationToken),
            bans => $"Fetched {bans.Count} bans of guild {guildId}",
            () => $"bans of guild {guildId}");

    public Task<DiscordBan?> GetGuildBanAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildBanAsync), LogLevel.Debug,
            () => _inner.GetGuildBanAsync(guildId, userId, cancellationToken),
            ban => ban is null
                ? $"User {userId} is not banned in guild {guildId}"
                : $"Fetched the ban of {userId} in guild {guildId}",
            () => $"ban of {userId} in guild {guildId}");

    public Task CreateGuildBanAsync(Snowflake guildId, Snowflake userId, BanCreateRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateGuildBanAsync), LogLevel.Warning,
            () => _inner.CreateGuildBanAsync(guildId, userId, request, reason, cancellationToken),
            () => $"Banned {userId} from guild {guildId}{Because(reason)}",
            () => $"ban of {userId} in guild {guildId}",
            () => Emit(new MemberBanned(guildId, userId, request?.DeleteMessageSeconds ?? 0, reason)));

    public Task RemoveGuildBanAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(RemoveGuildBanAsync), LogLevel.Information,
            () => _inner.RemoveGuildBanAsync(guildId, userId, reason, cancellationToken),
            () => $"Unbanned {userId} in guild {guildId}{Because(reason)}",
            () => $"ban of {userId} in guild {guildId}",
            () => Emit(new MemberUnbanned(guildId, userId, reason)));

    public Task<IReadOnlyList<DiscordRole>> GetGuildRolesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildRolesAsync), LogLevel.Debug,
            () => _inner.GetGuildRolesAsync(guildId, cancellationToken),
            roles => $"Fetched {roles.Count} roles of guild {guildId}",
            () => $"roles of guild {guildId}");

    public Task<DiscordRole> CreateGuildRoleAsync(Snowflake guildId, RoleCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateGuildRoleAsync), LogLevel.Information,
            () => _inner.CreateGuildRoleAsync(guildId, request, reason, cancellationToken),
            role => $"Created role {role.Name} ({role.Id}) in guild {guildId}{Because(reason)}",
            () => $"role in guild {guildId}",
            role => Emit(new RoleCreated(guildId, role.Id, role.Name, (ulong)role.Permissions)));

    public Task<DiscordRole> ModifyGuildRoleAsync(Snowflake guildId, Snowflake roleId,
        RoleModifyRequest request, string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(ModifyGuildRoleAsync), LogLevel.Information,
            () => _inner.ModifyGuildRoleAsync(guildId, roleId, request, reason, cancellationToken),
            role => $"Modified role {role.Name} ({roleId}) in guild {guildId}{Because(reason)}",
            () => $"role {roleId} in guild {guildId}",
            role => Emit(new RoleModified(guildId, roleId, role.Name, (ulong)role.Permissions)));

    public Task DeleteGuildRoleAsync(Snowflake guildId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteGuildRoleAsync), LogLevel.Warning,
            () => _inner.DeleteGuildRoleAsync(guildId, roleId, reason, cancellationToken),
            () => $"Deleted role {roleId} in guild {guildId}{Because(reason)}",
            () => $"role {roleId} in guild {guildId}",
            () => Emit(new RoleDeleted(guildId, roleId, reason)));

    public IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, MessageQuery query,
        CancellationToken cancellationToken = default) =>
        TrackMessagesAsync(_inner.GetMessagesAsync(channelId, query, cancellationToken), channelId,
            count => $"Read {count} messages from channel {channelId} ({Describe(query)})", cancellationToken);

    public Task BulkDeleteMessagesAsync(Snowflake channelId, IReadOnlyList<Snowflake> messageIds,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(BulkDeleteMessagesAsync), LogLevel.Warning,
            () => _inner.BulkDeleteMessagesAsync(channelId, messageIds, reason, cancellationToken),
            () => $"Bulk deleted {messageIds.Count} messages in channel {channelId}{Because(reason)}",
            () => $"{messageIds.Count} messages in channel {channelId}",
            () => Emit(new MessagesBulkDeleted(channelId, messageIds.Count, reason)));

    public Task<DiscordMessage> CrosspostMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CrosspostMessageAsync), LogLevel.Information,
            () => _inner.CrosspostMessageAsync(channelId, messageId, cancellationToken),
            _ => $"Crossposted message {messageId} from channel {channelId}",
            () => $"message {messageId} in channel {channelId}",
            _ => Emit(new MessageCrossposted(channelId, messageId)));

    public Task<IReadOnlyList<DiscordMessage>> GetPinnedMessagesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetPinnedMessagesAsync), LogLevel.Debug,
            () => _inner.GetPinnedMessagesAsync(channelId, cancellationToken),
            messages => $"Fetched {messages.Count} pinned messages from channel {channelId}",
            () => $"pins in channel {channelId}");

    public Task PinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(PinMessageAsync), LogLevel.Information,
            () => _inner.PinMessageAsync(channelId, messageId, reason, cancellationToken),
            () => $"Pinned message {messageId} in channel {channelId}{Because(reason)}",
            () => $"message {messageId} in channel {channelId}",
            () => Emit(new MessagePinned(channelId, messageId, reason)));

    public Task UnpinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(UnpinMessageAsync), LogLevel.Information,
            () => _inner.UnpinMessageAsync(channelId, messageId, reason, cancellationToken),
            () => $"Unpinned message {messageId} in channel {channelId}{Because(reason)}",
            () => $"message {messageId} in channel {channelId}",
            () => Emit(new MessageUnpinned(channelId, messageId, reason)));

    public Task TriggerTypingAsync(Snowflake channelId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(TriggerTypingAsync), LogLevel.Trace,
            () => _inner.TriggerTypingAsync(channelId, cancellationToken),
            () => $"Triggered typing in channel {channelId}",
            () => $"channel {channelId}",
            () => Emit(new TypingTriggered(channelId)));

    public Task<IReadOnlyList<DiscordUser>> GetReactionsAsync(Snowflake channelId, Snowflake messageId,
        DiscordEmoji emoji, ReactionQuery? query = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetReactionsAsync), LogLevel.Debug,
            () => _inner.GetReactionsAsync(channelId, messageId, emoji, query, cancellationToken),
            users => $"Fetched {users.Count} reactors of {Describe(emoji)} on message {messageId}",
            () => $"{Describe(emoji)} on message {messageId} in channel {channelId}");

    public Task DeleteUserReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        Snowflake userId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteUserReactionAsync), LogLevel.Debug,
            () => _inner.DeleteUserReactionAsync(channelId, messageId, emoji, userId, cancellationToken),
            () => $"Removed {Describe(emoji)} by {userId} from message {messageId}",
            () => $"{Describe(emoji)} by {userId} on message {messageId}",
            () => Emit(new UserReactionRemoved(channelId, messageId, Describe(emoji), userId)));

    public Task DeleteAllReactionsAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteAllReactionsAsync), LogLevel.Information,
            () => _inner.DeleteAllReactionsAsync(channelId, messageId, cancellationToken),
            () => $"Cleared all reactions on message {messageId} in channel {channelId}",
            () => $"message {messageId} in channel {channelId}",
            () => Emit(new ReactionsCleared(channelId, messageId, null)));

    public Task DeleteEmojiReactionsAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteEmojiReactionsAsync), LogLevel.Information,
            () => _inner.DeleteEmojiReactionsAsync(channelId, messageId, emoji, cancellationToken),
            () => $"Cleared {Describe(emoji)} reactions on message {messageId}",
            () => $"{Describe(emoji)} on message {messageId} in channel {channelId}",
            () => Emit(new ReactionsCleared(channelId, messageId, Describe(emoji))));

    public Task<DiscordUser> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetCurrentUserAsync), LogLevel.Debug,
            () => _inner.GetCurrentUserAsync(cancellationToken),
            user => $"Fetched current user {user.Username} ({user.Id})",
            () => "current user");

    public Task<DiscordUser> GetUserAsync(Snowflake userId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetUserAsync), LogLevel.Debug,
            () => _inner.GetUserAsync(userId, cancellationToken),
            user => $"Fetched user {user.Username} ({userId})",
            () => $"user {userId}");

    public Task<DiscordChannel> CreateDirectMessageChannelAsync(Snowflake userId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateDirectMessageChannelAsync), LogLevel.Information,
            () => _inner.CreateDirectMessageChannelAsync(userId, cancellationToken),
            channel => $"Opened direct channel {channel.Id} with user {userId}",
            () => $"user {userId}",
            channel => Emit(new DirectChannelOpened(userId, channel.Id)));

    public Task LeaveGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(LeaveGuildAsync), LogLevel.Warning,
            () => _inner.LeaveGuildAsync(guildId, cancellationToken),
            () => $"Left guild {guildId}",
            () => $"guild {guildId}",
            () => Emit(new GuildLeft(guildId)));

    public Task<GatewayBotInfo> GetGatewayBotAsync(CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGatewayBotAsync), LogLevel.Information,
            () => _inner.GetGatewayBotAsync(cancellationToken),
            info => $"Gateway {info.Url} recommends {info.Shards} shards, " +
                    $"{info.SessionStartLimit.Remaining}/{info.SessionStartLimit.Total} sessions left",
            () => "gateway bot info");

    public Task<IReadOnlyList<DiscordInvite>> GetChannelInvitesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetChannelInvitesAsync), LogLevel.Debug,
            () => _inner.GetChannelInvitesAsync(channelId, cancellationToken),
            invites => $"Fetched {invites.Count} invites for channel {channelId}",
            () => $"channel {channelId}");

    public Task<IReadOnlyList<DiscordInvite>> GetGuildInvitesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildInvitesAsync), LogLevel.Debug,
            () => _inner.GetGuildInvitesAsync(guildId, cancellationToken),
            invites => $"Fetched {invites.Count} invites for guild {guildId}",
            () => $"guild {guildId}");

    public Task<DiscordInvite> CreateChannelInviteAsync(Snowflake channelId,
        InviteCreateRequest? request = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(CreateChannelInviteAsync), LogLevel.Information,
            () => _inner.CreateChannelInviteAsync(channelId, request, reason, cancellationToken),
            invite => $"Created invite {invite.Code} for channel {channelId}{Because(reason)}",
            () => $"channel {channelId}",
            invite => Emit(new InviteIssued(channelId, invite.Code, invite.MaxUses, invite.MaxAge)));

    public Task<DiscordInvite> GetInviteAsync(string code, bool withCounts = false,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetInviteAsync), LogLevel.Debug,
            () => _inner.GetInviteAsync(code, withCounts, cancellationToken),
            _ => $"Fetched invite {code}",
            () => $"invite {code}");

    public Task DeleteInviteAsync(string code, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteInviteAsync), LogLevel.Warning,
            () => _inner.DeleteInviteAsync(code, reason, cancellationToken),
            () => $"Deleted invite {code}{Because(reason)}",
            () => $"invite {code}",
            () => Emit(new InviteRevoked(code, reason)));

    public Task<DiscordAuditLog> GetGuildAuditLogAsync(Snowflake guildId, AuditLogQuery? query = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildAuditLogAsync), LogLevel.Debug,
            () => _inner.GetGuildAuditLogAsync(guildId, query, cancellationToken),
            log => $"Fetched {log.Entries.Count} audit log entries for guild {guildId}",
            () => $"guild {guildId}");

    public Task<DiscordGuild> ModifyGuildAsync(Snowflake guildId, GuildModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(ModifyGuildAsync), LogLevel.Information,
            () => _inner.ModifyGuildAsync(guildId, request, reason, cancellationToken),
            guild => $"Modified guild {guild.Name} ({guildId}){Because(reason)}",
            () => $"guild {guildId}",
            _ => Emit(new GuildModified(guildId, Changes(request), reason)));

    public Task<int> GetGuildPruneCountAsync(Snowflake guildId, PruneRequest? request = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildPruneCountAsync), LogLevel.Debug,
            () => _inner.GetGuildPruneCountAsync(guildId, request, cancellationToken),
            count => $"Prune of guild {guildId} would remove {count} members",
            () => $"guild {guildId}");

    public Task<int?> BeginGuildPruneAsync(Snowflake guildId, PruneRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var days = (request ?? new PruneRequest()).Days;

        return TrackAsync(nameof(BeginGuildPruneAsync), LogLevel.Warning,
            () => _inner.BeginGuildPruneAsync(guildId, request, reason, cancellationToken),
            removed =>
                $"Pruned {Report(removed)} members inactive for {days} days from guild {guildId}{Because(reason)}",
            () => $"guild {guildId}",
            removed => Emit(new GuildPruned(guildId, days, removed, reason)));
    }

    public Task<ThreadListing> GetActiveThreadsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetActiveThreadsAsync), LogLevel.Debug,
            () => _inner.GetActiveThreadsAsync(guildId, cancellationToken),
            listing => $"Fetched {listing.Count} active threads in guild {guildId}",
            () => $"guild {guildId}");

    public Task<ThreadListing> GetPublicArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetPublicArchivedThreadsAsync), LogLevel.Debug,
            () => _inner.GetPublicArchivedThreadsAsync(channelId, query, cancellationToken),
            listing => $"Fetched {listing.Count} public archived threads in channel {channelId}",
            () => $"channel {channelId}");

    public Task<ThreadListing> GetPrivateArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetPrivateArchivedThreadsAsync), LogLevel.Debug,
            () => _inner.GetPrivateArchivedThreadsAsync(channelId, query, cancellationToken),
            listing => $"Fetched {listing.Count} private archived threads in channel {channelId}",
            () => $"channel {channelId}");

    public Task<ThreadListing> GetJoinedPrivateArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetJoinedPrivateArchivedThreadsAsync), LogLevel.Debug,
            () => _inner.GetJoinedPrivateArchivedThreadsAsync(channelId, query, cancellationToken),
            listing => $"Fetched {listing.Count} joined private archived threads in channel {channelId}",
            () => $"channel {channelId}");

    public Task JoinThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(JoinThreadAsync), LogLevel.Information,
            () => _inner.JoinThreadAsync(threadId, cancellationToken),
            () => $"Joined thread {threadId}",
            () => $"thread {threadId}",
            () => Emit(new ThreadJoined(threadId)));

    public Task LeaveThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(LeaveThreadAsync), LogLevel.Information,
            () => _inner.LeaveThreadAsync(threadId, cancellationToken),
            () => $"Left thread {threadId}",
            () => $"thread {threadId}",
            () => Emit(new ThreadLeft(threadId)));

    public Task AddThreadMemberAsync(Snowflake threadId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(AddThreadMemberAsync), LogLevel.Information,
            () => _inner.AddThreadMemberAsync(threadId, userId, cancellationToken),
            () => $"Added user {userId} to thread {threadId}",
            () => $"user {userId} in thread {threadId}",
            () => Emit(new ThreadMemberAdded(threadId, userId)));

    public Task RemoveThreadMemberAsync(Snowflake threadId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(RemoveThreadMemberAsync), LogLevel.Information,
            () => _inner.RemoveThreadMemberAsync(threadId, userId, cancellationToken),
            () => $"Removed user {userId} from thread {threadId}",
            () => $"user {userId} in thread {threadId}",
            () => Emit(new ThreadMemberRemoved(threadId, userId)));

    public Task<DiscordThreadMember> GetThreadMemberAsync(Snowflake threadId, Snowflake userId,
        bool withMember = false, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetThreadMemberAsync), LogLevel.Debug,
            () => _inner.GetThreadMemberAsync(threadId, userId, withMember, cancellationToken),
            _ => $"Fetched membership of {userId} in thread {threadId}",
            () => $"user {userId} in thread {threadId}");

    public Task<IReadOnlyList<DiscordThreadMember>> GetThreadMembersAsync(Snowflake threadId,
        bool withMember = false, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetThreadMembersAsync), LogLevel.Debug,
            () => _inner.GetThreadMembersAsync(threadId, withMember, cancellationToken),
            members => $"Fetched {members.Count} members of thread {threadId}",
            () => $"thread {threadId}");

    public Task<IReadOnlyList<DiscordCommandPermissions>> GetGuildCommandPermissionsAsync(
        Snowflake applicationId, Snowflake guildId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetGuildCommandPermissionsAsync), LogLevel.Debug,
            () => _inner.GetGuildCommandPermissionsAsync(applicationId, guildId, cancellationToken),
            permissions =>
                $"Fetched {permissions.Count} command permission sets for {Scope(applicationId, guildId)}",
            () => Scope(applicationId, guildId));

    public Task<DiscordCommandPermissions> GetCommandPermissionsAsync(Snowflake applicationId,
        Snowflake guildId, Snowflake commandId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetCommandPermissionsAsync), LogLevel.Debug,
            () => _inner.GetCommandPermissionsAsync(applicationId, guildId, commandId, cancellationToken),
            permissions =>
                $"Fetched {permissions.Permissions.Count} permissions for command {commandId} in guild {guildId}",
            () => $"command {commandId} in {Scope(applicationId, guildId)}");

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

    private async IAsyncEnumerable<DiscordMessage> TrackMessagesAsync(IAsyncEnumerable<DiscordMessage> source,
        Snowflake channelId, Func<int, string> succeeded,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        var count = 0;

        var messages = source.GetAsyncEnumerator(cancellationToken);

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

        Succeeded(nameof(GetMessagesAsync), start, LogLevel.Debug, succeeded(count));
    }

    private async Task<T> TrackAsync<T>(string operation, LogLevel level, Func<Task<T>> action,
        Func<T, string> succeeded, Func<string> failed, Action<T>? emit = null)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            var result = await action();

            Succeeded(operation, start, level, succeeded(result));
            emit?.Invoke(result);

            return result;
        }
        catch (Exception exception)
        {
            Failed(operation, start, exception, failed());
            throw;
        }
    }

    private async Task TrackAsync(string operation, LogLevel level, Func<Task> action,
        Func<string> succeeded, Func<string> failed, Action? emit = null)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await action();

            Succeeded(operation, start, level, succeeded());
            emit?.Invoke();
        }
        catch (Exception exception)
        {
            Failed(operation, start, exception, failed());
            throw;
        }
    }

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

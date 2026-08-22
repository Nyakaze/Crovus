using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Crovus.Client;
using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Json;

namespace Crovus.Rest;

public sealed class DiscordRestClient : IDiscordRest, IContextAware
{
    private const string LogCategory = "Rest.Http";
    private const int MaxMessagePageSize = 100;

    private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(1);

    private readonly DiscordRestOptions _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly RateLimiter _limiter;
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private readonly TimeProvider _time;
    private readonly string _authorization;

    private ICrovusContext? _context;
    private bool _disposed;

    public DiscordRestClient(DiscordRestOptions options, HttpClient? httpClient = null, RateLimiter? rateLimiter = null,
        ILogger? logger = null, ITelemetry? telemetry = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _time = timeProvider ?? TimeProvider.System;
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;
        _limiter = rateLimiter ?? new RateLimiter(_time, logger, telemetry);
        _authorization = options.BuildAuthorization();
        _ownsHttpClient = httpClient is null;
        _http = httpClient ?? new HttpClient();

        _http.BaseAddress ??= options.BuildBaseAddress();

        if (_ownsHttpClient)
            _http.Timeout = options.Timeout;

        if (!_http.DefaultRequestHeaders.UserAgent.TryParseAdd(options.UserAgent))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
    }

    public DiscordRestClient(DiscordRestOptions options, DiagnosticsHub diagnostics, HttpClient? httpClient = null,
        RateLimiter? rateLimiter = null, TimeProvider? timeProvider = null)
        : this(options, httpClient, rateLimiter, diagnostics, diagnostics, timeProvider)
    {
    }

    public ICrovusContext? Context
    {
        get => _context ??= new RestContext(this, _logger, _telemetry);
        set => _context = value;
    }

    public async Task<DiscordChannel> GetChannelAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/channels/{channel_id}", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}", cancellationToken: cancellationToken);

        return await ReadAsync<DiscordChannel>(response, route, cancellationToken);
    }

    public async Task<DiscordMessage> GetMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/channels/{channel_id}/messages/{message_id}", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/messages/{messageId}",
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordMessage>(response, route, cancellationToken);
    }

    public async IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, Snowflake? before = null,
        int? limit = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit is <= 0)
            yield break;

        var route = RouteKey.Get("/channels/{channel_id}/messages", channelId.ToString());
        var remaining = limit;
        var cursor = before;

        while (remaining is null or > 0)
        {
            var pageSize = Math.Min(MaxMessagePageSize, remaining ?? MaxMessagePageSize);
            var path = $"channels/{channelId}/messages?limit={pageSize}";

            if (cursor is { } value)
                path += $"&before={value}";

            IReadOnlyList<DiscordMessage> page;

            using (var response = await SendAsync(route, path, cancellationToken: cancellationToken))
                page = await ReadAsync<List<DiscordMessage>>(response, route, cancellationToken);

            if (page.Count == 0)
                yield break;

            foreach (var message in page)
            {
                yield return message;

                if (remaining is not { } left)
                    continue;

                remaining = left - 1;

                if (remaining == 0)
                    yield break;
            }

            if (page.Count < pageSize)
                yield break;

            cursor = page[^1].Id;
        }
    }

    public async Task<DiscordMessage> CreateMessageAsync(Snowflake channelId, MessageCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Post("/channels/{channel_id}/messages", channelId.ToString());
        var payload = MessageCreatePayload.From(request);

        using var response = await SendAsync(route, $"channels/{channelId}/messages",
            Body(route, payload, request.Files, request.Components, nameof(request)), cancellationToken: cancellationToken);

        return await ReadAsync<DiscordMessage>(response, route, cancellationToken);
    }

    public async Task<DiscordMessage> EditMessageAsync(Snowflake channelId, Snowflake messageId,
        MessageEditRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Patch("/channels/{channel_id}/messages/{message_id}", channelId.ToString());
        var payload = MessageEditPayload.From(request);

        using var response = await SendAsync(route, $"channels/{channelId}/messages/{messageId}",
            Body(route, payload, request.Files, request.Components, nameof(request)), cancellationToken: cancellationToken);

        return await ReadAsync<DiscordMessage>(response, route, cancellationToken);
    }

    public async Task DeleteMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/channels/{channel_id}/messages/{message_id}", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/messages/{messageId}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async Task CreateReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var route = RouteKey.Put("/channels/{channel_id}/messages/{message_id}/reactions/{emoji}/@me",
            channelId.ToString());

        using var response = await SendAsync(route,
            $"channels/{channelId}/messages/{messageId}/reactions/{emoji.ToReactionPath()}/@me",
            cancellationToken: cancellationToken);
    }

    public async Task DeleteOwnReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var route = RouteKey.Delete("/channels/{channel_id}/messages/{message_id}/reactions/{emoji}/@me",
            channelId.ToString());

        using var response = await SendAsync(route,
            $"channels/{channelId}/messages/{messageId}/reactions/{emoji.ToReactionPath()}/@me",
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordWebhook>> GetChannelWebhooksAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/channels/{channel_id}/webhooks", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/webhooks",
            cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordWebhook>>(response, route, cancellationToken);
    }

    public async Task<DiscordWebhook> GetWebhookAsync(Snowflake webhookId, string? token = null,
        CancellationToken cancellationToken = default)
    {
        var route = token is null
            ? RouteKey.Get("/webhooks/{webhook_id}", webhookId.ToString())
            : RouteKey.Get("/webhooks/{webhook_id}/{webhook_token}", webhookId.ToString());

        var path = token is null ? $"webhooks/{webhookId}" : $"webhooks/{webhookId}/{Uri.EscapeDataString(token)}";

        using var response = await SendAsync(route, path, authorize: token is null,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordWebhook>(response, route, cancellationToken);
    }

    public async Task<DiscordWebhook> CreateWebhookAsync(Snowflake channelId, WebhookCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Post("/channels/{channel_id}/webhooks", channelId.ToString());
        var payload = new WebhookCreatePayload(request.Name, request.AvatarData);

        using var response = await SendAsync(route, $"channels/{channelId}/webhooks",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordWebhook>(response, route, cancellationToken);
    }

    public async Task<DiscordWebhook> ModifyWebhookAsync(Snowflake webhookId, WebhookModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Patch("/webhooks/{webhook_id}", webhookId.ToString());
        var payload = new WebhookModifyPayload(request.Name, request.AvatarData, request.ChannelId);

        using var response = await SendAsync(route, $"webhooks/{webhookId}",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordWebhook>(response, route, cancellationToken);
    }

    public async Task DeleteWebhookAsync(Snowflake webhookId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/webhooks/{webhook_id}", webhookId.ToString());

        using var response = await SendAsync(route, $"webhooks/{webhookId}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async Task<DiscordMessage?> ExecuteWebhookAsync(DiscordWebhook webhook, WebhookExecuteRequest request,
        Snowflake? threadId = null, bool wait = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhook);
        ArgumentNullException.ThrowIfNull(request);

        if (webhook.Token is not { } token)
            throw new InvalidOperationException($"Webhook {webhook.Id} has no token and cannot be executed.");

        var route = RouteKey.Post("/webhooks/{webhook_id}/{webhook_token}", webhook.Id.ToString());
        var path = $"webhooks/{webhook.Id}/{Uri.EscapeDataString(token)}";
        var query = new List<string>(2);

        if (wait)
            query.Add("wait=true");

        if (threadId is { } thread)
            query.Add($"thread_id={thread}");

        if (query.Count > 0)
            path += $"?{string.Join('&', query)}";

        var payload = WebhookExecutePayload.From(request);

        using var response = await SendAsync(route, path, Body(route, payload, request.Files, request.Components, nameof(request)),
            authorize: false, cancellationToken: cancellationToken);

        if (!wait || response.StatusCode is HttpStatusCode.NoContent)
            return null;

        return await ReadAsync<DiscordMessage>(response, route, cancellationToken);
    }

    public async Task<DiscordChannel> CreateChannelAsync(Snowflake guildId, ChannelCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Post("/guilds/{guild_id}/channels", guildId.ToString());
        var payload = new ChannelCreatePayload(request.Name, request.Type, request.Topic, request.Position,
            request.Nsfw, request.RateLimitPerUser, request.Bitrate, request.UserLimit, request.ParentId,
            request.DefaultAutoArchiveDuration, request.PermissionOverwrites);

        using var response = await SendAsync(route, $"guilds/{guildId}/channels",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordChannel>(response, route, cancellationToken);
    }

    public async Task<DiscordChannel> ModifyChannelAsync(Snowflake channelId, ChannelModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Patch("/channels/{channel_id}", channelId.ToString());
        var payload = new ChannelModifyPayload(request.Name, request.Type, request.Topic, request.Position,
            request.Nsfw, request.RateLimitPerUser, request.Bitrate, request.UserLimit, request.ParentId,
            request.DefaultAutoArchiveDuration, request.PermissionOverwrites, request.Archived, request.Locked,
            request.Invitable, request.AutoArchiveDuration, request.AppliedTags);

        using var response = await SendAsync(route, $"channels/{channelId}",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordChannel>(response, route, cancellationToken);
    }

    public async Task DeleteChannelAsync(Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/channels/{channel_id}", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async Task<DiscordChannel> StartThreadAsync(Snowflake channelId, ThreadCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Post("/channels/{channel_id}/threads", channelId.ToString());
        var message = request.Message is { } starter ? MessageCreatePayload.From(starter) : null;

        var payload = new ThreadCreatePayload(request.Name, request.Type, request.AutoArchiveDuration,
            request.Invitable, request.RateLimitPerUser, message, request.AppliedTags);

        using var response = await SendAsync(route, $"channels/{channelId}/threads",
            Body(route, payload, request.Message?.Files, request.Message?.Components, nameof(request)), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordChannel>(response, route, cancellationToken);
    }

    public async Task<DiscordChannel> StartThreadFromMessageAsync(Snowflake channelId, Snowflake messageId,
        ThreadFromMessageRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Post("/channels/{channel_id}/messages/{message_id}/threads", channelId.ToString());
        var payload = new ThreadFromMessagePayload(request.Name, request.AutoArchiveDuration,
            request.RateLimitPerUser);

        using var response = await SendAsync(route, $"channels/{channelId}/messages/{messageId}/threads",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordChannel>(response, route, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordGuildEmoji>> GetGuildEmojisAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/emojis", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/emojis",
            cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordGuildEmoji>>(response, route, cancellationToken);
    }

    public async Task<DiscordGuildEmoji> GetGuildEmojiAsync(Snowflake guildId, Snowflake emojiId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/emojis/{emoji_id}", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/emojis/{emojiId}",
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordGuildEmoji>(response, route, cancellationToken);
    }

    public async Task<DiscordGuildEmoji> CreateGuildEmojiAsync(Snowflake guildId, EmojiCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Post("/guilds/{guild_id}/emojis", guildId.ToString());
        var payload = new EmojiCreatePayload(request.Name, request.ImageData, request.Roles ?? []);

        using var response = await SendAsync(route, $"guilds/{guildId}/emojis",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordGuildEmoji>(response, route, cancellationToken);
    }

    public async Task<DiscordGuildEmoji> ModifyGuildEmojiAsync(Snowflake guildId, Snowflake emojiId,
        EmojiModifyRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Patch("/guilds/{guild_id}/emojis/{emoji_id}", guildId.ToString());
        var payload = new EmojiModifyPayload(request.Name, request.Roles);

        using var response = await SendAsync(route, $"guilds/{guildId}/emojis/{emojiId}",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordGuildEmoji>(response, route, cancellationToken);
    }

    public async Task DeleteGuildEmojiAsync(Snowflake guildId, Snowflake emojiId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/guilds/{guild_id}/emojis/{emoji_id}", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/emojis/{emojiId}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordApplicationCommand>> GetApplicationCommandsAsync(Snowflake applicationId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        var route = CommandRoute(HttpMethod.Get, applicationId, guildId, false);

        using var response = await SendAsync(route, CommandPath(applicationId, guildId),
            cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordApplicationCommand>>(response, route, cancellationToken);
    }

    public async Task<DiscordApplicationCommand> CreateApplicationCommandAsync(Snowflake applicationId,
        ApplicationCommandRequest request, Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = CommandRoute(HttpMethod.Post, applicationId, guildId, false);
        var payload = ApplicationCommandPayload.From(request);

        using var response = await SendAsync(route, CommandPath(applicationId, guildId),
            () => JsonContent.Create(payload, options: DiscordJson.Options),
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordApplicationCommand>(response, route, cancellationToken);
    }

    public async Task<DiscordApplicationCommand> EditApplicationCommandAsync(Snowflake applicationId,
        Snowflake commandId, ApplicationCommandRequest request, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = CommandRoute(HttpMethod.Patch, applicationId, guildId, true);
        var payload = ApplicationCommandPayload.From(request);

        using var response = await SendAsync(route, $"{CommandPath(applicationId, guildId)}/{commandId}",
            () => JsonContent.Create(payload, options: DiscordJson.Options),
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordApplicationCommand>(response, route, cancellationToken);
    }

    public async Task DeleteApplicationCommandAsync(Snowflake applicationId, Snowflake commandId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        var route = CommandRoute(HttpMethod.Delete, applicationId, guildId, true);

        using var response = await SendAsync(route, $"{CommandPath(applicationId, guildId)}/{commandId}",
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordApplicationCommand>> SetApplicationCommandsAsync(Snowflake applicationId,
        IReadOnlyList<ApplicationCommandRequest> requests, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var route = CommandRoute(HttpMethod.Put, applicationId, guildId, false);
        var payload = requests.Select(ApplicationCommandPayload.From).ToArray();

        using var response = await SendAsync(route, CommandPath(applicationId, guildId),
            () => JsonContent.Create(payload, options: DiscordJson.Options),
            cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordApplicationCommand>>(response, route, cancellationToken);
    }

    public async Task CreateInteractionResponseAsync(Snowflake interactionId, string interactionToken,
        InteractionResponseRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(interactionToken);

        var route = RouteKey.Post("/interactions/{interaction_id}/{interaction_token}/callback",
            interactionId.ToString());

        if (request.Modal is { } modal)
            ComponentLimit.Modal(modal, nameof(request));

        var payload = InteractionCallbackPayload.From(request);

        using var response = await SendAsync(route,
            $"interactions/{interactionId}/{Uri.EscapeDataString(interactionToken)}/callback",
            Body(route, payload, request.Message?.Files, request.Message?.Components, nameof(request)), authorize: false,
            cancellationToken: cancellationToken);
    }

    public async Task<DiscordMessage> GetOriginalInteractionResponseAsync(Snowflake applicationId,
        string interactionToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(interactionToken);

        var route = RouteKey.Get("/webhooks/{application_id}/{interaction_token}/messages/@original",
            applicationId.ToString());

        using var response = await SendAsync(route, $"{InteractionPath(applicationId, interactionToken)}/messages/@original",
            authorize: false, cancellationToken: cancellationToken);

        return await ReadAsync<DiscordMessage>(response, route, cancellationToken);
    }

    public async Task<DiscordMessage> EditOriginalInteractionResponseAsync(Snowflake applicationId,
        string interactionToken, InteractionMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(interactionToken);

        var route = RouteKey.Patch("/webhooks/{application_id}/{interaction_token}/messages/@original",
            applicationId.ToString());

        var payload = InteractionMessagePayload.From(request);

        using var response = await SendAsync(route,
            $"{InteractionPath(applicationId, interactionToken)}/messages/@original",
            Body(route, payload, request.Files, request.Components, nameof(request)), authorize: false,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordMessage>(response, route, cancellationToken);
    }

    public async Task DeleteOriginalInteractionResponseAsync(Snowflake applicationId, string interactionToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(interactionToken);

        var route = RouteKey.Delete("/webhooks/{application_id}/{interaction_token}/messages/@original",
            applicationId.ToString());

        using var response = await SendAsync(route,
            $"{InteractionPath(applicationId, interactionToken)}/messages/@original", authorize: false,
            cancellationToken: cancellationToken);
    }

    public async Task<DiscordMessage> CreateFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        InteractionMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(interactionToken);

        var route = RouteKey.Post("/webhooks/{application_id}/{interaction_token}", applicationId.ToString());
        var payload = InteractionMessagePayload.From(request);

        using var response = await SendAsync(route, $"{InteractionPath(applicationId, interactionToken)}?wait=true",
            Body(route, payload, request.Files, request.Components, nameof(request)), authorize: false,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordMessage>(response, route, cancellationToken);
    }

    public async Task<DiscordMessage> EditFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        Snowflake messageId, InteractionMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(interactionToken);

        var route = RouteKey.Patch("/webhooks/{application_id}/{interaction_token}/messages/{message_id}",
            applicationId.ToString());

        var payload = InteractionMessagePayload.From(request);

        using var response = await SendAsync(route,
            $"{InteractionPath(applicationId, interactionToken)}/messages/{messageId}",
            Body(route, payload, request.Files, request.Components, nameof(request)), authorize: false,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordMessage>(response, route, cancellationToken);
    }

    public async Task DeleteFollowupMessageAsync(Snowflake applicationId, string interactionToken,
        Snowflake messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(interactionToken);

        var route = RouteKey.Delete("/webhooks/{application_id}/{interaction_token}/messages/{message_id}",
            applicationId.ToString());

        using var response = await SendAsync(route,
            $"{InteractionPath(applicationId, interactionToken)}/messages/{messageId}", authorize: false,
            cancellationToken: cancellationToken);
    }

    private static string InteractionPath(Snowflake applicationId, string interactionToken) =>
        $"webhooks/{applicationId}/{Uri.EscapeDataString(interactionToken)}";

    public async Task<DiscordGuild> GetGuildAsync(Snowflake guildId, bool withCounts = false,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}", guildId.ToString());
        var path = withCounts ? $"guilds/{guildId}?with_counts=true" : $"guilds/{guildId}";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        return await ReadAsync<DiscordGuild>(response, route, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordChannel>> GetGuildChannelsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/channels", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/channels",
            cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordChannel>>(response, route, cancellationToken);
    }

    public async Task<DiscordMember> GetGuildMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/members/{user_id}", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/members/{userId}",
            cancellationToken: cancellationToken);

        return Attach(await ReadAsync<DiscordMember>(response, route, cancellationToken), guildId);
    }

    public async Task<IReadOnlyList<DiscordMember>> GetGuildMembersAsync(Snowflake guildId, MemberQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/members", guildId.ToString());
        var parameters = new List<string>(2);

        if (query?.Limit is { } limit)
            parameters.Add($"limit={limit}");

        if (query?.After is { } after)
            parameters.Add($"after={after}");

        var path = $"guilds/{guildId}/members";

        if (parameters.Count > 0)
            path += $"?{string.Join('&', parameters)}";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        return Attach(await ReadAsync<List<DiscordMember>>(response, route, cancellationToken), guildId);
    }

    public async Task<IReadOnlyList<DiscordMember>> SearchGuildMembersAsync(Snowflake guildId, string search,
        int limit = 1, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(search);

        var route = RouteKey.Get("/guilds/{guild_id}/members/search", guildId.ToString());
        var path = $"guilds/{guildId}/members/search?query={Uri.EscapeDataString(search)}&limit={limit}";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        return Attach(await ReadAsync<List<DiscordMember>>(response, route, cancellationToken), guildId);
    }

    public async Task<DiscordMember> ModifyGuildMemberAsync(Snowflake guildId, Snowflake userId,
        MemberModifyRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Patch("/guilds/{guild_id}/members/{user_id}", guildId.ToString());
        var payload = MemberModifyPayload.From(request);

        using var response = await SendAsync(route, $"guilds/{guildId}/members/{userId}",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return Attach(await ReadAsync<DiscordMember>(response, route, cancellationToken), guildId);
    }

    public async Task AddGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Put("/guilds/{guild_id}/members/{user_id}/roles/{role_id}", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/members/{userId}/roles/{roleId}",
            reason: reason, cancellationToken: cancellationToken);
    }

    public async Task RemoveGuildMemberRoleAsync(Snowflake guildId, Snowflake userId, Snowflake roleId,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/guilds/{guild_id}/members/{user_id}/roles/{role_id}", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/members/{userId}/roles/{roleId}",
            reason: reason, cancellationToken: cancellationToken);
    }

    public async Task RemoveGuildMemberAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/guilds/{guild_id}/members/{user_id}", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/members/{userId}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordBan>> GetGuildBansAsync(Snowflake guildId, BanQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/bans", guildId.ToString());
        var parameters = new List<string>(3);

        if (query?.Limit is { } limit)
            parameters.Add($"limit={limit}");

        if (query?.Before is { } before)
            parameters.Add($"before={before}");

        if (query?.After is { } after)
            parameters.Add($"after={after}");

        var path = $"guilds/{guildId}/bans";

        if (parameters.Count > 0)
            path += $"?{string.Join('&', parameters)}";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordBan>>(response, route, cancellationToken);
    }

    public async Task<DiscordBan?> GetGuildBanAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/bans/{user_id}", guildId.ToString());

        try
        {
            using var response = await SendAsync(route, $"guilds/{guildId}/bans/{userId}",
                cancellationToken: cancellationToken);

            return await ReadAsync<DiscordBan>(response, route, cancellationToken);
        }
        catch (DiscordRestException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task CreateGuildBanAsync(Snowflake guildId, Snowflake userId, BanCreateRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Put("/guilds/{guild_id}/bans/{user_id}", guildId.ToString());
        var payload = new BanCreatePayload(request?.DeleteMessageSeconds ?? 0);

        using var response = await SendAsync(route, $"guilds/{guildId}/bans/{userId}",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);
    }

    public async Task RemoveGuildBanAsync(Snowflake guildId, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/guilds/{guild_id}/bans/{user_id}", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/bans/{userId}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordRole>> GetGuildRolesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/roles", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/roles", cancellationToken: cancellationToken);

        return Attach(await ReadAsync<List<DiscordRole>>(response, route, cancellationToken), guildId);
    }

    public async Task<DiscordRole> CreateGuildRoleAsync(Snowflake guildId, RoleCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Post("/guilds/{guild_id}/roles", guildId.ToString());
        var payload = RoleCreatePayload.From(request);

        using var response = await SendAsync(route, $"guilds/{guildId}/roles",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return Attach(await ReadAsync<DiscordRole>(response, route, cancellationToken), guildId);
    }

    public async Task<DiscordRole> ModifyGuildRoleAsync(Snowflake guildId, Snowflake roleId,
        RoleModifyRequest request, string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Patch("/guilds/{guild_id}/roles/{role_id}", guildId.ToString());
        var payload = RoleModifyPayload.From(request);

        using var response = await SendAsync(route, $"guilds/{guildId}/roles/{roleId}",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return Attach(await ReadAsync<DiscordRole>(response, route, cancellationToken), guildId);
    }

    public async Task DeleteGuildRoleAsync(Snowflake guildId, Snowflake roleId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/guilds/{guild_id}/roles/{role_id}", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/roles/{roleId}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<DiscordMessage> GetMessagesAsync(Snowflake channelId, MessageQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Limit is <= 0)
            yield break;

        var route = RouteKey.Get("/channels/{channel_id}/messages", channelId.ToString());

        if (query.Around is { } anchor)
        {
            var size = Math.Min(MaxMessagePageSize, query.Limit ?? 50);
            var anchored = $"channels/{channelId}/messages?limit={size}&around={anchor}";

            using var response = await SendAsync(route, anchored, cancellationToken: cancellationToken);

            foreach (var message in await ReadAsync<List<DiscordMessage>>(response, route, cancellationToken))
                yield return message;

            yield break;
        }

        var ascending = query.After is not null;
        var remaining = query.Limit;
        var cursor = ascending ? query.After : query.Before;

        while (remaining is null or > 0)
        {
            var pageSize = Math.Min(MaxMessagePageSize, remaining ?? MaxMessagePageSize);
            var path = $"channels/{channelId}/messages?limit={pageSize}";

            if (cursor is { } value)
                path += ascending ? $"&after={value}" : $"&before={value}";

            IReadOnlyList<DiscordMessage> page;

            using (var response = await SendAsync(route, path, cancellationToken: cancellationToken))
                page = await ReadAsync<List<DiscordMessage>>(response, route, cancellationToken);

            if (page.Count == 0)
                yield break;

            foreach (var message in page)
            {
                yield return message;

                if (remaining is not { } left)
                    continue;

                remaining = left - 1;

                if (remaining == 0)
                    yield break;
            }

            if (page.Count < pageSize)
                yield break;

            cursor = ascending ? page[0].Id : page[^1].Id;
        }
    }

    public async Task BulkDeleteMessagesAsync(Snowflake channelId, IReadOnlyList<Snowflake> messageIds,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);

        if (messageIds.Count == 0)
            return;

        if (messageIds.Count == 1)
        {
            await DeleteMessageAsync(channelId, messageIds[0], reason, cancellationToken);

            return;
        }

        if (messageIds.Count > 100)
            throw new ArgumentException("Discord accepts at most 100 messages per bulk delete.", nameof(messageIds));

        var route = RouteKey.Post("/channels/{channel_id}/messages/bulk-delete", channelId.ToString());
        var payload = new BulkDeletePayload(messageIds.Select(id => id.ToString()).ToArray());

        using var response = await SendAsync(route, $"channels/{channelId}/messages/bulk-delete",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);
    }

    public async Task<DiscordMessage> CrosspostMessageAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Post("/channels/{channel_id}/messages/{message_id}/crosspost", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/messages/{messageId}/crosspost",
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordMessage>(response, route, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordMessage>> GetPinnedMessagesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/channels/{channel_id}/pins", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/pins",
            cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordMessage>>(response, route, cancellationToken);
    }

    public async Task PinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Put("/channels/{channel_id}/pins/{message_id}", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/pins/{messageId}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async Task UnpinMessageAsync(Snowflake channelId, Snowflake messageId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/channels/{channel_id}/pins/{message_id}", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/pins/{messageId}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async Task TriggerTypingAsync(Snowflake channelId, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Post("/channels/{channel_id}/typing", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/typing",
            () => new StringContent(string.Empty), cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordUser>> GetReactionsAsync(Snowflake channelId, Snowflake messageId,
        DiscordEmoji emoji, ReactionQuery? query = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var route = RouteKey.Get("/channels/{channel_id}/messages/{message_id}/reactions/{emoji}",
            channelId.ToString());

        var path = $"channels/{channelId}/messages/{messageId}/reactions/{emoji.ToReactionPath()}";
        var parameters = new List<string>(2);

        if (query?.Limit is { } limit)
            parameters.Add($"limit={limit}");

        if (query?.After is { } after)
            parameters.Add($"after={after}");

        if (parameters.Count > 0)
            path += $"?{string.Join('&', parameters)}";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordUser>>(response, route, cancellationToken);
    }

    public async Task DeleteUserReactionAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        Snowflake userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var route = RouteKey.Delete("/channels/{channel_id}/messages/{message_id}/reactions/{emoji}/{user_id}",
            channelId.ToString());

        using var response = await SendAsync(route,
            $"channels/{channelId}/messages/{messageId}/reactions/{emoji.ToReactionPath()}/{userId}",
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAllReactionsAsync(Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/channels/{channel_id}/messages/{message_id}/reactions", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/messages/{messageId}/reactions",
            cancellationToken: cancellationToken);
    }

    public async Task DeleteEmojiReactionsAsync(Snowflake channelId, Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var route = RouteKey.Delete("/channels/{channel_id}/messages/{message_id}/reactions/{emoji}",
            channelId.ToString());

        using var response = await SendAsync(route,
            $"channels/{channelId}/messages/{messageId}/reactions/{emoji.ToReactionPath()}",
            cancellationToken: cancellationToken);
    }

    public async Task<DiscordUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/users/@me");

        using var response = await SendAsync(route, "users/@me", cancellationToken: cancellationToken);

        return await ReadAsync<DiscordUser>(response, route, cancellationToken);
    }

    public async Task<DiscordUser> GetUserAsync(Snowflake userId, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/users/{user_id}");

        using var response = await SendAsync(route, $"users/{userId}", cancellationToken: cancellationToken);

        return await ReadAsync<DiscordUser>(response, route, cancellationToken);
    }

    public async Task<DiscordChannel> CreateDirectMessageChannelAsync(Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Post("/users/@me/channels");
        var payload = new CreateDmPayload(userId.ToString());

        using var response = await SendAsync(route, "users/@me/channels",
            () => JsonContent.Create(payload, options: DiscordJson.Options), cancellationToken: cancellationToken);

        return await ReadAsync<DiscordChannel>(response, route, cancellationToken);
    }

    public async Task LeaveGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/users/@me/guilds/{guild_id}");

        using var response = await SendAsync(route, $"users/@me/guilds/{guildId}",
            cancellationToken: cancellationToken);
    }

    public async Task<GatewayBotInfo> GetGatewayBotAsync(CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/gateway/bot");

        using var response = await SendAsync(route, "gateway/bot", cancellationToken: cancellationToken);

        return await ReadAsync<GatewayBotInfo>(response, route, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordInvite>> GetChannelInvitesAsync(Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/channels/{channel_id}/invites", channelId.ToString());

        using var response = await SendAsync(route, $"channels/{channelId}/invites",
            cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordInvite>>(response, route, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordInvite>> GetGuildInvitesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/invites", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/invites",
            cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordInvite>>(response, route, cancellationToken);
    }

    public async Task<DiscordInvite> CreateChannelInviteAsync(Snowflake channelId,
        InviteCreateRequest? request = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Post("/channels/{channel_id}/invites", channelId.ToString());
        var payload = InviteCreatePayload.From(request ?? new InviteCreateRequest());

        using var response = await SendAsync(route, $"channels/{channelId}/invites",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordInvite>(response, route, cancellationToken);
    }

    public async Task<DiscordInvite> GetInviteAsync(string code, bool withCounts = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var route = RouteKey.Get("/invites/{code}");
        var path = $"invites/{Uri.EscapeDataString(code)}";

        if (withCounts)
            path += "?with_counts=true";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        return await ReadAsync<DiscordInvite>(response, route, cancellationToken);
    }

    public async Task DeleteInviteAsync(string code, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var route = RouteKey.Delete("/invites/{code}");

        using var response = await SendAsync(route, $"invites/{Uri.EscapeDataString(code)}", reason: reason,
            cancellationToken: cancellationToken);
    }

    public async Task<DiscordAuditLog> GetGuildAuditLogAsync(Snowflake guildId, AuditLogQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/audit-logs", guildId.ToString());
        var parameters = new List<string>(5);

        if (query?.UserId is { } userId)
            parameters.Add($"user_id={userId}");

        if (query?.Action is { } action)
            parameters.Add($"action_type={(int)action}");

        if (query?.Before is { } before)
            parameters.Add($"before={before}");

        if (query?.After is { } after)
            parameters.Add($"after={after}");

        if (query?.Limit is { } limit)
            parameters.Add($"limit={limit}");

        var path = $"guilds/{guildId}/audit-logs";

        if (parameters.Count > 0)
            path += $"?{string.Join('&', parameters)}";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        var log = await ReadAsync<DiscordAuditLog>(response, route, cancellationToken);

        return log with { Entries = log.Entries.Select(entry => entry.In(guildId)).ToArray() };
    }

    public async Task<DiscordGuild> ModifyGuildAsync(Snowflake guildId, GuildModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = RouteKey.Patch("/guilds/{guild_id}", guildId.ToString());
        var payload = GuildModifyPayload.From(request);

        using var response = await SendAsync(route, $"guilds/{guildId}",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordGuild>(response, route, cancellationToken);
    }

    public async Task<int> GetGuildPruneCountAsync(Snowflake guildId, PruneRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var prune = request ?? new PruneRequest();
        var route = RouteKey.Get("/guilds/{guild_id}/prune", guildId.ToString());
        var path = $"guilds/{guildId}/prune?days={prune.Days}";

        if (prune.IncludeRoles.Count > 0)
            path += $"&include_roles={string.Join(',', prune.IncludeRoles)}";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        return (await ReadAsync<JsonElement>(response, route, cancellationToken)).IntegerOrNull("pruned") ?? 0;
    }

    public async Task<int?> BeginGuildPruneAsync(Snowflake guildId, PruneRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Post("/guilds/{guild_id}/prune", guildId.ToString());
        var payload = PrunePayload.From(request ?? new PruneRequest());

        using var response = await SendAsync(route, $"guilds/{guildId}/prune",
            () => JsonContent.Create(payload, options: DiscordJson.Options), reason,
            cancellationToken: cancellationToken);

        return (await ReadAsync<JsonElement>(response, route, cancellationToken)).IntegerOrNull("pruned");
    }

    public async Task<ThreadListing> GetActiveThreadsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/guilds/{guild_id}/threads/active", guildId.ToString());

        using var response = await SendAsync(route, $"guilds/{guildId}/threads/active",
            cancellationToken: cancellationToken);

        return await ReadAsync<ThreadListing>(response, route, cancellationToken);
    }

    public Task<ThreadListing> GetPublicArchivedThreadsAsync(Snowflake channelId, ArchivedThreadQuery? query = null,
        CancellationToken cancellationToken = default) =>
        ArchivedThreadsAsync("/channels/{channel_id}/threads/archived/public",
            $"channels/{channelId}/threads/archived/public", channelId, query, cancellationToken);

    public Task<ThreadListing> GetPrivateArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default) =>
        ArchivedThreadsAsync("/channels/{channel_id}/threads/archived/private",
            $"channels/{channelId}/threads/archived/private", channelId, query, cancellationToken);

    public Task<ThreadListing> GetJoinedPrivateArchivedThreadsAsync(Snowflake channelId,
        ArchivedThreadQuery? query = null, CancellationToken cancellationToken = default) =>
        ArchivedThreadsAsync("/channels/{channel_id}/users/@me/threads/archived/private",
            $"channels/{channelId}/users/@me/threads/archived/private", channelId, query, cancellationToken);

    public async Task JoinThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Put("/channels/{channel_id}/thread-members/@me", threadId.ToString());

        using var response = await SendAsync(route, $"channels/{threadId}/thread-members/@me",
            cancellationToken: cancellationToken);
    }

    public async Task LeaveThreadAsync(Snowflake threadId, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/channels/{channel_id}/thread-members/@me", threadId.ToString());

        using var response = await SendAsync(route, $"channels/{threadId}/thread-members/@me",
            cancellationToken: cancellationToken);
    }

    public async Task AddThreadMemberAsync(Snowflake threadId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Put("/channels/{channel_id}/thread-members/{user_id}", threadId.ToString());

        using var response = await SendAsync(route, $"channels/{threadId}/thread-members/{userId}",
            cancellationToken: cancellationToken);
    }

    public async Task RemoveThreadMemberAsync(Snowflake threadId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Delete("/channels/{channel_id}/thread-members/{user_id}", threadId.ToString());

        using var response = await SendAsync(route, $"channels/{threadId}/thread-members/{userId}",
            cancellationToken: cancellationToken);
    }

    public async Task<DiscordThreadMember> GetThreadMemberAsync(Snowflake threadId, Snowflake userId,
        bool withMember = false, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/channels/{channel_id}/thread-members/{user_id}", threadId.ToString());
        var path = $"channels/{threadId}/thread-members/{userId}";

        if (withMember)
            path += "?with_member=true";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        return (await ReadAsync<DiscordThreadMember>(response, route, cancellationToken)).On(threadId);
    }

    public async Task<IReadOnlyList<DiscordThreadMember>> GetThreadMembersAsync(Snowflake threadId,
        bool withMember = false, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/channels/{channel_id}/thread-members", threadId.ToString());
        var path = $"channels/{threadId}/thread-members";

        if (withMember)
            path += "?with_member=true";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        var members = await ReadAsync<List<DiscordThreadMember>>(response, route, cancellationToken);

        return members.Select(member => member.On(threadId)).ToArray();
    }

    public async Task<IReadOnlyList<DiscordCommandPermissions>> GetGuildCommandPermissionsAsync(
        Snowflake applicationId, Snowflake guildId, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get("/applications/{application_id}/guilds/{guild_id}/commands/permissions",
            applicationId.ToString());

        using var response = await SendAsync(route,
            $"applications/{applicationId}/guilds/{guildId}/commands/permissions",
            cancellationToken: cancellationToken);

        return await ReadAsync<List<DiscordCommandPermissions>>(response, route, cancellationToken);
    }

    public async Task<DiscordCommandPermissions> GetCommandPermissionsAsync(Snowflake applicationId,
        Snowflake guildId, Snowflake commandId, CancellationToken cancellationToken = default)
    {
        var route = RouteKey.Get(
            "/applications/{application_id}/guilds/{guild_id}/commands/{command_id}/permissions",
            applicationId.ToString());

        using var response = await SendAsync(route,
            $"applications/{applicationId}/guilds/{guildId}/commands/{commandId}/permissions",
            cancellationToken: cancellationToken);

        return await ReadAsync<DiscordCommandPermissions>(response, route, cancellationToken);
    }

    private async Task<ThreadListing> ArchivedThreadsAsync(string template, string path, Snowflake channelId,
        ArchivedThreadQuery? query, CancellationToken cancellationToken)
    {
        var route = RouteKey.Get(template, channelId.ToString());
        var parameters = new List<string>(2);

        if (query?.Before is { } before)
            parameters.Add($"before={Uri.EscapeDataString(before.ToString("O"))}");
        else if (query?.BeforeId is { } beforeId)
            parameters.Add($"before={beforeId}");

        if (query?.Limit is { } limit)
            parameters.Add($"limit={limit}");

        if (parameters.Count > 0)
            path += $"?{string.Join('&', parameters)}";

        using var response = await SendAsync(route, path, cancellationToken: cancellationToken);

        return await ReadAsync<ThreadListing>(response, route, cancellationToken);
    }

    private static DiscordMember Attach(DiscordMember member, Snowflake guildId) =>
        member.GuildId is null ? member.In(guildId) : member;

    private static IReadOnlyList<DiscordMember> Attach(List<DiscordMember> members, Snowflake guildId)
    {
        for (var index = 0; index < members.Count; index++)
            members[index] = Attach(members[index], guildId);

        return members;
    }

    private static DiscordRole Attach(DiscordRole role, Snowflake guildId) =>
        role.GuildId is null ? role with { GuildId = guildId } : role;

    private static IReadOnlyList<DiscordRole> Attach(List<DiscordRole> roles, Snowflake guildId)
    {
        for (var index = 0; index < roles.Count; index++)
            roles[index] = Attach(roles[index], guildId);

        return roles;
    }

    private static RouteKey CommandRoute(HttpMethod method, Snowflake applicationId, Snowflake? guildId,
        bool targetsOne)
    {
        var template = guildId is null
            ? "/applications/{application_id}/commands"
            : "/applications/{application_id}/guilds/{guild_id}/commands";

        if (targetsOne)
            template += "/{command_id}";

        return new RouteKey(method, template, applicationId.ToString());
    }

    private static string CommandPath(Snowflake applicationId, Snowflake? guildId) =>
        guildId is { } guild
            ? $"applications/{applicationId}/guilds/{guild}/commands"
            : $"applications/{applicationId}/commands";

    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;

        if (_ownsHttpClient)
            _http.Dispose();

        return ValueTask.CompletedTask;
    }

    private Func<HttpContent> Body<TPayload>(RouteKey route, TPayload payload, IReadOnlyList<DiscordFile>? files,
        IReadOnlyList<DiscordComponent>? components, string field)
    {
        ComponentLimit.Rows(components, field);

        if (files is not { Count: > 0 })
            return () => JsonContent.Create(payload, options: DiscordJson.Options);

        AttachmentUpload.Validate(files, field);
        ReportUpload(route, files);

        return () => AttachmentUpload.Build(payload, files);
    }

    private void ReportUpload(RouteKey route, IReadOnlyList<DiscordFile> files)
    {
        var bytes = AttachmentUpload.TotalBytes(files);

        _logger.LogDebug($"{route} uploads {files.Count} file(s) totalling {bytes} bytes");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new AttachmentsUploaded(route.Method.Method, route.Template, files.Count, bytes));
    }

    private async Task<HttpResponseMessage> SendAsync(RouteKey route, string path,
        Func<HttpContent>? content = null, string? reason = null, bool authorize = true,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        for (var attempt = 1;; attempt++)
        {
            using var lease = await _limiter.AcquireAsync(route, cancellationToken);
            using var request = new HttpRequestMessage(route.Method, path);

            if (authorize)
                request.Headers.TryAddWithoutValidation("Authorization", _authorization);

            if (reason is not null)
                request.Headers.TryAddWithoutValidation("X-Audit-Log-Reason", Uri.EscapeDataString(reason));

            if (content is not null)
                request.Content = content();

            var start = Stopwatch.GetTimestamp();
            HttpResponseMessage response;

            try
            {
                response = await _http.SendAsync(request, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                ReportFailed(route, exception.Message, Stopwatch.GetElapsedTime(start), attempt);

                if (attempt >= _options.MaxAttempts)
                    throw new DiscordRestException($"{route} failed after {attempt} attempts: {exception.Message}",
                        HttpStatusCode.ServiceUnavailable, null, route.ToString(), attempt, exception);

                await Task.Delay(RetryDelay(attempt), _time, cancellationToken);
                continue;
            }

            lease.Complete(response);
            ReportCompleted(route, (int)response.StatusCode, Stopwatch.GetElapsedTime(start), attempt);

            if (response.IsSuccessStatusCode)
                return response;

            if (attempt < _options.MaxAttempts && IsTransient(response.StatusCode))
            {
                var throttled = response.StatusCode is HttpStatusCode.TooManyRequests;
                response.Dispose();

                if (!throttled)
                    await Task.Delay(RetryDelay(attempt), _time, cancellationToken);

                continue;
            }

            try
            {
                throw await DiscordRestException.FromResponseAsync(route, response, attempt, cancellationToken);
            }
            finally
            {
                response.Dispose();
            }
        }
    }

    private async Task<T> ReadAsync<T>(HttpResponseMessage response, RouteKey route,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        try
        {
            return EntityBinder.Bind(
                       await JsonSerializer.DeserializeAsync<T>(stream, DiscordJson.Options, cancellationToken),
                       Context)
                   ?? throw new DiscordRestException($"{route} returned an empty body.", response.StatusCode, null,
                       route.ToString());
        }
        catch (JsonException exception)
        {
            _logger.LogError($"Could not deserialize the response for {route}", exception);

            throw new DiscordRestException($"{route} returned a body that could not be deserialized.",
                response.StatusCode, null, route.ToString(), 1, exception);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private static TimeSpan RetryDelay(int attempt) =>
        BaseRetryDelay * Math.Pow(2, attempt - 1) * (0.8 + Random.Shared.NextDouble() * 0.4);

    private void ReportCompleted(RouteKey route, int statusCode, TimeSpan duration, int attempt)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug($"{route} responded {statusCode} in {duration.TotalMilliseconds:F0}ms (attempt {attempt})");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new RestRequestCompleted(route.Method.Method, route.Template, statusCode, duration,
                attempt));
    }

    private void ReportFailed(RouteKey route, string reason, TimeSpan duration, int attempt)
    {
        _logger.LogWarning($"{route} failed after {duration.TotalMilliseconds:F0}ms (attempt {attempt}): {reason}");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new RestRequestFailed(route.Method.Method, route.Template, reason, duration, attempt));
    }
}

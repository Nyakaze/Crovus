using Crovus.Client;
using Crovus.Logs;
using Crovus.Models;

namespace Crovus.Cache;

public sealed class DiscordCache : IDiscordCache, IContextAware
{
    private const string LogCategory = "Cache";

    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private readonly ICacheStore<Snowflake, DiscordChannel> _channels;
    private readonly ICacheStore<Snowflake, DiscordMessage> _messages;
    private readonly ICacheStore<Snowflake, DiscordUser> _users;
    private readonly ICacheStore<Snowflake, DiscordWebhook> _webhooks;
    private readonly ICacheStore<Snowflake, IReadOnlyList<DiscordWebhook>> _channelWebhooks;
    private readonly ICacheStore<ReactionKey, IReadOnlySet<Snowflake>> _reactions;
    private readonly ICacheStore<Snowflake, IReadOnlySet<string>> _reactionIndex;
    private readonly ICacheStore<Snowflake, DiscordGuild> _guilds;
    private readonly ICacheStore<MemberKey, DiscordMember> _members;
    private readonly ICacheStore<Snowflake, IReadOnlyList<DiscordRole>> _guildRoles;

    private long _hits;
    private long _misses;
    private long _writes;
    private long _invalidations;

    public DiscordCache(CacheOptions? options = null, ICacheStoreFactory? storeFactory = null, ILogger? logger = null,
        ITelemetry? telemetry = null, TimeProvider? timeProvider = null)
    {
        var settings = options ?? new CacheOptions();
        var factory = storeFactory ?? new MemoryCacheStoreFactory(logger, telemetry, timeProvider);

        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;

        _channels = factory.Create<Snowflake, DiscordChannel>("channels", settings.Channels);
        _messages = factory.Create<Snowflake, DiscordMessage>("messages", settings.Messages);
        _users = factory.Create<Snowflake, DiscordUser>("users", settings.Users);
        _webhooks = factory.Create<Snowflake, DiscordWebhook>("webhooks", settings.Webhooks);
        _channelWebhooks = factory.Create<Snowflake, IReadOnlyList<DiscordWebhook>>("channel-webhooks",
            settings.ChannelWebhooks);
        _reactions = factory.Create<ReactionKey, IReadOnlySet<Snowflake>>("reactions", settings.Reactions);
        _reactionIndex = factory.Create<Snowflake, IReadOnlySet<string>>("reaction-index", settings.Reactions);
        _guilds = factory.Create<Snowflake, DiscordGuild>("guilds", settings.Guilds);
        _members = factory.Create<MemberKey, DiscordMember>("members", settings.Members);
        _guildRoles = factory.Create<Snowflake, IReadOnlyList<DiscordRole>>("guild-roles", settings.GuildRoles);
    }

    public DiscordCache(CacheOptions options, DiagnosticsHub diagnostics, ICacheStoreFactory? storeFactory = null,
        TimeProvider? timeProvider = null)
        : this(options, storeFactory, diagnostics, diagnostics, timeProvider)
    {
    }

    public ICrovusContext? Context { get; set; }

    public CacheStatistics Statistics => new(
        Interlocked.Read(ref _hits),
        Interlocked.Read(ref _misses),
        Interlocked.Read(ref _writes),
        Interlocked.Read(ref _invalidations));

    public ValueTask<DiscordChannel?> GetChannelAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        LookupAsync(_channels, channelId, "channels", channelId.ToString(), cancellationToken);

    public ValueTask SetChannelAsync(DiscordChannel channel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(channel);

        return StoreAsync(_channels, channel.Id, channel, "channels", cancellationToken);
    }

    public async ValueTask RemoveChannelAsync(Snowflake channelId, CancellationToken cancellationToken = default)
    {
        await InvalidateAsync(_channels, channelId, "channels", cancellationToken);
        await InvalidateAsync(_channelWebhooks, channelId, "channel-webhooks", cancellationToken);
    }

    public ValueTask<DiscordMessage?> GetMessageAsync(Snowflake messageId,
        CancellationToken cancellationToken = default) =>
        LookupAsync(_messages, messageId, "messages", messageId.ToString(), cancellationToken);

    public async ValueTask SetMessageAsync(DiscordMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await StoreAsync(_messages, message.Id, message, "messages", cancellationToken);
        await SetUserAsync(message.Author, cancellationToken);
    }

    public async ValueTask RemoveMessageAsync(Snowflake messageId, CancellationToken cancellationToken = default)
    {
        await InvalidateAsync(_messages, messageId, "messages", cancellationToken);
        await ClearReactionsAsync(messageId, cancellationToken);
    }

    public ValueTask<DiscordUser?> GetUserAsync(Snowflake userId, CancellationToken cancellationToken = default) =>
        LookupAsync(_users, userId, "users", userId.ToString(), cancellationToken);

    public ValueTask SetUserAsync(DiscordUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        return StoreAsync(_users, user.Id, user, "users", cancellationToken);
    }

    public ValueTask<DiscordWebhook?> GetWebhookAsync(Snowflake webhookId,
        CancellationToken cancellationToken = default) =>
        LookupAsync(_webhooks, webhookId, "webhooks", webhookId.ToString(), cancellationToken);

    public async ValueTask SetWebhookAsync(DiscordWebhook webhook, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhook);

        await StoreAsync(_webhooks, webhook.Id, webhook, "webhooks", cancellationToken);

        if (webhook.Creator is { } creator)
            await SetUserAsync(creator, cancellationToken);
    }

    public async ValueTask RemoveWebhookAsync(Snowflake webhookId, CancellationToken cancellationToken = default)
    {
        if (await _webhooks.GetAsync(webhookId, cancellationToken) is { } webhook)
            await InvalidateAsync(_channelWebhooks, webhook.ChannelId, "channel-webhooks", cancellationToken);

        await InvalidateAsync(_webhooks, webhookId, "webhooks", cancellationToken);
    }

    public ValueTask<IReadOnlyList<DiscordWebhook>?> GetChannelWebhooksAsync(Snowflake channelId,
        CancellationToken cancellationToken = default) =>
        LookupAsync(_channelWebhooks, channelId, "channel-webhooks", channelId.ToString(), cancellationToken);

    public async ValueTask SetChannelWebhooksAsync(Snowflake channelId, IReadOnlyList<DiscordWebhook> webhooks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhooks);

        await StoreAsync(_channelWebhooks, channelId, webhooks, "channel-webhooks", cancellationToken);

        foreach (var webhook in webhooks)
            await SetWebhookAsync(webhook, cancellationToken);
    }

    public ValueTask RemoveChannelWebhooksAsync(Snowflake channelId, CancellationToken cancellationToken = default) =>
        InvalidateAsync(_channelWebhooks, channelId, "channel-webhooks", cancellationToken);

    public ValueTask<IReadOnlySet<Snowflake>?> GetReactionsAsync(Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var key = ReactionKey.For(messageId, emoji);

        return LookupAsync(_reactions, key, "reactions", key.ToString(), cancellationToken);
    }

    public async ValueTask AddReactionAsync(Snowflake messageId, DiscordEmoji emoji, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var key = ReactionKey.For(messageId, emoji);
        var current = await _reactions.GetAsync(key, cancellationToken);
        var next = current is null ? [] : new HashSet<Snowflake>(current);

        if (!next.Add(userId) && current is not null)
            return;

        await _reactions.SetAsync(key, next, cancellationToken);
        await TrackReactionKeyAsync(messageId, key.Emoji, cancellationToken);

        Interlocked.Increment(ref _writes);
    }

    public async ValueTask RemoveReactionAsync(Snowflake messageId, DiscordEmoji emoji, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var key = ReactionKey.For(messageId, emoji);

        if (await _reactions.GetAsync(key, cancellationToken) is not { } current || !current.Contains(userId))
            return;

        var next = new HashSet<Snowflake>(current);
        next.Remove(userId);

        if (next.Count == 0)
        {
            await _reactions.RemoveAsync(key, cancellationToken);
            Interlocked.Increment(ref _invalidations);
            return;
        }

        await _reactions.SetAsync(key, next, cancellationToken);
        Interlocked.Increment(ref _writes);
    }

    public async ValueTask RemoveReactionsAsync(Snowflake messageId, DiscordEmoji emoji,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        await InvalidateAsync(_reactions, ReactionKey.For(messageId, emoji), "reactions", cancellationToken);
    }

    public async ValueTask ClearReactionsAsync(Snowflake messageId, CancellationToken cancellationToken = default)
    {
        if (await _reactionIndex.GetAsync(messageId, cancellationToken) is not { } emojis)
            return;

        foreach (var emoji in emojis)
            await _reactions.RemoveAsync(new ReactionKey(messageId, emoji), cancellationToken);

        await _reactionIndex.RemoveAsync(messageId, cancellationToken);

        Interlocked.Add(ref _invalidations, emojis.Count);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace($"Cleared {emojis.Count} reaction entries for message {messageId}");
    }

    public ValueTask<DiscordGuild?> GetGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default) =>
        LookupAsync(_guilds, guildId, "guilds", guildId.ToString(), cancellationToken);

    public async ValueTask SetGuildAsync(DiscordGuild guild, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(guild);

        await StoreAsync(_guilds, guild.Id, guild, "guilds", cancellationToken);

        if (guild.Roles.Count > 0)
            await StoreAsync(_guildRoles, guild.Id, guild.Roles, "guild-roles", cancellationToken);
    }

    public async ValueTask RemoveGuildAsync(Snowflake guildId, CancellationToken cancellationToken = default)
    {
        await InvalidateAsync(_guilds, guildId, "guilds", cancellationToken);
        await InvalidateAsync(_guildRoles, guildId, "guild-roles", cancellationToken);
    }

    public ValueTask<DiscordMember?> GetMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var key = new MemberKey(guildId, userId);

        return LookupAsync(_members, key, "members", key.ToString(), cancellationToken);
    }

    public async ValueTask SetMemberAsync(Snowflake guildId, DiscordMember member,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(member);

        await StoreAsync(_members, new MemberKey(guildId, member.User.Id), member, "members", cancellationToken);
        await SetUserAsync(member.User, cancellationToken);
    }

    public ValueTask RemoveMemberAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        InvalidateAsync(_members, new MemberKey(guildId, userId), "members", cancellationToken);

    public ValueTask<IReadOnlyList<DiscordRole>?> GetGuildRolesAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        LookupAsync(_guildRoles, guildId, "guild-roles", guildId.ToString(), cancellationToken);

    public async ValueTask SetGuildRolesAsync(Snowflake guildId, IReadOnlyList<DiscordRole> roles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roles);

        await StoreAsync(_guildRoles, guildId, roles, "guild-roles", cancellationToken);

        if (await _guilds.GetAsync(guildId, cancellationToken) is { } guild)
            await StoreAsync(_guilds, guildId, guild with { Roles = roles }, "guilds", cancellationToken);
    }

    public ValueTask RemoveGuildRolesAsync(Snowflake guildId, CancellationToken cancellationToken = default) =>
        InvalidateAsync(_guildRoles, guildId, "guild-roles", cancellationToken);

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        await _channels.ClearAsync(cancellationToken);
        await _messages.ClearAsync(cancellationToken);
        await _users.ClearAsync(cancellationToken);
        await _webhooks.ClearAsync(cancellationToken);
        await _channelWebhooks.ClearAsync(cancellationToken);
        await _reactions.ClearAsync(cancellationToken);
        await _reactionIndex.ClearAsync(cancellationToken);
        await _guilds.ClearAsync(cancellationToken);
        await _members.ClearAsync(cancellationToken);
        await _guildRoles.ClearAsync(cancellationToken);

        _logger.LogInformation("Cache cleared");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new CacheCleared());
    }

    private async ValueTask TrackReactionKeyAsync(Snowflake messageId, string emoji,
        CancellationToken cancellationToken)
    {
        var current = await _reactionIndex.GetAsync(messageId, cancellationToken);

        if (current is not null && current.Contains(emoji))
            return;

        var next = current is null ? [] : new HashSet<string>(current);
        next.Add(emoji);

        await _reactionIndex.SetAsync(messageId, next, cancellationToken);
    }

    private async ValueTask<TValue?> LookupAsync<TKey, TValue>(ICacheStore<TKey, TValue> store, TKey key,
        string entity, string display, CancellationToken cancellationToken) where TKey : notnull
    {
        var value = await store.GetAsync(key, cancellationToken);

        if (value is not null)
        {
            Interlocked.Increment(ref _hits);

            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace($"Cache hit on {entity} for {display}");

            if (_telemetry.HasSubscribers)
                _telemetry.Emit(new CacheHit(entity, display));

            return value;
        }

        Interlocked.Increment(ref _misses);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace($"Cache miss on {entity} for {display}");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new CacheMiss(entity, display));

        return default;
    }

    private async ValueTask StoreAsync<TKey, TValue>(ICacheStore<TKey, TValue> store, TKey key, TValue value,
        string entity, CancellationToken cancellationToken) where TKey : notnull
    {
        await store.SetAsync(key, EntityBinder.Bind(value, Context), cancellationToken);

        Interlocked.Increment(ref _writes);

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new CacheEntryWritten(entity));
    }

    private async ValueTask InvalidateAsync<TKey, TValue>(ICacheStore<TKey, TValue> store, TKey key, string entity,
        CancellationToken cancellationToken) where TKey : notnull
    {
        if (!await store.RemoveAsync(key, cancellationToken))
            return;

        Interlocked.Increment(ref _invalidations);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace($"Invalidated {entity} entry {key}");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new CacheEntryInvalidated(entity, key.ToString() ?? string.Empty));
    }
}

using System.Runtime.CompilerServices;
using System.Text.Json;
using Crovus.Gateway;
using Crovus.Json;
using Crovus.Logs;
using Crovus.Models;

namespace Crovus.Cache;

public sealed class CachedDiscordGateway : IDiscordGateway
{
    private const string LogCategory = "Cache.Gateway";

    private readonly IDiscordGateway _inner;
    private readonly IDiscordCache _cache;
    private readonly ILogger _logger;

    public CachedDiscordGateway(IDiscordGateway inner, IDiscordCache cache, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);

        _inner = inner;
        _cache = cache;
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
    }

    public CachedDiscordGateway(IDiscordGateway inner, IDiscordCache cache, DiagnosticsHub diagnostics)
        : this(inner, cache, (ILogger)diagnostics)
    {
    }

    public GatewayState State => _inner.State;

    public string? SessionId => _inner.SessionId;

    public int? LastSequence => _inner.LastSequence;

    public TimeSpan? Latency => _inner.Latency;

    public Task ConnectAsync(CancellationToken cancellationToken = default) =>
        _inner.ConnectAsync(cancellationToken);

    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        _inner.DisconnectAsync(cancellationToken);

    public ValueTask SendAsync(GatewayOpcode opcode, object? payload, CancellationToken cancellationToken = default) =>
        _inner.SendAsync(opcode, payload, cancellationToken);

    public ValueTask UpdatePresenceAsync(PresenceUpdate presence, CancellationToken cancellationToken = default) =>
        _inner.UpdatePresenceAsync(presence, cancellationToken);

    public ValueTask RequestGuildMembersAsync(GuildMembersRequest request,
        CancellationToken cancellationToken = default) =>
        _inner.RequestGuildMembersAsync(request, cancellationToken);

    public async IAsyncEnumerable<GatewayEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var gatewayEvent in _inner.ReadEventsAsync(cancellationToken))
        {
            if (gatewayEvent is { IsDispatch: true, Name: { } name, Data: { } data })
                await ApplyAsync(name, data, cancellationToken);

            yield return gatewayEvent;
        }
    }

    public ValueTask DisposeAsync() => _inner.DisposeAsync();

    private async ValueTask ApplyAsync(string name, JsonElement data, CancellationToken cancellationToken)
    {
        try
        {
            switch (name)
            {
                case "READY":
                    if (data.Deserialize<DiscordUser>("user", DiscordJson.Options) is { } self)
                        await _cache.SetUserAsync(self, cancellationToken);
                    break;

                case "GUILD_CREATE":
                case "GUILD_UPDATE":
                    await CacheGuildAsync(data, cancellationToken);
                    break;

                case "GUILD_DELETE":
                    await _cache.RemoveGuildAsync(data.RequireSnowflake("id"), cancellationToken);
                    break;

                case "GUILD_MEMBER_ADD":
                case "GUILD_MEMBER_UPDATE":
                    await CacheMemberAsync(data, cancellationToken);
                    break;

                case "GUILD_MEMBER_REMOVE":
                    if (data.Property("user") is { } departed)
                        await _cache.RemoveMemberAsync(data.RequireSnowflake("guild_id"),
                            departed.RequireSnowflake("id"), cancellationToken);
                    break;

                case "GUILD_ROLE_CREATE":
                case "GUILD_ROLE_UPDATE":
                case "GUILD_ROLE_DELETE":
                    await _cache.RemoveGuildRolesAsync(data.RequireSnowflake("guild_id"), cancellationToken);
                    break;

                case "GUILD_BAN_ADD":
                    if (data.Property("user") is { } banned)
                        await _cache.RemoveMemberAsync(data.RequireSnowflake("guild_id"),
                            banned.RequireSnowflake("id"), cancellationToken);
                    break;

                case "CHANNEL_CREATE":
                case "CHANNEL_UPDATE":
                case "THREAD_CREATE":
                case "THREAD_UPDATE":
                    if (data.Deserialize<DiscordChannel>(DiscordJson.Options) is { } channel)
                        await _cache.SetChannelAsync(channel, cancellationToken);
                    break;

                case "CHANNEL_DELETE":
                case "THREAD_DELETE":
                    await _cache.RemoveChannelAsync(data.RequireSnowflake("id"), cancellationToken);
                    break;

                case "MESSAGE_CREATE":
                    if (data.Deserialize<DiscordMessage>(DiscordJson.Options) is { } created)
                        await _cache.SetMessageAsync(created, cancellationToken);
                    break;

                case "MESSAGE_UPDATE":
                    if (data.Property("author") is not null &&
                        data.Deserialize<DiscordMessage>(DiscordJson.Options) is { } updated)
                        await _cache.SetMessageAsync(updated, cancellationToken);
                    break;

                case "MESSAGE_DELETE":
                    await _cache.RemoveMessageAsync(data.RequireSnowflake("id"), cancellationToken);
                    break;

                case "MESSAGE_DELETE_BULK":
                    await RemoveBulkAsync(data, cancellationToken);
                    break;

                case "MESSAGE_REACTION_ADD":
                    await _cache.AddReactionAsync(data.RequireSnowflake("message_id"), ReadEmoji(data),
                        data.RequireSnowflake("user_id"), cancellationToken);
                    break;

                case "MESSAGE_REACTION_REMOVE":
                    await _cache.RemoveReactionAsync(data.RequireSnowflake("message_id"), ReadEmoji(data),
                        data.RequireSnowflake("user_id"), cancellationToken);
                    break;

                case "MESSAGE_REACTION_REMOVE_ALL":
                    await _cache.ClearReactionsAsync(data.RequireSnowflake("message_id"), cancellationToken);
                    break;

                case "MESSAGE_REACTION_REMOVE_EMOJI":
                    await _cache.RemoveReactionsAsync(data.RequireSnowflake("message_id"), ReadEmoji(data),
                        cancellationToken);
                    break;

                case "WEBHOOKS_UPDATE":
                    await _cache.RemoveChannelWebhooksAsync(data.RequireSnowflake("channel_id"), cancellationToken);
                    break;
            }
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or FormatException)
        {
            _logger.LogWarning($"Could not apply {name} to the cache", exception);
        }
    }

    private async ValueTask CacheGuildAsync(JsonElement data, CancellationToken cancellationToken)
    {
        if (data.Flag("unavailable"))
        {
            await _cache.RemoveGuildAsync(data.RequireSnowflake("id"), cancellationToken);
            return;
        }

        if (data.Deserialize<DiscordGuild>(DiscordJson.Options) is { } guild)
            await _cache.SetGuildAsync(guild, cancellationToken);

        await CacheGuildChannelsAsync(data, cancellationToken);
    }

    private async ValueTask CacheMemberAsync(JsonElement data, CancellationToken cancellationToken)
    {
        if (data.Property("user") is null || data.Deserialize<DiscordMember>(DiscordJson.Options) is not { } member)
            return;

        var guildId = data.RequireSnowflake("guild_id");

        await _cache.SetMemberAsync(guildId, member.GuildId is null ? member.In(guildId) : member, cancellationToken);
    }

    private async ValueTask CacheGuildChannelsAsync(JsonElement data, CancellationToken cancellationToken)
    {
        if (data.Property("channels") is not { ValueKind: JsonValueKind.Array } channels)
            return;

        foreach (var element in channels.EnumerateArray())
        {
            if (element.Deserialize<DiscordChannel>(DiscordJson.Options) is { } channel)
                await _cache.SetChannelAsync(channel, cancellationToken);
        }
    }

    private async ValueTask RemoveBulkAsync(JsonElement data, CancellationToken cancellationToken)
    {
        if (data.Property("ids") is not { ValueKind: JsonValueKind.Array } ids)
            return;

        foreach (var element in ids.EnumerateArray())
        {
            if (element.GetString() is { } raw && ulong.TryParse(raw, out var id))
                await _cache.RemoveMessageAsync(new Snowflake(id), cancellationToken);
        }
    }

    private static DiscordEmoji ReadEmoji(JsonElement data)
    {
        if (data.Property("emoji") is not { } emoji)
            throw new JsonException("The reaction payload has no emoji.");

        return new DiscordEmoji(
            emoji.StringOrNull("name") ?? string.Empty,
            emoji.SnowflakeOrNull("id"),
            emoji.Flag("animated"));
    }
}

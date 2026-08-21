using System.Text.Json;
using Crovus.Gateway;
using Crovus.Json;
using Crovus.Models;

namespace Crovus.Events;

public static class DiscordEventFactory
{
    public static DiscordEvent Create(GatewayEvent gatewayEvent)
    {
        ArgumentNullException.ThrowIfNull(gatewayEvent);

        if (gatewayEvent is not { IsDispatch: true, Name: { } name, Data: { } data })
            throw new InvalidOperationException("Only dispatch events carry a payload.");

        var decoded = Decode(name, data);

        return decoded with { Name = name, Sequence = gatewayEvent.Sequence, ReceivedAt = DateTimeOffset.UtcNow };
    }

    private static DiscordEvent Decode(string name, JsonElement data) => name switch
    {
        "READY" => new ReadyEvent(
            Require<DiscordUser>(data, "user", name),
            data.Property("application")?.SnowflakeOrNull("id"),
            data.RequireString("session_id"),
            data.StringOrNull("resume_gateway_url")),

        "RESUMED" => new ResumedEvent(),

        "MESSAGE_CREATE" => new MessageCreatedEvent(RequireBody<DiscordMessage>(data, name)),

        "MESSAGE_UPDATE" => new MessageUpdatedEvent(
            data.RequireSnowflake("id"),
            data.RequireSnowflake("channel_id"),
            data.SnowflakeOrNull("guild_id"),
            data.Property("author") is null ? null : data.Deserialize<DiscordMessage>(DiscordJson.Options)),

        "MESSAGE_DELETE" => new MessageDeletedEvent(
            data.RequireSnowflake("id"),
            data.RequireSnowflake("channel_id"),
            data.SnowflakeOrNull("guild_id")),

        "MESSAGE_DELETE_BULK" => new MessagesBulkDeletedEvent(
            ReadSnowflakes(data, "ids"),
            data.RequireSnowflake("channel_id"),
            data.SnowflakeOrNull("guild_id")),

        "MESSAGE_REACTION_ADD" => new ReactionAddedEvent(
            data.RequireSnowflake("message_id"),
            data.RequireSnowflake("channel_id"),
            data.RequireSnowflake("user_id"),
            data.SnowflakeOrNull("guild_id"),
            ReadEmoji(data)),

        "MESSAGE_REACTION_REMOVE" => new ReactionRemovedEvent(
            data.RequireSnowflake("message_id"),
            data.RequireSnowflake("channel_id"),
            data.RequireSnowflake("user_id"),
            data.SnowflakeOrNull("guild_id"),
            ReadEmoji(data)),

        "MESSAGE_REACTION_REMOVE_ALL" => new ReactionsClearedEvent(
            data.RequireSnowflake("message_id"),
            data.RequireSnowflake("channel_id"),
            data.SnowflakeOrNull("guild_id")),

        "MESSAGE_REACTION_REMOVE_EMOJI" => new ReactionEmojiClearedEvent(
            data.RequireSnowflake("message_id"),
            data.RequireSnowflake("channel_id"),
            data.SnowflakeOrNull("guild_id"),
            ReadEmoji(data)),

        "CHANNEL_CREATE" => new ChannelCreatedEvent(RequireBody<DiscordChannel>(data, name)),

        "CHANNEL_UPDATE" => new ChannelUpdatedEvent(RequireBody<DiscordChannel>(data, name)),

        "CHANNEL_DELETE" => new ChannelDeletedEvent(
            data.RequireSnowflake("id"),
            data.SnowflakeOrNull("guild_id"),
            data.Deserialize<DiscordChannel>(DiscordJson.Options)),

        "THREAD_CREATE" => new ThreadCreatedEvent(RequireBody<DiscordChannel>(data, name)),

        "THREAD_UPDATE" => new ThreadUpdatedEvent(RequireBody<DiscordChannel>(data, name)),

        "THREAD_DELETE" => new ThreadDeletedEvent(
            data.RequireSnowflake("id"),
            data.SnowflakeOrNull("parent_id"),
            data.SnowflakeOrNull("guild_id")),

        "GUILD_CREATE" => new GuildAvailableEvent(
            data.RequireSnowflake("id"),
            data.DeserializeList<DiscordChannel>("channels", DiscordJson.Options),
            RequireBody<DiscordGuild>(data, name))
        {
            Presences = ReadPresences(data),
            VoiceStates = ReadVoiceStates(data),
            Threads = data.DeserializeList<DiscordChannel>("threads", DiscordJson.Options),
            Stickers = ReadGuildStickers(data),
            ScheduledEvents = ReadScheduledEvents(data),
            StageInstances = ReadStageInstances(data)
        },

        "GUILD_UPDATE" => new GuildUpdatedEvent(RequireBody<DiscordGuild>(data, name)),

        "GUILD_DELETE" => new GuildUnavailableEvent(
            data.RequireSnowflake("id"),
            !data.Flag("unavailable")),

        "GUILD_MEMBER_ADD" => new MemberJoinedEvent(
            data.RequireSnowflake("guild_id"),
            ReadMember(data, name)),

        "GUILD_MEMBER_UPDATE" => new MemberUpdatedEvent(
            data.RequireSnowflake("guild_id"),
            ReadMember(data, name)),

        "GUILD_MEMBER_REMOVE" => new MemberLeftEvent(
            data.RequireSnowflake("guild_id"),
            Require<DiscordUser>(data, "user", name)),

        "GUILD_ROLE_CREATE" => new RoleCreatedEvent(
            data.RequireSnowflake("guild_id"),
            ReadRole(data, name)),

        "GUILD_ROLE_UPDATE" => new RoleUpdatedEvent(
            data.RequireSnowflake("guild_id"),
            ReadRole(data, name)),

        "GUILD_ROLE_DELETE" => new RoleDeletedEvent(
            data.RequireSnowflake("guild_id"),
            data.RequireSnowflake("role_id")),

        "GUILD_BAN_ADD" => new BanAddedEvent(
            data.RequireSnowflake("guild_id"),
            Require<DiscordUser>(data, "user", name)),

        "GUILD_BAN_REMOVE" => new BanRemovedEvent(
            data.RequireSnowflake("guild_id"),
            Require<DiscordUser>(data, "user", name)),

        "WEBHOOKS_UPDATE" => new WebhooksUpdatedEvent(
            data.RequireSnowflake("channel_id"),
            data.SnowflakeOrNull("guild_id")),

        "TYPING_START" => new TypingStartedEvent(
            data.RequireSnowflake("channel_id"),
            data.RequireSnowflake("user_id"),
            data.SnowflakeOrNull("guild_id")),

        "INTERACTION_CREATE" => new InteractionCreatedEvent(RequireBody<DiscordInteraction>(data, name)),

        "PRESENCE_UPDATE" => new PresenceUpdatedEvent(ReadPresence(data, name), null),

        "VOICE_STATE_UPDATE" => new VoiceStateUpdatedEvent(RequireBody<DiscordVoiceState>(data, name)),

        "VOICE_SERVER_UPDATE" => new VoiceServerUpdatedEvent(
            data.RequireSnowflake("guild_id"),
            data.StringOrNull("token") ?? string.Empty,
            data.StringOrNull("endpoint")),

        "GUILD_MEMBERS_CHUNK" => ReadMembersChunk(data, name),

        "THREAD_LIST_SYNC" => ReadThreadListSync(data),

        "THREAD_MEMBER_UPDATE" => new ThreadMemberUpdatedEvent(RequireBody<DiscordThreadMember>(data, name)),

        "THREAD_MEMBERS_UPDATE" => ReadThreadMembersUpdate(data),

        "CHANNEL_PINS_UPDATE" => new ChannelPinsUpdatedEvent(
            data.RequireSnowflake("channel_id"),
            data.SnowflakeOrNull("guild_id"),
            data.TimestampOrNull("last_pin_timestamp")),

        "INVITE_CREATE" => new InviteCreatedEvent(RequireBody<DiscordInvite>(data, name)),

        "INVITE_DELETE" => new InviteDeletedEvent(
            data.RequireString("code"),
            data.RequireSnowflake("channel_id"),
            data.SnowflakeOrNull("guild_id")),

        "USER_UPDATE" => new UserUpdatedEvent(RequireBody<DiscordUser>(data, name)),

        "GUILD_EMOJIS_UPDATE" => new GuildEmojisUpdatedEvent(
            data.RequireSnowflake("guild_id"),
            data.DeserializeList<DiscordGuildEmoji>("emojis", DiscordJson.Options)),

        "GUILD_STICKERS_UPDATE" => ReadStickersUpdate(data),

        "GUILD_AUDIT_LOG_ENTRY_CREATE" => new AuditLogEntryCreatedEvent(ReadAuditLogEntry(data, name)),

        "MESSAGE_POLL_VOTE_ADD" => new PollVoteAddedEvent(
            data.RequireSnowflake("user_id"),
            data.RequireSnowflake("channel_id"),
            data.RequireSnowflake("message_id"),
            data.SnowflakeOrNull("guild_id"),
            data.RequireInt32("answer_id")),

        "MESSAGE_POLL_VOTE_REMOVE" => new PollVoteRemovedEvent(
            data.RequireSnowflake("user_id"),
            data.RequireSnowflake("channel_id"),
            data.RequireSnowflake("message_id"),
            data.SnowflakeOrNull("guild_id"),
            data.RequireInt32("answer_id")),

        "AUTO_MODERATION_RULE_CREATE" => new AutoModerationRuleCreatedEvent(RequireBody<DiscordAutoModerationRule>(
            data, name)),

        "AUTO_MODERATION_RULE_UPDATE" => new AutoModerationRuleUpdatedEvent(RequireBody<DiscordAutoModerationRule>(
            data, name)),

        "AUTO_MODERATION_RULE_DELETE" => new AutoModerationRuleDeletedEvent(RequireBody<DiscordAutoModerationRule>(
            data, name)),

        "AUTO_MODERATION_ACTION_EXECUTION" => ReadAutoModerationExecution(data, name),

        "GUILD_SCHEDULED_EVENT_CREATE" => new ScheduledEventCreatedEvent(
            RequireBody<DiscordScheduledEvent>(data, name)),

        "GUILD_SCHEDULED_EVENT_UPDATE" => new ScheduledEventUpdatedEvent(
            RequireBody<DiscordScheduledEvent>(data, name)),

        "GUILD_SCHEDULED_EVENT_DELETE" => new ScheduledEventDeletedEvent(
            RequireBody<DiscordScheduledEvent>(data, name)),

        "GUILD_SCHEDULED_EVENT_USER_ADD" => new ScheduledEventUserAddedEvent(
            data.RequireSnowflake("guild_scheduled_event_id"),
            data.RequireSnowflake("user_id"),
            data.RequireSnowflake("guild_id")),

        "GUILD_SCHEDULED_EVENT_USER_REMOVE" => new ScheduledEventUserRemovedEvent(
            data.RequireSnowflake("guild_scheduled_event_id"),
            data.RequireSnowflake("user_id"),
            data.RequireSnowflake("guild_id")),

        "STAGE_INSTANCE_CREATE" => new StageInstanceCreatedEvent(RequireBody<DiscordStageInstance>(data, name)),

        "STAGE_INSTANCE_UPDATE" => new StageInstanceUpdatedEvent(RequireBody<DiscordStageInstance>(data, name)),

        "STAGE_INSTANCE_DELETE" => new StageInstanceDeletedEvent(RequireBody<DiscordStageInstance>(data, name)),

        "INTEGRATION_CREATE" => new IntegrationCreatedEvent(ReadIntegration(data, name)),

        "INTEGRATION_UPDATE" => new IntegrationUpdatedEvent(ReadIntegration(data, name)),

        "INTEGRATION_DELETE" => new IntegrationDeletedEvent(
            data.RequireSnowflake("id"),
            data.RequireSnowflake("guild_id"),
            data.SnowflakeOrNull("application_id")),

        "GUILD_INTEGRATIONS_UPDATE" => new GuildIntegrationsUpdatedEvent(data.RequireSnowflake("guild_id")),

        "ENTITLEMENT_CREATE" => new EntitlementCreatedEvent(RequireBody<DiscordEntitlement>(data, name)),

        "ENTITLEMENT_UPDATE" => new EntitlementUpdatedEvent(RequireBody<DiscordEntitlement>(data, name)),

        "ENTITLEMENT_DELETE" => new EntitlementDeletedEvent(RequireBody<DiscordEntitlement>(data, name)),

        "APPLICATION_COMMAND_PERMISSIONS_UPDATE" => new CommandPermissionsUpdatedEvent(
            RequireBody<DiscordCommandPermissions>(data, name)),

        _ => new UnknownEvent(data.Clone())
    };

    private static T RequireBody<T>(JsonElement data, string name) =>
        data.Deserialize<T>(DiscordJson.Options) ??
        throw new JsonException($"The {name} dispatch carried no readable body.");

    private static T Require<T>(JsonElement data, string property, string name) =>
        data.Deserialize<T>(property, DiscordJson.Options) ??
        throw new JsonException($"The {name} dispatch has no '{property}' property.");

    private static DiscordMember ReadMember(JsonElement data, string name)
    {
        var member = RequireBody<DiscordMember>(data, name);

        return member.GuildId is null ? member.In(data.RequireSnowflake("guild_id")) : member;
    }

    private static DiscordRole ReadRole(JsonElement data, string name)
    {
        var role = Require<DiscordRole>(data, "role", name);

        return role.GuildId is null ? role with { GuildId = data.RequireSnowflake("guild_id") } : role;
    }

    private static IReadOnlyList<Snowflake> ReadSnowflakes(JsonElement data, string property)
    {
        if (data.Property(property) is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var values = new List<Snowflake>(array.GetArrayLength());

        foreach (var element in array.EnumerateArray())
        {
            if (element.GetString() is { } raw && ulong.TryParse(raw, out var id))
                values.Add(new Snowflake(id));
            else if (element.ValueKind is JsonValueKind.Number)
                values.Add(new Snowflake(element.GetUInt64()));
        }

        return values;
    }

    private static DiscordPresence ReadPresence(JsonElement data, string name)
    {
        var presence = RequireBody<DiscordPresence>(data, name);

        return presence.GuildId is null && data.SnowflakeOrNull("guild_id") is { } guildId
            ? presence.In(guildId)
            : presence;
    }

    private static IReadOnlyList<DiscordPresence> ReadPresences(JsonElement data)
    {
        var presences = data.DeserializeList<DiscordPresence>("presences", DiscordJson.Options);

        if (presences.Count == 0)
            return presences;

        var guildId = data.RequireSnowflake("id");

        return presences
            .Select(presence => presence.GuildId is null ? presence.In(guildId) : presence)
            .ToArray();
    }

    private static IReadOnlyList<DiscordVoiceState> ReadVoiceStates(JsonElement data)
    {
        var states = data.DeserializeList<DiscordVoiceState>("voice_states", DiscordJson.Options);

        if (states.Count == 0)
            return states;

        var guildId = data.RequireSnowflake("id");

        return states.Select(state => state.GuildId is null ? state.In(guildId) : state).ToArray();
    }

    private static IReadOnlyList<DiscordSticker> ReadGuildStickers(JsonElement data)
    {
        var stickers = data.DeserializeList<DiscordSticker>("stickers", DiscordJson.Options);

        return stickers.Count == 0
            ? stickers
            : stickers.Select(sticker => sticker.In(data.RequireSnowflake("id"))).ToArray();
    }

    private static IReadOnlyList<DiscordScheduledEvent> ReadScheduledEvents(JsonElement data)
    {
        var events = data.DeserializeList<DiscordScheduledEvent>("guild_scheduled_events", DiscordJson.Options);

        return events.Count == 0
            ? events
            : events.Select(scheduled => scheduled.In(data.RequireSnowflake("id"))).ToArray();
    }

    private static IReadOnlyList<DiscordStageInstance> ReadStageInstances(JsonElement data)
    {
        var instances = data.DeserializeList<DiscordStageInstance>("stage_instances", DiscordJson.Options);

        return instances.Count == 0
            ? instances
            : instances.Select(instance => instance.In(data.RequireSnowflake("id"))).ToArray();
    }

    private static GuildMembersChunkEvent ReadMembersChunk(JsonElement data, string name)
    {
        var guildId = data.RequireSnowflake("guild_id");
        var members = data.DeserializeList<DiscordMember>("members", DiscordJson.Options)
            .Select(member => member.GuildId is null ? member.In(guildId) : member)
            .ToArray();

        var presences = data.DeserializeList<DiscordPresence>("presences", DiscordJson.Options)
            .Select(presence => presence.GuildId is null ? presence.In(guildId) : presence)
            .ToArray();

        return new GuildMembersChunkEvent(
            guildId,
            members,
            data.Int32OrNull("chunk_index") ?? 0,
            data.Int32OrNull("chunk_count") ?? 1)
        {
            NotFound = ReadSnowflakes(data, "not_found"),
            Presences = presences,
            Nonce = data.StringOrNull("nonce")
        };
    }

    private static ThreadListSyncEvent ReadThreadListSync(JsonElement data)
    {
        var guildId = data.RequireSnowflake("guild_id");

        return new ThreadListSyncEvent(
            guildId,
            data.DeserializeList<DiscordChannel>("threads", DiscordJson.Options),
            StampThreadMembers(data, "members", guildId, null),
            ReadSnowflakes(data, "channel_ids"));
    }

    private static ThreadMembersUpdatedEvent ReadThreadMembersUpdate(JsonElement data)
    {
        var threadId = data.RequireSnowflake("id");
        var guildId = data.RequireSnowflake("guild_id");

        return new ThreadMembersUpdatedEvent(
            threadId,
            guildId,
            data.Int32OrNull("member_count") ?? 0,
            StampThreadMembers(data, "added_members", guildId, threadId),
            ReadSnowflakes(data, "removed_member_ids"));
    }

    private static IReadOnlyList<DiscordThreadMember> StampThreadMembers(JsonElement data, string property,
        Snowflake guildId, Snowflake? threadId)
    {
        var members = data.DeserializeList<DiscordThreadMember>(property, DiscordJson.Options);

        if (members.Count == 0)
            return members;

        return members
            .Select(member => threadId is { } id ? member.On(id) : member)
            .Select(member => member.GuildId is null ? member.In(guildId) : member)
            .ToArray();
    }

    private static GuildStickersUpdatedEvent ReadStickersUpdate(JsonElement data)
    {
        var guildId = data.RequireSnowflake("guild_id");
        var stickers = data.DeserializeList<DiscordSticker>("stickers", DiscordJson.Options)
            .Select(sticker => sticker.In(guildId))
            .ToArray();

        return new GuildStickersUpdatedEvent(guildId, stickers);
    }

    private static DiscordAuditLogEntry ReadAuditLogEntry(JsonElement data, string name)
    {
        var entry = RequireBody<DiscordAuditLogEntry>(data, name);

        return data.SnowflakeOrNull("guild_id") is { } guildId ? entry.In(guildId) : entry;
    }

    private static DiscordIntegration ReadIntegration(JsonElement data, string name)
    {
        var integration = RequireBody<DiscordIntegration>(data, name);

        return data.SnowflakeOrNull("guild_id") is { } guildId ? integration.In(guildId) : integration;
    }

    private static AutoModerationActionExecutedEvent ReadAutoModerationExecution(JsonElement data, string name) =>
        new(
            data.RequireSnowflake("guild_id"),
            data.RequireSnowflake("rule_id"),
            Require<AutoModerationAction>(data, "action", name),
            data.RequireSnowflake("user_id"))
        {
            TriggerType = (AutoModerationTriggerType)(data.Int32OrNull("rule_trigger_type") ?? 1),
            ChannelId = data.SnowflakeOrNull("channel_id"),
            MessageId = data.SnowflakeOrNull("message_id"),
            AlertMessageId = data.SnowflakeOrNull("alert_system_message_id"),
            Content = data.StringOrNull("content"),
            MatchedKeyword = data.StringOrNull("matched_keyword"),
            MatchedContent = data.StringOrNull("matched_content")
        };

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

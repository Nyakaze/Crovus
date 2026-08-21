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
            data.StringOrNull("resume_gateway_url"))
        {
            Guilds = ReadGuildStubs(data)
        },

        "RESUMED" => new ResumedEvent(),

        "MESSAGE_CREATE" => ReadMessageCreate(data, name),

        "MESSAGE_UPDATE" => ReadMessageUpdate(data),

        "MESSAGE_DELETE" => new MessageDeletedEvent(
            DiscordMessage.Partial(data.RequireSnowflake("id"), data.RequireSnowflake("channel_id"),
                data.SnowflakeOrNull("guild_id")),
            Channel(data, "channel_id"),
            Guild(data)),

        "MESSAGE_DELETE_BULK" => ReadBulkDelete(data),

        "MESSAGE_REACTION_ADD" => new ReactionAddedEvent(
            Message(data),
            Channel(data, "channel_id"),
            DiscordUser.Partial(data.RequireSnowflake("user_id")),
            Guild(data),
            ReadEmoji(data))
        {
            Member = ReadOptionalMember(data)
        },

        "MESSAGE_REACTION_REMOVE" => new ReactionRemovedEvent(
            Message(data),
            Channel(data, "channel_id"),
            DiscordUser.Partial(data.RequireSnowflake("user_id")),
            Guild(data),
            ReadEmoji(data))
        {
            Member = ReadOptionalMember(data)
        },

        "MESSAGE_REACTION_REMOVE_ALL" => new ReactionsClearedEvent(
            Message(data),
            Channel(data, "channel_id"),
            Guild(data)),

        "MESSAGE_REACTION_REMOVE_EMOJI" => new ReactionEmojiClearedEvent(
            Message(data),
            Channel(data, "channel_id"),
            Guild(data),
            ReadEmoji(data)),

        "CHANNEL_CREATE" => new ChannelCreatedEvent(RequireBody<DiscordChannel>(data, name), Guild(data)),

        "CHANNEL_UPDATE" => new ChannelUpdatedEvent(RequireBody<DiscordChannel>(data, name), Guild(data)),

        "CHANNEL_DELETE" => new ChannelDeletedEvent(RequireBody<DiscordChannel>(data, name), Guild(data)),

        "THREAD_CREATE" => ReadThread(data, name, (thread, parent, guild) =>
            new ThreadCreatedEvent(thread, parent, guild)),

        "THREAD_UPDATE" => ReadThread(data, name, (thread, parent, guild) =>
            new ThreadUpdatedEvent(thread, parent, guild)),

        "THREAD_DELETE" => new ThreadDeletedEvent(
            DiscordChannel.Partial(data.RequireSnowflake("id"), data.SnowflakeOrNull("guild_id"),
                data.SnowflakeOrNull("parent_id"), isThread: true),
            ParentChannel(data),
            Guild(data)),

        "GUILD_CREATE" => ReadGuildCreate(data, name),

        "GUILD_UPDATE" => new GuildUpdatedEvent(RequireBody<DiscordGuild>(data, name)),

        "GUILD_DELETE" => new GuildUnavailableEvent(
            DiscordGuild.Partial(data.RequireSnowflake("id")),
            !data.Flag("unavailable")),

        "GUILD_MEMBER_ADD" => new MemberJoinedEvent(
            RequiredGuild(data),
            ReadMember(data, name)),

        "GUILD_MEMBER_UPDATE" => new MemberUpdatedEvent(
            RequiredGuild(data),
            ReadMember(data, name)),

        "GUILD_MEMBER_REMOVE" => new MemberLeftEvent(
            RequiredGuild(data),
            Require<DiscordUser>(data, "user", name)),

        "GUILD_ROLE_CREATE" => new RoleCreatedEvent(
            RequiredGuild(data),
            ReadRole(data, name)),

        "GUILD_ROLE_UPDATE" => new RoleUpdatedEvent(
            RequiredGuild(data),
            ReadRole(data, name)),

        "GUILD_ROLE_DELETE" => new RoleDeletedEvent(
            RequiredGuild(data),
            DiscordRole.Partial(data.RequireSnowflake("role_id"), data.RequireSnowflake("guild_id"))),

        "GUILD_BAN_ADD" => new BanAddedEvent(
            RequiredGuild(data),
            Require<DiscordUser>(data, "user", name)),

        "GUILD_BAN_REMOVE" => new BanRemovedEvent(
            RequiredGuild(data),
            Require<DiscordUser>(data, "user", name)),

        "WEBHOOKS_UPDATE" => new WebhooksUpdatedEvent(
            Channel(data, "channel_id"),
            Guild(data)),

        "TYPING_START" => new TypingStartedEvent(
            Channel(data, "channel_id"),
            DiscordUser.Partial(data.RequireSnowflake("user_id")),
            Guild(data))
        {
            Member = ReadOptionalMember(data),
            StartedAt = data.Int32OrNull("timestamp") is { } seconds
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : null
        },

        "INTERACTION_CREATE" => ReadInteraction(data, name),

        "PRESENCE_UPDATE" => ReadPresenceUpdate(data, name),

        "VOICE_STATE_UPDATE" => ReadVoiceState(data, name),

        "VOICE_SERVER_UPDATE" => new VoiceServerUpdatedEvent(
            RequiredGuild(data),
            data.StringOrNull("token") ?? string.Empty,
            data.StringOrNull("endpoint")),

        "GUILD_MEMBERS_CHUNK" => ReadMembersChunk(data),

        "THREAD_LIST_SYNC" => ReadThreadListSync(data),

        "THREAD_MEMBER_UPDATE" => ReadThreadMemberUpdate(data, name),

        "THREAD_MEMBERS_UPDATE" => ReadThreadMembersUpdate(data),

        "CHANNEL_PINS_UPDATE" => new ChannelPinsUpdatedEvent(
            Channel(data, "channel_id"),
            Guild(data),
            data.TimestampOrNull("last_pin_timestamp")),

        "INVITE_CREATE" => ReadInviteCreate(data, name),

        "INVITE_DELETE" => new InviteDeletedEvent(
            DiscordInvite.Partial(data.RequireString("code"), data.RequireSnowflake("channel_id"),
                data.SnowflakeOrNull("guild_id")),
            Channel(data, "channel_id"),
            Guild(data)),

        "USER_UPDATE" => new UserUpdatedEvent(RequireBody<DiscordUser>(data, name)),

        "GUILD_EMOJIS_UPDATE" => new GuildEmojisUpdatedEvent(
            RequiredGuild(data),
            data.DeserializeList<DiscordGuildEmoji>("emojis", DiscordJson.Options)),

        "GUILD_STICKERS_UPDATE" => ReadStickersUpdate(data),

        "GUILD_AUDIT_LOG_ENTRY_CREATE" => ReadAuditLogEntry(data, name),

        "MESSAGE_POLL_VOTE_ADD" => new PollVoteAddedEvent(
            DiscordUser.Partial(data.RequireSnowflake("user_id")),
            Channel(data, "channel_id"),
            Message(data),
            Guild(data),
            data.RequireInt32("answer_id")),

        "MESSAGE_POLL_VOTE_REMOVE" => new PollVoteRemovedEvent(
            DiscordUser.Partial(data.RequireSnowflake("user_id")),
            Channel(data, "channel_id"),
            Message(data),
            Guild(data),
            data.RequireInt32("answer_id")),

        "AUTO_MODERATION_RULE_CREATE" => new AutoModerationRuleCreatedEvent(
            RequireBody<DiscordAutoModerationRule>(data, name), Guild(data)),

        "AUTO_MODERATION_RULE_UPDATE" => new AutoModerationRuleUpdatedEvent(
            RequireBody<DiscordAutoModerationRule>(data, name), Guild(data)),

        "AUTO_MODERATION_RULE_DELETE" => new AutoModerationRuleDeletedEvent(
            RequireBody<DiscordAutoModerationRule>(data, name), Guild(data)),

        "AUTO_MODERATION_ACTION_EXECUTION" => ReadAutoModerationExecution(data, name),

        "GUILD_SCHEDULED_EVENT_CREATE" => ReadScheduledEvent(data, name, (scheduled, guild, channel) =>
            new ScheduledEventCreatedEvent(scheduled, guild, channel)),

        "GUILD_SCHEDULED_EVENT_UPDATE" => ReadScheduledEvent(data, name, (scheduled, guild, channel) =>
            new ScheduledEventUpdatedEvent(scheduled, guild, channel)),

        "GUILD_SCHEDULED_EVENT_DELETE" => ReadScheduledEvent(data, name, (scheduled, guild, channel) =>
            new ScheduledEventDeletedEvent(scheduled, guild, channel)),

        "GUILD_SCHEDULED_EVENT_USER_ADD" => new ScheduledEventUserAddedEvent(
            DiscordScheduledEvent.Partial(data.RequireSnowflake("guild_scheduled_event_id"),
                data.RequireSnowflake("guild_id")),
            DiscordUser.Partial(data.RequireSnowflake("user_id")),
            RequiredGuild(data)),

        "GUILD_SCHEDULED_EVENT_USER_REMOVE" => new ScheduledEventUserRemovedEvent(
            DiscordScheduledEvent.Partial(data.RequireSnowflake("guild_scheduled_event_id"),
                data.RequireSnowflake("guild_id")),
            DiscordUser.Partial(data.RequireSnowflake("user_id")),
            RequiredGuild(data)),

        "STAGE_INSTANCE_CREATE" => ReadStageInstance(data, name, (instance, channel, guild) =>
            new StageInstanceCreatedEvent(instance, channel, guild)),

        "STAGE_INSTANCE_UPDATE" => ReadStageInstance(data, name, (instance, channel, guild) =>
            new StageInstanceUpdatedEvent(instance, channel, guild)),

        "STAGE_INSTANCE_DELETE" => ReadStageInstance(data, name, (instance, channel, guild) =>
            new StageInstanceDeletedEvent(instance, channel, guild)),

        "INTEGRATION_CREATE" => new IntegrationCreatedEvent(ReadIntegration(data, name), Guild(data)),

        "INTEGRATION_UPDATE" => new IntegrationUpdatedEvent(ReadIntegration(data, name), Guild(data)),

        "INTEGRATION_DELETE" => new IntegrationDeletedEvent(
            DiscordIntegration.Partial(data.RequireSnowflake("id"), data.RequireSnowflake("guild_id")),
            RequiredGuild(data),
            data.SnowflakeOrNull("application_id")),

        "GUILD_INTEGRATIONS_UPDATE" => new GuildIntegrationsUpdatedEvent(RequiredGuild(data)),

        "ENTITLEMENT_CREATE" => ReadEntitlement(data, name, (entitlement, user, guild) =>
            new EntitlementCreatedEvent(entitlement, user, guild)),

        "ENTITLEMENT_UPDATE" => ReadEntitlement(data, name, (entitlement, user, guild) =>
            new EntitlementUpdatedEvent(entitlement, user, guild)),

        "ENTITLEMENT_DELETE" => ReadEntitlement(data, name, (entitlement, user, guild) =>
            new EntitlementDeletedEvent(entitlement, user, guild)),

        "APPLICATION_COMMAND_PERMISSIONS_UPDATE" => ReadCommandPermissions(data, name),

        _ => new UnknownEvent(data.Clone())
    };

    private static T RequireBody<T>(JsonElement data, string name) =>
        data.Deserialize<T>(DiscordJson.Options) ??
        throw new JsonException($"The {name} dispatch carried no readable body.");

    private static T Require<T>(JsonElement data, string property, string name) =>
        data.Deserialize<T>(property, DiscordJson.Options) ??
        throw new JsonException($"The {name} dispatch has no '{property}' property.");

    private static DiscordGuild? Guild(JsonElement data) =>
        data.SnowflakeOrNull("guild_id") is { } guildId ? DiscordGuild.Partial(guildId) : null;

    private static DiscordGuild RequiredGuild(JsonElement data) =>
        DiscordGuild.Partial(data.RequireSnowflake("guild_id"));

    private static DiscordChannel Channel(JsonElement data, string property) =>
        DiscordChannel.Partial(data.RequireSnowflake(property), data.SnowflakeOrNull("guild_id"));

    private static DiscordChannel? ParentChannel(JsonElement data) =>
        data.SnowflakeOrNull("parent_id") is { } parentId
            ? DiscordChannel.Partial(parentId, data.SnowflakeOrNull("guild_id"))
            : null;

    private static DiscordMessage Message(JsonElement data) =>
        DiscordMessage.Partial(data.RequireSnowflake("message_id"), data.RequireSnowflake("channel_id"),
            data.SnowflakeOrNull("guild_id"));

    private static MessageCreatedEvent ReadMessageCreate(JsonElement data, string name)
    {
        var message = RequireBody<DiscordMessage>(data, name);

        return new MessageCreatedEvent(
            message,
            DiscordChannel.Partial(message.ChannelId, message.GuildId),
            message.GuildId is { } guildId ? DiscordGuild.Partial(guildId) : null)
        {
            Member = ReadOptionalMember(data) is { } member ? member with { User = message.Author } : null
        };
    }

    private static MessageUpdatedEvent ReadMessageUpdate(JsonElement data)
    {
        var messageId = data.RequireSnowflake("id");
        var channelId = data.RequireSnowflake("channel_id");
        var guildId = data.SnowflakeOrNull("guild_id");

        var message = data.Property("author") is null
            ? DiscordMessage.Partial(messageId, channelId, guildId)
            : data.Deserialize<DiscordMessage>(DiscordJson.Options) ??
              DiscordMessage.Partial(messageId, channelId, guildId);

        return new MessageUpdatedEvent(
            message,
            DiscordChannel.Partial(channelId, guildId),
            guildId is { } id ? DiscordGuild.Partial(id) : null);
    }

    private static MessagesBulkDeletedEvent ReadBulkDelete(JsonElement data)
    {
        var channelId = data.RequireSnowflake("channel_id");
        var guildId = data.SnowflakeOrNull("guild_id");

        var messages = ReadSnowflakes(data, "ids")
            .Select(id => DiscordMessage.Partial(id, channelId, guildId))
            .ToArray();

        return new MessagesBulkDeletedEvent(
            messages,
            DiscordChannel.Partial(channelId, guildId),
            guildId is { } id ? DiscordGuild.Partial(id) : null);
    }

    private static DiscordEvent ReadThread(JsonElement data, string name,
        Func<DiscordChannel, DiscordChannel?, DiscordGuild?, DiscordEvent> build)
    {
        var thread = RequireBody<DiscordChannel>(data, name);
        var guildId = thread.GuildId ?? data.SnowflakeOrNull("guild_id");

        var parent = thread.ParentId is { } parentId ? DiscordChannel.Partial(parentId, guildId) : null;

        return build(thread, parent, guildId is { } id ? DiscordGuild.Partial(id) : null);
    }

    private static GuildAvailableEvent ReadGuildCreate(JsonElement data, string name)
    {
        var guild = RequireBody<DiscordGuild>(data, name);

        return new GuildAvailableEvent(
            guild,
            data.DeserializeList<DiscordChannel>("channels", DiscordJson.Options))
        {
            Presences = ReadPresences(data),
            VoiceStates = ReadVoiceStates(data),
            Threads = data.DeserializeList<DiscordChannel>("threads", DiscordJson.Options),
            Stickers = ReadGuildStickers(data),
            ScheduledEvents = ReadScheduledEvents(data),
            StageInstances = ReadStageInstances(data),
            Members = ReadGuildMembers(data)
        };
    }

    private static IReadOnlyList<DiscordGuild> ReadGuildStubs(JsonElement data)
    {
        if (data.Property("guilds") is not { ValueKind: JsonValueKind.Array } guilds)
            return [];

        var stubs = new List<DiscordGuild>(guilds.GetArrayLength());

        foreach (var element in guilds.EnumerateArray())
        {
            if (element.SnowflakeOrNull("id") is { } id)
                stubs.Add(DiscordGuild.Partial(id));
        }

        return stubs;
    }

    private static IReadOnlyList<DiscordMember> ReadGuildMembers(JsonElement data)
    {
        var members = data.DeserializeList<DiscordMember>("members", DiscordJson.Options);

        if (members.Count == 0)
            return members;

        var guildId = data.RequireSnowflake("id");

        return members.Select(member => member.GuildId is null ? member.In(guildId) : member).ToArray();
    }

    private static DiscordMember ReadMember(JsonElement data, string name)
    {
        var member = RequireBody<DiscordMember>(data, name);

        return member.GuildId is null ? member.In(data.RequireSnowflake("guild_id")) : member;
    }

    private static DiscordMember? ReadOptionalMember(JsonElement data)
    {
        if (data.Property("member") is not { ValueKind: JsonValueKind.Object } element ||
            element.Deserialize<DiscordMember>(DiscordJson.Options) is not { } member)
            return null;

        return member.GuildId is null && data.SnowflakeOrNull("guild_id") is { } guildId
            ? member.In(guildId)
            : member;
    }

    private static DiscordRole ReadRole(JsonElement data, string name)
    {
        var role = Require<DiscordRole>(data, "role", name);

        return role.In(data.RequireSnowflake("guild_id"));
    }

    private static InteractionCreatedEvent ReadInteraction(JsonElement data, string name)
    {
        var interaction = RequireBody<DiscordInteraction>(data, name);

        return new InteractionCreatedEvent(
            interaction,
            interaction.ChannelId is { } channelId
                ? DiscordChannel.Partial(channelId, interaction.GuildId)
                : null,
            interaction.GuildId is { } guildId ? DiscordGuild.Partial(guildId) : null);
    }

    private static InviteCreatedEvent ReadInviteCreate(JsonElement data, string name)
    {
        var invite = RequireBody<DiscordInvite>(data, name);
        var guildId = invite.GuildId ?? data.SnowflakeOrNull("guild_id");
        var channelId = invite.ChannelId ?? data.RequireSnowflake("channel_id");

        return new InviteCreatedEvent(
            invite,
            DiscordChannel.Partial(channelId, guildId),
            guildId is { } id ? DiscordGuild.Partial(id) : null);
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

    private static PresenceUpdatedEvent ReadPresenceUpdate(JsonElement data, string name)
    {
        var presence = RequireBody<DiscordPresence>(data, name);
        var guildId = presence.GuildId ?? data.SnowflakeOrNull("guild_id");

        if (presence.GuildId is null && guildId is { } id)
            presence = presence.In(id);

        return new PresenceUpdatedEvent(presence, null)
        {
            Guild = guildId is { } owner ? DiscordGuild.Partial(owner) : null
        };
    }

    private static VoiceStateUpdatedEvent ReadVoiceState(JsonElement data, string name)
    {
        var state = RequireBody<DiscordVoiceState>(data, name);

        return new VoiceStateUpdatedEvent(
            state,
            state.Member?.User ?? DiscordUser.Partial(state.UserId),
            state.GuildId is { } guildId ? DiscordGuild.Partial(guildId) : null)
        {
            Channel = state.ChannelId is { } channelId
                ? DiscordChannel.Partial(channelId, state.GuildId)
                : null
        };
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

    private static GuildMembersChunkEvent ReadMembersChunk(JsonElement data)
    {
        var guildId = data.RequireSnowflake("guild_id");
        var members = data.DeserializeList<DiscordMember>("members", DiscordJson.Options)
            .Select(member => member.GuildId is null ? member.In(guildId) : member)
            .ToArray();

        var presences = data.DeserializeList<DiscordPresence>("presences", DiscordJson.Options)
            .Select(presence => presence.GuildId is null ? presence.In(guildId) : presence)
            .ToArray();

        return new GuildMembersChunkEvent(
            DiscordGuild.Partial(guildId),
            members,
            data.Int32OrNull("chunk_index") ?? 0,
            data.Int32OrNull("chunk_count") ?? 1)
        {
            NotFound = [.. ReadSnowflakes(data, "not_found").Select(DiscordUser.Partial)],
            Presences = presences,
            Nonce = data.StringOrNull("nonce")
        };
    }

    private static ThreadListSyncEvent ReadThreadListSync(JsonElement data)
    {
        var guildId = data.RequireSnowflake("guild_id");

        return new ThreadListSyncEvent(
            DiscordGuild.Partial(guildId),
            data.DeserializeList<DiscordChannel>("threads", DiscordJson.Options),
            StampThreadMembers(data, "members", guildId, null),
            [.. ReadSnowflakes(data, "channel_ids").Select(id => DiscordChannel.Partial(id, guildId))]);
    }

    private static ThreadMemberUpdatedEvent ReadThreadMemberUpdate(JsonElement data, string name)
    {
        var member = RequireBody<DiscordThreadMember>(data, name);
        var guildId = member.GuildId ?? data.SnowflakeOrNull("guild_id");

        if (member.GuildId is null && guildId is { } owner)
            member = member.In(owner);

        return new ThreadMemberUpdatedEvent(
            member,
            member.ThreadId is { } threadId ? DiscordChannel.Partial(threadId, guildId, isThread: true) : null,
            guildId is { } id ? DiscordGuild.Partial(id) : null);
    }

    private static ThreadMembersUpdatedEvent ReadThreadMembersUpdate(JsonElement data)
    {
        var threadId = data.RequireSnowflake("id");
        var guildId = data.RequireSnowflake("guild_id");

        return new ThreadMembersUpdatedEvent(
            DiscordChannel.Partial(threadId, guildId, isThread: true),
            DiscordGuild.Partial(guildId),
            data.Int32OrNull("member_count") ?? 0,
            StampThreadMembers(data, "added_members", guildId, threadId),
            [.. ReadSnowflakes(data, "removed_member_ids").Select(DiscordUser.Partial)]);
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

        return new GuildStickersUpdatedEvent(DiscordGuild.Partial(guildId), stickers);
    }

    private static AuditLogEntryCreatedEvent ReadAuditLogEntry(JsonElement data, string name)
    {
        var entry = RequireBody<DiscordAuditLogEntry>(data, name);
        var guildId = entry.GuildId ?? data.SnowflakeOrNull("guild_id");

        if (entry.GuildId is null && guildId is { } owner)
            entry = entry.In(owner);

        return new AuditLogEntryCreatedEvent(
            entry,
            guildId is { } id ? DiscordGuild.Partial(id) : null,
            entry.UserId is { } userId ? DiscordUser.Partial(userId) : null);
    }

    private static DiscordIntegration ReadIntegration(JsonElement data, string name)
    {
        var integration = RequireBody<DiscordIntegration>(data, name);

        return data.SnowflakeOrNull("guild_id") is { } guildId ? integration.In(guildId) : integration;
    }

    private static DiscordEvent ReadScheduledEvent(JsonElement data, string name,
        Func<DiscordScheduledEvent, DiscordGuild?, DiscordChannel?, DiscordEvent> build)
    {
        var scheduled = RequireBody<DiscordScheduledEvent>(data, name);
        var guildId = scheduled.GuildId ?? data.SnowflakeOrNull("guild_id");

        if (scheduled.GuildId is null && guildId is { } owner)
            scheduled = scheduled.In(owner);

        return build(
            scheduled,
            guildId is { } id ? DiscordGuild.Partial(id) : null,
            scheduled.ChannelId is { } channelId ? DiscordChannel.Partial(channelId, guildId) : null);
    }

    private static DiscordEvent ReadStageInstance(JsonElement data, string name,
        Func<DiscordStageInstance, DiscordChannel, DiscordGuild?, DiscordEvent> build)
    {
        var instance = RequireBody<DiscordStageInstance>(data, name);
        var guildId = instance.GuildId ?? data.SnowflakeOrNull("guild_id");

        if (instance.GuildId is null && guildId is { } owner)
            instance = instance.In(owner);

        return build(
            instance,
            DiscordChannel.Partial(instance.ChannelId, guildId),
            guildId is { } id ? DiscordGuild.Partial(id) : null);
    }

    private static DiscordEvent ReadEntitlement(JsonElement data, string name,
        Func<DiscordEntitlement, DiscordUser?, DiscordGuild?, DiscordEvent> build)
    {
        var entitlement = RequireBody<DiscordEntitlement>(data, name);

        return build(
            entitlement,
            entitlement.UserId is { } userId ? DiscordUser.Partial(userId) : null,
            entitlement.GuildId is { } guildId ? DiscordGuild.Partial(guildId) : null);
    }

    private static CommandPermissionsUpdatedEvent ReadCommandPermissions(JsonElement data, string name)
    {
        var permissions = RequireBody<DiscordCommandPermissions>(data, name);

        return new CommandPermissionsUpdatedEvent(permissions, DiscordGuild.Partial(permissions.GuildId));
    }

    private static AutoModerationActionExecutedEvent ReadAutoModerationExecution(JsonElement data, string name)
    {
        var guildId = data.RequireSnowflake("guild_id");
        var channelId = data.SnowflakeOrNull("channel_id");

        return new AutoModerationActionExecutedEvent(
            DiscordGuild.Partial(guildId),
            DiscordAutoModerationRule.Partial(data.RequireSnowflake("rule_id"), guildId),
            Require<AutoModerationAction>(data, "action", name),
            DiscordUser.Partial(data.RequireSnowflake("user_id")))
        {
            TriggerType = (AutoModerationTriggerType)(data.Int32OrNull("rule_trigger_type") ?? 1),
            Channel = channelId is { } id ? DiscordChannel.Partial(id, guildId) : null,
            Message = data.SnowflakeOrNull("message_id") is { } messageId && channelId is { } owner
                ? DiscordMessage.Partial(messageId, owner, guildId)
                : null,
            AlertMessage = data.SnowflakeOrNull("alert_system_message_id") is { } alertId && channelId is { } source
                ? DiscordMessage.Partial(alertId, source, guildId)
                : null,
            Content = data.StringOrNull("content"),
            MatchedKeyword = data.StringOrNull("matched_keyword"),
            MatchedContent = data.StringOrNull("matched_content")
        };
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

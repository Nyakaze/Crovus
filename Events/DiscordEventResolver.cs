using Crovus.Cache;
using Crovus.Logs;
using Crovus.Models;

namespace Crovus.Events;

public sealed class DiscordEventResolver
{
    private const string LogCategory = "Client.Events.Resolve";

    private readonly IDiscordCache _cache;
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;

    private int _requested;
    private int _resolved;

    public DiscordEventResolver(IDiscordCache cache, ILogger? logger = null, ITelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;
    }

    public DiscordEventResolver(IDiscordCache cache, DiagnosticsHub diagnostics)
        : this(cache, diagnostics, diagnostics)
    {
    }

    public async ValueTask<DiscordEvent> ResolveAsync(DiscordEvent discordEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(discordEvent);

        if (_cache is NullDiscordCache)
            return discordEvent;

        var before = (Volatile.Read(ref _requested), Volatile.Read(ref _resolved));

        try
        {
            var resolved = await ExpandAsync(discordEvent, cancellationToken);

            if (_telemetry.HasSubscribers)
            {
                var requested = Volatile.Read(ref _requested) - before.Item1;

                if (requested > 0)
                    _telemetry.Emit(new EventEntitiesResolved(discordEvent.Name, requested,
                        Volatile.Read(ref _resolved) - before.Item2));
            }

            return resolved;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning($"Could not resolve entities for {discordEvent.Name}", exception);
            _telemetry.Emit(new EventResolveFailed(discordEvent.Name, exception.GetType().Name));

            return discordEvent;
        }
    }

    private async ValueTask<DiscordEvent> ExpandAsync(DiscordEvent discordEvent, CancellationToken token) =>
        discordEvent switch
        {
            MessageCreatedEvent message => message with
            {
                Channel = await ChannelAsync(message.Channel, token),
                Guild = await GuildAsync(message.Guild, token),
                Member = message.Member ?? await MemberAsync(message.Guild?.Id, message.Author.Id, token)
            },

            MessageUpdatedEvent message => message with
            {
                Message = await MessageAsync(message.Message, token),
                Channel = await ChannelAsync(message.Channel, token),
                Guild = await GuildAsync(message.Guild, token),
                Previous = message.Previous ?? await CachedMessageAsync(message.Message.Id, token)
            },

            MessageDeletedEvent message => message with
            {
                Message = await MessageAsync(message.Message, token),
                Channel = await ChannelAsync(message.Channel, token),
                Guild = await GuildAsync(message.Guild, token)
            },

            MessagesBulkDeletedEvent bulk => bulk with
            {
                Messages = await MessagesAsync(bulk.Messages, token),
                Channel = await ChannelAsync(bulk.Channel, token),
                Guild = await GuildAsync(bulk.Guild, token)
            },

            ReactionAddedEvent reaction => reaction with
            {
                Message = await MessageAsync(reaction.Message, token),
                Channel = await ChannelAsync(reaction.Channel, token),
                User = await UserAsync(reaction.User, token),
                Guild = await GuildAsync(reaction.Guild, token),
                Member = reaction.Member ?? await MemberAsync(reaction.Guild?.Id, reaction.User.Id, token)
            },

            ReactionRemovedEvent reaction => reaction with
            {
                Message = await MessageAsync(reaction.Message, token),
                Channel = await ChannelAsync(reaction.Channel, token),
                User = await UserAsync(reaction.User, token),
                Guild = await GuildAsync(reaction.Guild, token),
                Member = reaction.Member ?? await MemberAsync(reaction.Guild?.Id, reaction.User.Id, token)
            },

            ReactionsClearedEvent reaction => reaction with
            {
                Message = await MessageAsync(reaction.Message, token),
                Channel = await ChannelAsync(reaction.Channel, token),
                Guild = await GuildAsync(reaction.Guild, token)
            },

            ReactionEmojiClearedEvent reaction => reaction with
            {
                Message = await MessageAsync(reaction.Message, token),
                Channel = await ChannelAsync(reaction.Channel, token),
                Guild = await GuildAsync(reaction.Guild, token)
            },

            ChannelCreatedEvent channel => channel with { Guild = await GuildAsync(channel.Guild, token) },

            ChannelUpdatedEvent channel => channel with
            {
                Guild = await GuildAsync(channel.Guild, token),
                Previous = channel.Previous ?? await CachedChannelAsync(channel.Channel.Id, token)
            },

            ChannelDeletedEvent channel => channel with
            {
                Channel = await ChannelAsync(channel.Channel, token),
                Guild = await GuildAsync(channel.Guild, token)
            },

            ThreadCreatedEvent thread => thread with
            {
                Parent = await ChannelAsync(thread.Parent, token),
                Guild = await GuildAsync(thread.Guild, token)
            },

            ThreadUpdatedEvent thread => thread with
            {
                Parent = await ChannelAsync(thread.Parent, token),
                Guild = await GuildAsync(thread.Guild, token)
            },

            ThreadDeletedEvent thread => thread with
            {
                Thread = await ChannelAsync(thread.Thread, token),
                Parent = await ChannelAsync(thread.Parent, token),
                Guild = await GuildAsync(thread.Guild, token)
            },

            GuildUnavailableEvent guild => guild with { Guild = await GuildAsync(guild.Guild, token) },

            GuildUpdatedEvent guild => guild with
            {
                Previous = guild.Previous ?? await CachedGuildAsync(guild.Guild.Id, token)
            },

            MemberJoinedEvent member => member with { Guild = await GuildAsync(member.Guild, token) },

            MemberUpdatedEvent member => member with
            {
                Guild = await GuildAsync(member.Guild, token),
                Previous = member.Previous ?? await MemberAsync(member.Guild.Id, member.User.Id, token)
            },

            MemberLeftEvent member => member with
            {
                Guild = await GuildAsync(member.Guild, token),
                Member = member.Member ?? await MemberAsync(member.Guild.Id, member.User.Id, token)
            },

            RoleCreatedEvent role => role with { Guild = await GuildAsync(role.Guild, token) },

            RoleUpdatedEvent role => role with
            {
                Guild = await GuildAsync(role.Guild, token),
                Previous = role.Previous ?? await CachedRoleAsync(role.Guild.Id, role.Role.Id, token)
            },

            RoleDeletedEvent role => role with
            {
                Guild = await GuildAsync(role.Guild, token),
                Role = await RoleAsync(role.Guild.Id, role.Role, token)
            },

            BanAddedEvent ban => ban with
            {
                Guild = await GuildAsync(ban.Guild, token),
                Member = ban.Member ?? await MemberAsync(ban.Guild.Id, ban.User.Id, token)
            },

            BanRemovedEvent ban => ban with { Guild = await GuildAsync(ban.Guild, token) },

            WebhooksUpdatedEvent webhooks => webhooks with
            {
                Channel = await ChannelAsync(webhooks.Channel, token),
                Guild = await GuildAsync(webhooks.Guild, token)
            },

            TypingStartedEvent typing => typing with
            {
                Channel = await ChannelAsync(typing.Channel, token),
                User = await UserAsync(typing.User, token),
                Guild = await GuildAsync(typing.Guild, token),
                Member = typing.Member ?? await MemberAsync(typing.Guild?.Id, typing.User.Id, token)
            },

            InteractionCreatedEvent interaction => interaction with
            {
                Channel = await ChannelAsync(interaction.Channel, token),
                Guild = await GuildAsync(interaction.Guild, token)
            },

            PresenceUpdatedEvent presence => presence with
            {
                Guild = await GuildAsync(presence.Guild, token),
                User = await UserAsync(presence.User, token)
            },

            VoiceStateUpdatedEvent voice => voice with
            {
                User = await UserAsync(voice.User, token),
                Guild = await GuildAsync(voice.Guild, token),
                Channel = await ChannelAsync(voice.Channel, token),
                PreviousChannel = await ChannelAsync(PartialChannel(voice.Previous), token)
            },

            VoiceServerUpdatedEvent server => server with { Guild = await GuildAsync(server.Guild, token) },

            GuildMembersChunkEvent chunk => chunk with
            {
                Guild = await GuildAsync(chunk.Guild, token),
                NotFound = await UsersAsync(chunk.NotFound, token)
            },

            ThreadListSyncEvent sync => sync with
            {
                Guild = await GuildAsync(sync.Guild, token),
                Channels = await ChannelsAsync(sync.Channels, token)
            },

            ThreadMemberUpdatedEvent member => member with
            {
                Thread = await ChannelAsync(member.Thread, token),
                Guild = await GuildAsync(member.Guild, token)
            },

            ThreadMembersUpdatedEvent members => members with
            {
                Thread = await ChannelAsync(members.Thread, token),
                Guild = await GuildAsync(members.Guild, token),
                Removed = await UsersAsync(members.Removed, token)
            },

            ChannelPinsUpdatedEvent pins => pins with
            {
                Channel = await ChannelAsync(pins.Channel, token),
                Guild = await GuildAsync(pins.Guild, token)
            },

            InviteCreatedEvent invite => invite with
            {
                Channel = await ChannelAsync(invite.Channel, token),
                Guild = await GuildAsync(invite.Guild, token)
            },

            InviteDeletedEvent invite => invite with
            {
                Channel = await ChannelAsync(invite.Channel, token),
                Guild = await GuildAsync(invite.Guild, token)
            },

            UserUpdatedEvent user => user with
            {
                Previous = user.Previous ?? await CachedUserAsync(user.User.Id, token)
            },

            GuildEmojisUpdatedEvent emojis => emojis with { Guild = await GuildAsync(emojis.Guild, token) },

            GuildStickersUpdatedEvent stickers => stickers with { Guild = await GuildAsync(stickers.Guild, token) },

            AuditLogEntryCreatedEvent audit => audit with
            {
                Guild = await GuildAsync(audit.Guild, token),
                User = await UserAsync(audit.User, token)
            },

            PollVoteAddedEvent vote => vote with
            {
                User = await UserAsync(vote.User, token),
                Channel = await ChannelAsync(vote.Channel, token),
                Message = await MessageAsync(vote.Message, token),
                Guild = await GuildAsync(vote.Guild, token)
            },

            PollVoteRemovedEvent vote => vote with
            {
                User = await UserAsync(vote.User, token),
                Channel = await ChannelAsync(vote.Channel, token),
                Message = await MessageAsync(vote.Message, token),
                Guild = await GuildAsync(vote.Guild, token)
            },

            AutoModerationRuleCreatedEvent rule => rule with { Guild = await GuildAsync(rule.Guild, token) },

            AutoModerationRuleUpdatedEvent rule => rule with { Guild = await GuildAsync(rule.Guild, token) },

            AutoModerationRuleDeletedEvent rule => rule with { Guild = await GuildAsync(rule.Guild, token) },

            AutoModerationActionExecutedEvent execution => execution with
            {
                Guild = await GuildAsync(execution.Guild, token),
                User = await UserAsync(execution.User, token),
                Channel = await ChannelAsync(execution.Channel, token),
                Message = await MessageAsync(execution.Message, token),
                AlertMessage = await MessageAsync(execution.AlertMessage, token),
                Member = execution.Member ?? await MemberAsync(execution.Guild.Id, execution.User.Id, token)
            },

            ScheduledEventCreatedEvent scheduled => scheduled with
            {
                Guild = await GuildAsync(scheduled.Guild, token),
                Channel = await ChannelAsync(scheduled.Channel, token)
            },

            ScheduledEventUpdatedEvent scheduled => scheduled with
            {
                Guild = await GuildAsync(scheduled.Guild, token),
                Channel = await ChannelAsync(scheduled.Channel, token)
            },

            ScheduledEventDeletedEvent scheduled => scheduled with
            {
                Guild = await GuildAsync(scheduled.Guild, token),
                Channel = await ChannelAsync(scheduled.Channel, token)
            },

            ScheduledEventUserAddedEvent scheduled => scheduled with
            {
                Guild = await GuildAsync(scheduled.Guild, token),
                User = await UserAsync(scheduled.User, token),
                Member = scheduled.Member ?? await MemberAsync(scheduled.Guild.Id, scheduled.User.Id, token)
            },

            ScheduledEventUserRemovedEvent scheduled => scheduled with
            {
                Guild = await GuildAsync(scheduled.Guild, token),
                User = await UserAsync(scheduled.User, token),
                Member = scheduled.Member ?? await MemberAsync(scheduled.Guild.Id, scheduled.User.Id, token)
            },

            StageInstanceCreatedEvent stage => stage with
            {
                Channel = await ChannelAsync(stage.Channel, token),
                Guild = await GuildAsync(stage.Guild, token)
            },

            StageInstanceUpdatedEvent stage => stage with
            {
                Channel = await ChannelAsync(stage.Channel, token),
                Guild = await GuildAsync(stage.Guild, token)
            },

            StageInstanceDeletedEvent stage => stage with
            {
                Channel = await ChannelAsync(stage.Channel, token),
                Guild = await GuildAsync(stage.Guild, token)
            },

            IntegrationCreatedEvent integration => integration with
            {
                Guild = await GuildAsync(integration.Guild, token)
            },

            IntegrationUpdatedEvent integration => integration with
            {
                Guild = await GuildAsync(integration.Guild, token)
            },

            IntegrationDeletedEvent integration => integration with
            {
                Guild = await GuildAsync(integration.Guild, token)
            },

            GuildIntegrationsUpdatedEvent integrations => integrations with
            {
                Guild = await GuildAsync(integrations.Guild, token)
            },

            EntitlementCreatedEvent entitlement => entitlement with
            {
                User = await UserAsync(entitlement.User, token),
                Guild = await GuildAsync(entitlement.Guild, token)
            },

            EntitlementUpdatedEvent entitlement => entitlement with
            {
                User = await UserAsync(entitlement.User, token),
                Guild = await GuildAsync(entitlement.Guild, token)
            },

            EntitlementDeletedEvent entitlement => entitlement with
            {
                User = await UserAsync(entitlement.User, token),
                Guild = await GuildAsync(entitlement.Guild, token)
            },

            CommandPermissionsUpdatedEvent permissions => permissions with
            {
                Guild = await GuildAsync(permissions.Guild, token)
            },

            _ => discordEvent
        };

    private static DiscordChannel? PartialChannel(DiscordVoiceState? state) =>
        state?.ChannelId is { } channelId ? DiscordChannel.Partial(channelId, state.GuildId) : null;

    private async ValueTask<DiscordGuild?> GuildAsync(DiscordGuild? guild, CancellationToken token)
    {
        if (guild is not { IsPartial: true })
            return guild;

        Interlocked.Increment(ref _requested);

        if (await _cache.GetGuildAsync(guild.Id, token) is not { } cached)
            return guild;

        Interlocked.Increment(ref _resolved);

        return cached;
    }

    private async ValueTask<DiscordChannel?> ChannelAsync(DiscordChannel? channel, CancellationToken token)
    {
        if (channel is not { IsPartial: true })
            return channel;

        Interlocked.Increment(ref _requested);

        if (await _cache.GetChannelAsync(channel.Id, token) is not { } cached)
            return channel;

        Interlocked.Increment(ref _resolved);

        return channel.GuildId is { } guildId ? cached.In(guildId) : cached;
    }

    private async ValueTask<DiscordMessage?> MessageAsync(DiscordMessage? message, CancellationToken token)
    {
        if (message is not { IsPartial: true })
            return message;

        Interlocked.Increment(ref _requested);

        if (await _cache.GetMessageAsync(message.Id, token) is not { } cached)
            return message;

        Interlocked.Increment(ref _resolved);

        return message.GuildId is { } guildId ? cached.In(guildId) : cached;
    }

    private async ValueTask<DiscordUser?> UserAsync(DiscordUser? user, CancellationToken token)
    {
        if (user is not { IsPartial: true })
            return user;

        Interlocked.Increment(ref _requested);

        if (await _cache.GetUserAsync(user.Id, token) is not { } cached)
            return user;

        Interlocked.Increment(ref _resolved);

        return cached;
    }

    private async ValueTask<DiscordRole> RoleAsync(Snowflake guildId, DiscordRole role, CancellationToken token)
    {
        if (!role.IsPartial)
            return role;

        Interlocked.Increment(ref _requested);

        if (await CachedRoleAsync(guildId, role.Id, token) is not { } cached)
            return role;

        Interlocked.Increment(ref _resolved);

        return cached;
    }

    private async ValueTask<DiscordMember?> MemberAsync(Snowflake? guildId, Snowflake userId,
        CancellationToken token)
    {
        if (guildId is not { } owner)
            return null;

        Interlocked.Increment(ref _requested);

        if (await _cache.GetMemberAsync(owner, userId, token) is not { } cached)
            return null;

        Interlocked.Increment(ref _resolved);

        return cached;
    }

    private async ValueTask<IReadOnlyList<DiscordMessage>> MessagesAsync(IReadOnlyList<DiscordMessage> messages,
        CancellationToken token)
    {
        if (messages.Count == 0)
            return messages;

        var resolved = new DiscordMessage[messages.Count];

        for (var index = 0; index < messages.Count; index++)
            resolved[index] = await MessageAsync(messages[index], token) ?? messages[index];

        return resolved;
    }

    private async ValueTask<IReadOnlyList<DiscordChannel>> ChannelsAsync(IReadOnlyList<DiscordChannel> channels,
        CancellationToken token)
    {
        if (channels.Count == 0)
            return channels;

        var resolved = new DiscordChannel[channels.Count];

        for (var index = 0; index < channels.Count; index++)
            resolved[index] = await ChannelAsync(channels[index], token) ?? channels[index];

        return resolved;
    }

    private async ValueTask<IReadOnlyList<DiscordUser>> UsersAsync(IReadOnlyList<DiscordUser> users,
        CancellationToken token)
    {
        if (users.Count == 0)
            return users;

        var resolved = new DiscordUser[users.Count];

        for (var index = 0; index < users.Count; index++)
            resolved[index] = await UserAsync(users[index], token) ?? users[index];

        return resolved;
    }

    private ValueTask<DiscordGuild?> CachedGuildAsync(Snowflake guildId, CancellationToken token) =>
        _cache.GetGuildAsync(guildId, token);

    private ValueTask<DiscordChannel?> CachedChannelAsync(Snowflake channelId, CancellationToken token) =>
        _cache.GetChannelAsync(channelId, token);

    private ValueTask<DiscordMessage?> CachedMessageAsync(Snowflake messageId, CancellationToken token) =>
        _cache.GetMessageAsync(messageId, token);

    private ValueTask<DiscordUser?> CachedUserAsync(Snowflake userId, CancellationToken token) =>
        _cache.GetUserAsync(userId, token);

    private async ValueTask<DiscordRole?> CachedRoleAsync(Snowflake guildId, Snowflake roleId,
        CancellationToken token)
    {
        if (await _cache.GetGuildRolesAsync(guildId, token) is not { } roles)
            return null;

        return roles.FirstOrDefault(role => role.Id == roleId);
    }
}

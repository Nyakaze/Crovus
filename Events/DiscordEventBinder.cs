using Crovus.Client;
using Crovus.Models;

namespace Crovus.Events;

public static class DiscordEventBinder
{
    public static DiscordEvent Bind(DiscordEvent discordEvent, ICrovusContext context)
    {
        ArgumentNullException.ThrowIfNull(discordEvent);
        ArgumentNullException.ThrowIfNull(context);

        return discordEvent switch
        {
            ReadyEvent ready => ready with
            {
                User = ready.User.Bind(context),
                Guilds = EntityBinder.BindAll(ready.Guilds, context)
            },

            MessageCreatedEvent message => message with
            {
                Message = message.Message.Bind(context),
                Channel = message.Channel.Bind(context),
                Guild = message.Guild?.Bind(context),
                Member = message.Member?.Bind(context)
            },

            MessageUpdatedEvent message => message with
            {
                Message = message.Message.Bind(context),
                Channel = message.Channel.Bind(context),
                Guild = message.Guild?.Bind(context),
                Previous = message.Previous?.Bind(context)
            },

            MessageDeletedEvent message => message with
            {
                Message = message.Message.Bind(context),
                Channel = message.Channel.Bind(context),
                Guild = message.Guild?.Bind(context)
            },

            MessagesBulkDeletedEvent messages => messages with
            {
                Messages = EntityBinder.BindAll(messages.Messages, context),
                Channel = messages.Channel.Bind(context),
                Guild = messages.Guild?.Bind(context)
            },

            ReactionAddedEvent reaction => reaction with
            {
                Message = reaction.Message.Bind(context),
                Channel = reaction.Channel.Bind(context),
                User = reaction.User.Bind(context),
                Guild = reaction.Guild?.Bind(context),
                Member = reaction.Member?.Bind(context)
            },

            ReactionRemovedEvent reaction => reaction with
            {
                Message = reaction.Message.Bind(context),
                Channel = reaction.Channel.Bind(context),
                User = reaction.User.Bind(context),
                Guild = reaction.Guild?.Bind(context),
                Member = reaction.Member?.Bind(context)
            },

            ReactionsClearedEvent reaction => reaction with
            {
                Message = reaction.Message.Bind(context),
                Channel = reaction.Channel.Bind(context),
                Guild = reaction.Guild?.Bind(context)
            },

            ReactionEmojiClearedEvent reaction => reaction with
            {
                Message = reaction.Message.Bind(context),
                Channel = reaction.Channel.Bind(context),
                Guild = reaction.Guild?.Bind(context)
            },

            ChannelCreatedEvent channel => channel with
            {
                Channel = channel.Channel.Bind(context),
                Guild = channel.Guild?.Bind(context)
            },

            ChannelUpdatedEvent channel => channel with
            {
                Channel = channel.Channel.Bind(context),
                Guild = channel.Guild?.Bind(context),
                Previous = channel.Previous?.Bind(context)
            },

            ChannelDeletedEvent channel => channel with
            {
                Channel = channel.Channel.Bind(context),
                Guild = channel.Guild?.Bind(context)
            },

            ThreadCreatedEvent thread => thread with
            {
                Thread = thread.Thread.Bind(context),
                Parent = thread.Parent?.Bind(context),
                Guild = thread.Guild?.Bind(context)
            },

            ThreadUpdatedEvent thread => thread with
            {
                Thread = thread.Thread.Bind(context),
                Parent = thread.Parent?.Bind(context),
                Guild = thread.Guild?.Bind(context)
            },

            ThreadDeletedEvent thread => thread with
            {
                Thread = thread.Thread.Bind(context),
                Parent = thread.Parent?.Bind(context),
                Guild = thread.Guild?.Bind(context)
            },

            GuildAvailableEvent guild => guild with
            {
                Guild = guild.Guild.Bind(context),
                Channels = EntityBinder.BindAll(guild.Channels, context),
                Presences = EntityBinder.BindAll(guild.Presences, context),
                VoiceStates = EntityBinder.BindAll(guild.VoiceStates, context),
                Threads = EntityBinder.BindAll(guild.Threads, context),
                Stickers = EntityBinder.BindAll(guild.Stickers, context),
                ScheduledEvents = EntityBinder.BindAll(guild.ScheduledEvents, context),
                StageInstances = EntityBinder.BindAll(guild.StageInstances, context),
                Members = EntityBinder.BindAll(guild.Members, context)
            },

            GuildUpdatedEvent guild => guild with
            {
                Guild = guild.Guild.Bind(context),
                Previous = guild.Previous?.Bind(context)
            },

            GuildUnavailableEvent guild => guild with { Guild = guild.Guild.Bind(context) },

            MemberJoinedEvent member => member with
            {
                Guild = member.Guild.Bind(context),
                Member = member.Member.Bind(context)
            },

            MemberUpdatedEvent member => member with
            {
                Guild = member.Guild.Bind(context),
                Member = member.Member.Bind(context),
                Previous = member.Previous?.Bind(context)
            },

            MemberLeftEvent member => member with
            {
                Guild = member.Guild.Bind(context),
                User = member.User.Bind(context),
                Member = member.Member?.Bind(context)
            },

            RoleCreatedEvent role => role with
            {
                Guild = role.Guild.Bind(context),
                Role = role.Role.Bind(context)
            },

            RoleUpdatedEvent role => role with
            {
                Guild = role.Guild.Bind(context),
                Role = role.Role.Bind(context),
                Previous = role.Previous?.Bind(context)
            },

            RoleDeletedEvent role => role with
            {
                Guild = role.Guild.Bind(context),
                Role = role.Role.Bind(context)
            },

            BanAddedEvent ban => ban with
            {
                Guild = ban.Guild.Bind(context),
                User = ban.User.Bind(context),
                Member = ban.Member?.Bind(context)
            },

            BanRemovedEvent ban => ban with
            {
                Guild = ban.Guild.Bind(context),
                User = ban.User.Bind(context)
            },

            WebhooksUpdatedEvent webhooks => webhooks with
            {
                Channel = webhooks.Channel.Bind(context),
                Guild = webhooks.Guild?.Bind(context)
            },

            TypingStartedEvent typing => typing with
            {
                Channel = typing.Channel.Bind(context),
                User = typing.User.Bind(context),
                Guild = typing.Guild?.Bind(context),
                Member = typing.Member?.Bind(context)
            },

            InteractionCreatedEvent interaction => interaction with
            {
                Interaction = interaction.Interaction.Bind(context),
                Channel = interaction.Channel?.Bind(context),
                Guild = interaction.Guild?.Bind(context)
            },

            PresenceUpdatedEvent presence => presence with
            {
                Presence = presence.Presence.Bind(context),
                Previous = presence.Previous?.Bind(context),
                Guild = presence.Guild?.Bind(context),
                User = presence.User.Bind(context)
            },

            VoiceStateUpdatedEvent voice => voice with
            {
                VoiceState = voice.VoiceState.Bind(context),
                User = voice.User.Bind(context),
                Guild = voice.Guild?.Bind(context),
                Previous = voice.Previous?.Bind(context),
                Channel = voice.Channel?.Bind(context),
                PreviousChannel = voice.PreviousChannel?.Bind(context)
            },

            VoiceServerUpdatedEvent voice => voice with { Guild = voice.Guild.Bind(context) },

            GuildMembersChunkEvent chunk => chunk with
            {
                Guild = chunk.Guild.Bind(context),
                Members = EntityBinder.BindAll(chunk.Members, context),
                NotFound = EntityBinder.BindAll(chunk.NotFound, context),
                Presences = EntityBinder.BindAll(chunk.Presences, context)
            },

            ThreadListSyncEvent sync => sync with
            {
                Guild = sync.Guild.Bind(context),
                Threads = EntityBinder.BindAll(sync.Threads, context),
                Members = EntityBinder.BindAll(sync.Members, context),
                Channels = EntityBinder.BindAll(sync.Channels, context)
            },

            ThreadMemberUpdatedEvent member => member with
            {
                Member = member.Member.Bind(context),
                Thread = member.Thread?.Bind(context),
                Guild = member.Guild?.Bind(context)
            },

            ThreadMembersUpdatedEvent members => members with
            {
                Thread = members.Thread.Bind(context),
                Guild = members.Guild.Bind(context),
                Added = EntityBinder.BindAll(members.Added, context),
                Removed = EntityBinder.BindAll(members.Removed, context)
            },

            ChannelPinsUpdatedEvent pins => pins with
            {
                Channel = pins.Channel.Bind(context),
                Guild = pins.Guild?.Bind(context)
            },

            InviteCreatedEvent invite => invite with
            {
                Invite = invite.Invite.Bind(context),
                Channel = invite.Channel.Bind(context),
                Guild = invite.Guild?.Bind(context)
            },

            InviteDeletedEvent invite => invite with
            {
                Invite = invite.Invite.Bind(context),
                Channel = invite.Channel.Bind(context),
                Guild = invite.Guild?.Bind(context)
            },

            UserUpdatedEvent user => user with
            {
                User = user.User.Bind(context),
                Previous = user.Previous?.Bind(context)
            },

            GuildEmojisUpdatedEvent emojis => emojis with
            {
                Guild = emojis.Guild.Bind(context),
                Emojis = EntityBinder.BindAll(emojis.Emojis, context)
            },

            GuildStickersUpdatedEvent stickers => stickers with
            {
                Guild = stickers.Guild.Bind(context),
                Stickers = EntityBinder.BindAll(stickers.Stickers, context)
            },

            AuditLogEntryCreatedEvent entry => entry with
            {
                Entry = entry.Entry.Bind(context),
                Guild = entry.Guild?.Bind(context),
                User = entry.User?.Bind(context)
            },

            PollVoteAddedEvent vote => vote with
            {
                User = vote.User.Bind(context),
                Channel = vote.Channel.Bind(context),
                Message = vote.Message.Bind(context),
                Guild = vote.Guild?.Bind(context)
            },

            PollVoteRemovedEvent vote => vote with
            {
                User = vote.User.Bind(context),
                Channel = vote.Channel.Bind(context),
                Message = vote.Message.Bind(context),
                Guild = vote.Guild?.Bind(context)
            },

            AutoModerationRuleCreatedEvent rule => rule with
            {
                Rule = rule.Rule.Bind(context),
                Guild = rule.Guild?.Bind(context)
            },

            AutoModerationRuleUpdatedEvent rule => rule with
            {
                Rule = rule.Rule.Bind(context),
                Guild = rule.Guild?.Bind(context)
            },

            AutoModerationRuleDeletedEvent rule => rule with
            {
                Rule = rule.Rule.Bind(context),
                Guild = rule.Guild?.Bind(context)
            },

            AutoModerationActionExecutedEvent action => action with
            {
                Guild = action.Guild.Bind(context),
                Rule = action.Rule.Bind(context),
                User = action.User.Bind(context),
                Channel = action.Channel?.Bind(context),
                Message = action.Message?.Bind(context),
                AlertMessage = action.AlertMessage?.Bind(context),
                Member = action.Member?.Bind(context)
            },

            ScheduledEventCreatedEvent scheduled => scheduled with
            {
                ScheduledEvent = scheduled.ScheduledEvent.Bind(context),
                Guild = scheduled.Guild?.Bind(context),
                Channel = scheduled.Channel?.Bind(context)
            },

            ScheduledEventUpdatedEvent scheduled => scheduled with
            {
                ScheduledEvent = scheduled.ScheduledEvent.Bind(context),
                Guild = scheduled.Guild?.Bind(context),
                Channel = scheduled.Channel?.Bind(context)
            },

            ScheduledEventDeletedEvent scheduled => scheduled with
            {
                ScheduledEvent = scheduled.ScheduledEvent.Bind(context),
                Guild = scheduled.Guild?.Bind(context),
                Channel = scheduled.Channel?.Bind(context)
            },

            ScheduledEventUserAddedEvent scheduled => scheduled with
            {
                ScheduledEvent = scheduled.ScheduledEvent.Bind(context),
                User = scheduled.User.Bind(context),
                Guild = scheduled.Guild.Bind(context),
                Member = scheduled.Member?.Bind(context)
            },

            ScheduledEventUserRemovedEvent scheduled => scheduled with
            {
                ScheduledEvent = scheduled.ScheduledEvent.Bind(context),
                User = scheduled.User.Bind(context),
                Guild = scheduled.Guild.Bind(context),
                Member = scheduled.Member?.Bind(context)
            },

            StageInstanceCreatedEvent stage => stage with
            {
                Instance = stage.Instance.Bind(context),
                Channel = stage.Channel.Bind(context),
                Guild = stage.Guild?.Bind(context)
            },

            StageInstanceUpdatedEvent stage => stage with
            {
                Instance = stage.Instance.Bind(context),
                Channel = stage.Channel.Bind(context),
                Guild = stage.Guild?.Bind(context)
            },

            StageInstanceDeletedEvent stage => stage with
            {
                Instance = stage.Instance.Bind(context),
                Channel = stage.Channel.Bind(context),
                Guild = stage.Guild?.Bind(context)
            },

            IntegrationCreatedEvent integration => integration with
            {
                Integration = integration.Integration.Bind(context),
                Guild = integration.Guild?.Bind(context)
            },

            IntegrationUpdatedEvent integration => integration with
            {
                Integration = integration.Integration.Bind(context),
                Guild = integration.Guild?.Bind(context)
            },

            IntegrationDeletedEvent integration => integration with
            {
                Integration = integration.Integration.Bind(context),
                Guild = integration.Guild.Bind(context)
            },

            GuildIntegrationsUpdatedEvent integrations => integrations with
            {
                Guild = integrations.Guild.Bind(context)
            },

            EntitlementCreatedEvent entitlement => entitlement with
            {
                Entitlement = entitlement.Entitlement.Bind(context),
                User = entitlement.User?.Bind(context),
                Guild = entitlement.Guild?.Bind(context)
            },

            EntitlementUpdatedEvent entitlement => entitlement with
            {
                Entitlement = entitlement.Entitlement.Bind(context),
                User = entitlement.User?.Bind(context),
                Guild = entitlement.Guild?.Bind(context)
            },

            EntitlementDeletedEvent entitlement => entitlement with
            {
                Entitlement = entitlement.Entitlement.Bind(context),
                User = entitlement.User?.Bind(context),
                Guild = entitlement.Guild?.Bind(context)
            },

            CommandPermissionsUpdatedEvent permissions => permissions with
            {
                Guild = permissions.Guild?.Bind(context)
            },

            _ => discordEvent
        };
    }
}

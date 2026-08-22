using Crovus.Factory;
using Crovus.Services;

namespace Crovus.Models;

public static class GuildFluent
{
    public static Task<DiscordGuild> RefreshAsync(this DiscordGuild guild, bool withCounts = false,
        CancellationToken cancellationToken = default) =>
        guild.Services().Guilds.GetAsync(guild.Id, withCounts, cancellationToken);

    public static Task<DiscordGuild> ModifyAsync(this DiscordGuild guild, GuildModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        guild.Rest().ModifyGuildAsync(guild.Id, request, reason, cancellationToken);

    public static Task LeaveAsync(this DiscordGuild guild, CancellationToken cancellationToken = default) =>
        guild.Rest().LeaveGuildAsync(guild.Id, cancellationToken);

    public static Task<IReadOnlyList<DiscordChannel>> GetChannelsAsync(this DiscordGuild guild,
        CancellationToken cancellationToken = default) =>
        guild.Services().Guilds.GetChannelsAsync(guild.Id, cancellationToken);

    public static Task<DiscordChannel?> FindChannelAsync(this DiscordGuild guild, string name,
        CancellationToken cancellationToken = default) =>
        guild.Services().Guilds.FindChannelAsync(guild.Id, name, cancellationToken);

    public static Task<DiscordChannel> CreateChannelAsync(this DiscordGuild guild, ChannelCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        guild.Services().Channels.CreateAsync(guild.Id, request, reason, cancellationToken);

    public static Task<DiscordChannel> CreateChannelAsync(this DiscordGuild guild, ChannelFactory channel,
        string? reason = null, CancellationToken cancellationToken = default) =>
        guild.Services().Channels.CreateAsync(guild.Id, channel, reason, cancellationToken);

    public static Task<DiscordChannel> CreateTextChannelAsync(this DiscordGuild guild, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Channels.CreateTextAsync(guild.Id, name, configure, reason, cancellationToken);

    public static Task<DiscordChannel> CreateVoiceChannelAsync(this DiscordGuild guild, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Channels.CreateVoiceAsync(guild.Id, name, configure, reason, cancellationToken);

    public static Task<DiscordChannel> CreateCategoryAsync(this DiscordGuild guild, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Channels.CreateCategoryAsync(guild.Id, name, configure, reason, cancellationToken);

    public static Task<DiscordChannel> CreateAnnouncementChannelAsync(this DiscordGuild guild, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Channels.CreateAnnouncementAsync(guild.Id, name, configure, reason, cancellationToken);

    public static Task<DiscordChannel> CreateStageChannelAsync(this DiscordGuild guild, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Channels.CreateStageAsync(guild.Id, name, configure, reason, cancellationToken);

    public static Task<DiscordChannel> CreateForumChannelAsync(this DiscordGuild guild, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Channels.CreateForumAsync(guild.Id, name, configure, reason, cancellationToken);

    public static Task<DiscordChannel> CreateMediaChannelAsync(this DiscordGuild guild, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Channels.CreateMediaAsync(guild.Id, name, configure, reason, cancellationToken);

    public static Task<DiscordMember> GetMemberAsync(this DiscordGuild guild, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        guild.Services().Members.GetAsync(guild.Id, userId, cancellationToken);

    public static Task<IReadOnlyList<DiscordMember>> GetMembersAsync(this DiscordGuild guild, int limit = 100,
        Snowflake? after = null, CancellationToken cancellationToken = default) =>
        guild.Services().Members.GetPageAsync(guild.Id, limit, after, cancellationToken);

    public static IAsyncEnumerable<DiscordMember> GetAllMembersAsync(this DiscordGuild guild, int pageSize = 1000,
        CancellationToken cancellationToken = default) =>
        guild.Services().Members.GetAllAsync(guild.Id, pageSize, cancellationToken);

    public static Task<IReadOnlyList<DiscordMember>> SearchMembersAsync(this DiscordGuild guild, string query,
        int limit = 10, CancellationToken cancellationToken = default) =>
        guild.Services().Members.SearchAsync(guild.Id, query, limit, cancellationToken);

    public static Task<DiscordMember?> FindMemberAsync(this DiscordGuild guild, string name,
        CancellationToken cancellationToken = default) =>
        guild.Services().Members.FindAsync(guild.Id, name, cancellationToken);

    public static Task<IReadOnlyList<DiscordRole>> GetRolesAsync(this DiscordGuild guild,
        CancellationToken cancellationToken = default) =>
        guild.Services().Roles.GetAllAsync(guild.Id, cancellationToken);

    public static Task<DiscordRole?> GetRoleAsync(this DiscordGuild guild, Snowflake roleId,
        CancellationToken cancellationToken = default) =>
        guild.Services().Roles.GetAsync(guild.Id, roleId, cancellationToken);

    public static Task<DiscordRole?> FindRoleAsync(this DiscordGuild guild, string name,
        CancellationToken cancellationToken = default) =>
        guild.Services().Roles.FindAsync(guild.Id, name, cancellationToken);

    public static Task<DiscordRole> CreateRoleAsync(this DiscordGuild guild, string name,
        DiscordPermissions permissions = DiscordPermissions.None, int color = 0, bool hoist = false,
        bool mentionable = false, string? reason = null, CancellationToken cancellationToken = default) =>
        guild.Services().Roles.CreateAsync(guild.Id, name, permissions, color, hoist, mentionable, reason,
            cancellationToken);

    public static Task<DiscordRole> CreateRoleAsync(this DiscordGuild guild, RoleCreateRequest request,
        string? reason = null, CancellationToken cancellationToken = default) =>
        guild.Services().Roles.CreateAsync(guild.Id, request, reason, cancellationToken);

    public static Task<DiscordRole> EnsureRoleAsync(this DiscordGuild guild, string name,
        DiscordPermissions permissions = DiscordPermissions.None, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Roles.EnsureAsync(guild.Id, name, permissions, reason, cancellationToken);

    public static Task<IReadOnlyList<DiscordGuildEmoji>> GetEmojisAsync(this DiscordGuild guild,
        CancellationToken cancellationToken = default) =>
        guild.Services().Emojis.GetAllAsync(guild.Id, cancellationToken);

    public static Task<DiscordGuildEmoji> GetEmojiAsync(this DiscordGuild guild, Snowflake emojiId,
        CancellationToken cancellationToken = default) =>
        guild.Services().Emojis.GetAsync(guild.Id, emojiId, cancellationToken);

    public static Task<DiscordGuildEmoji?> FindEmojiAsync(this DiscordGuild guild, string name,
        CancellationToken cancellationToken = default) =>
        guild.Services().Emojis.FindAsync(guild.Id, name, cancellationToken);

    public static Task<DiscordGuildEmoji> CreateEmojiAsync(this DiscordGuild guild, string name,
        ReadOnlySpan<byte> image, string mediaType, IEnumerable<Snowflake>? roleIds = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Emojis.CreateAsync(guild.Id, name, image, mediaType, roleIds, reason, cancellationToken);

    public static Task<DiscordGuildEmoji> GetOrCreateEmojiAsync(this DiscordGuild guild, string name,
        byte[] image, string mediaType, IEnumerable<Snowflake>? roleIds = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Emojis.GetOrCreateAsync(guild.Id, name, image, mediaType, roleIds, reason,
            cancellationToken);

    public static Task BanAsync(this DiscordGuild guild, Snowflake userId,
        TimeSpan deleteMessageHistory = default, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Members.BanAsync(guild.Id, userId, deleteMessageHistory, reason, cancellationToken);

    public static Task UnbanAsync(this DiscordGuild guild, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Members.UnbanAsync(guild.Id, userId, reason, cancellationToken);

    public static Task KickAsync(this DiscordGuild guild, Snowflake userId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Members.KickAsync(guild.Id, userId, reason, cancellationToken);

    public static Task<IReadOnlyList<DiscordBan>> GetBansAsync(this DiscordGuild guild, int limit = 1000,
        Snowflake? after = null, CancellationToken cancellationToken = default) =>
        guild.Services().Members.GetBansAsync(guild.Id, limit, after, cancellationToken);

    public static Task<DiscordBan?> GetBanAsync(this DiscordGuild guild, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        guild.Services().Members.GetBanAsync(guild.Id, userId, cancellationToken);

    public static Task<bool> IsBannedAsync(this DiscordGuild guild, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        guild.Services().Members.IsBannedAsync(guild.Id, userId, cancellationToken);

    public static Task<int> PruneCountAsync(this DiscordGuild guild, PruneRequest? request = null,
        CancellationToken cancellationToken = default) =>
        guild.Rest().GetGuildPruneCountAsync(guild.Id, request, cancellationToken);

    public static Task<int?> PruneAsync(this DiscordGuild guild, PruneRequest? request = null,
        string? reason = null, CancellationToken cancellationToken = default) =>
        guild.Rest().BeginGuildPruneAsync(guild.Id, request, reason, cancellationToken);

    public static Task<DiscordAuditLog> GetAuditLogAsync(this DiscordGuild guild, AuditLogQuery? query = null,
        CancellationToken cancellationToken = default) =>
        guild.Rest().GetGuildAuditLogAsync(guild.Id, query, cancellationToken);

    public static Task<DiscordUser> GetOwnerAsync(this DiscordGuild guild,
        CancellationToken cancellationToken = default) =>
        guild.Services().Guilds.GetOwnerAsync(guild.Id, cancellationToken);

    public static Task<DiscordPermissions> PermissionsOfAsync(this DiscordGuild guild, Snowflake userId,
        CancellationToken cancellationToken = default) =>
        guild.Services().Guilds.PermissionsOfAsync(guild.Id, userId, cancellationToken);

    public static Task<IReadOnlyList<DiscordInvite>> GetInvitesAsync(this DiscordGuild guild,
        CancellationToken cancellationToken = default) =>
        guild.Rest().GetGuildInvitesAsync(guild.Id, cancellationToken);

    public static Task<ThreadListing> GetActiveThreadsAsync(this DiscordGuild guild,
        CancellationToken cancellationToken = default) =>
        guild.Services().Threads.GetActiveAsync(guild.Id, cancellationToken);

    public static Task<IReadOnlyList<DiscordApplicationCommand>> GetCommandsAsync(this DiscordGuild guild,
        CancellationToken cancellationToken = default) =>
        guild.Services().Commands.GetAllAsync(guild.RequireApplicationId(), guild.Id, cancellationToken);

    public static Task<DiscordApplicationCommand> RegisterCommandAsync(this DiscordGuild guild,
        SlashCommandFactory command, CancellationToken cancellationToken = default) =>
        guild.Services().Commands.RegisterAsync(guild.RequireApplicationId(), command, guild.Id,
            cancellationToken);

    public static Task<DiscordApplicationCommand> RegisterCommandAsync(this DiscordGuild guild, string name,
        string description, Action<SlashCommandFactory>? configure = null,
        CancellationToken cancellationToken = default) =>
        guild.Services().Commands.RegisterAsync(guild.RequireApplicationId(), name, description, configure,
            guild.Id, cancellationToken);

    public static Task<CommandSyncResult> SyncCommandsAsync(this DiscordGuild guild,
        IEnumerable<ApplicationCommandRequest> desired, CancellationToken cancellationToken = default) =>
        guild.Services().Commands.SynchronizeAsync(guild.RequireApplicationId(), desired, guild.Id,
            cancellationToken);

    public static Task RequestMembersAsync(this DiscordGuild guild, string? query = null, int limit = 0,
        CancellationToken cancellationToken = default)
    {
        var gateway = guild.Context().Gateway ?? throw new InvalidOperationException(
            "Requesting guild members needs a gateway connection; this entity came from a REST-only client.");

        return gateway.RequestGuildMembersAsync(new GuildMembersRequest
        {
            GuildId = guild.Id,
            Query = query,
            Limit = limit
        }, cancellationToken).AsTask();
    }
}

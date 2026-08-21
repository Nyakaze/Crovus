using System.Globalization;
using Crovus.Models;

namespace Crovus.Rest;

internal sealed record AttachmentPayload(string Id, string? Filename, string? Description)
{
    public static IReadOnlyList<AttachmentPayload>? Build(IReadOnlyList<DiscordFile>? files,
        IReadOnlyList<Snowflake>? kept = null)
    {
        if (kept is null && files is not { Count: > 0 })
            return null;

        var payloads = new List<AttachmentPayload>((kept?.Count ?? 0) + (files?.Count ?? 0));

        if (kept is not null)
            payloads.AddRange(kept.Select(id => new AttachmentPayload(id.ToString(), null, null)));

        for (var index = 0; index < (files?.Count ?? 0); index++)
        {
            var file = files![index];

            payloads.Add(new AttachmentPayload(index.ToString(CultureInfo.InvariantCulture), file.UploadName,
                file.Description));
        }

        return payloads;
    }
}

internal sealed record MessageCreatePayload(
    string? Content,
    IReadOnlyList<DiscordEmbed>? Embeds,
    DiscordMessageReference? MessageReference,
    bool Tts,
    IReadOnlyList<AttachmentPayload>? Attachments = null,
    IReadOnlyList<DiscordComponent>? Components = null)
{
    public static MessageCreatePayload From(MessageCreateRequest request) =>
        new(request.Content, request.Embeds, request.Reply, request.Tts, AttachmentPayload.Build(request.Files),
            request.Components);
}

internal sealed record MessageEditPayload(
    string? Content,
    IReadOnlyList<DiscordEmbed>? Embeds,
    IReadOnlyList<AttachmentPayload>? Attachments = null,
    IReadOnlyList<DiscordComponent>? Components = null)
{
    public static MessageEditPayload From(MessageEditRequest request) =>
        new(request.Content, request.Embeds,
            AttachmentPayload.Build(request.Files, request.KeptAttachments), request.Components);
}

internal sealed record WebhookCreatePayload(
    string Name,
    string? Avatar);

internal sealed record WebhookModifyPayload(
    string? Name,
    string? Avatar,
    Snowflake? ChannelId);

internal sealed record WebhookExecutePayload(
    string? Content,
    IReadOnlyList<DiscordEmbed>? Embeds,
    string? Username,
    string? AvatarUrl,
    string? ThreadName,
    bool Tts,
    IReadOnlyList<AttachmentPayload>? Attachments = null,
    IReadOnlyList<DiscordComponent>? Components = null)
{
    public static WebhookExecutePayload From(WebhookExecuteRequest request) =>
        new(request.Content, request.Embeds, request.Username, request.AvatarUrl, request.ThreadName, request.Tts,
            AttachmentPayload.Build(request.Files), request.Components);
}

internal sealed record ChannelCreatePayload(
    string Name,
    ChannelType Type,
    string? Topic,
    int? Position,
    bool? Nsfw,
    int? RateLimitPerUser,
    int? Bitrate,
    int? UserLimit,
    Snowflake? ParentId,
    ThreadArchiveDuration? DefaultAutoArchiveDuration,
    IReadOnlyList<DiscordPermissionOverwrite>? PermissionOverwrites);

internal sealed record ChannelModifyPayload(
    string? Name,
    ChannelType? Type,
    string? Topic,
    int? Position,
    bool? Nsfw,
    int? RateLimitPerUser,
    int? Bitrate,
    int? UserLimit,
    Snowflake? ParentId,
    ThreadArchiveDuration? DefaultAutoArchiveDuration,
    IReadOnlyList<DiscordPermissionOverwrite>? PermissionOverwrites,
    bool? Archived,
    bool? Locked,
    bool? Invitable,
    ThreadArchiveDuration? AutoArchiveDuration,
    IReadOnlyList<Snowflake>? AppliedTags);

internal sealed record ThreadCreatePayload(
    string Name,
    ChannelType Type,
    ThreadArchiveDuration? AutoArchiveDuration,
    bool? Invitable,
    int? RateLimitPerUser,
    MessageCreatePayload? Message,
    IReadOnlyList<Snowflake>? AppliedTags);

internal sealed record ThreadFromMessagePayload(
    string Name,
    ThreadArchiveDuration? AutoArchiveDuration,
    int? RateLimitPerUser);

internal sealed record EmojiCreatePayload(
    string Name,
    string Image,
    IReadOnlyList<Snowflake> Roles);

internal sealed record EmojiModifyPayload(
    string? Name,
    IReadOnlyList<Snowflake>? Roles);

internal sealed record ApplicationCommandPayload(
    string Name,
    ApplicationCommandType Type,
    string? Description,
    IReadOnlyList<DiscordApplicationCommandOption>? Options,
    DiscordPermissions? DefaultMemberPermissions,
    bool? Nsfw,
    IReadOnlyList<ApplicationIntegrationType>? IntegrationTypes,
    IReadOnlyList<InteractionContextType>? Contexts)
{
    public static ApplicationCommandPayload From(ApplicationCommandRequest request) =>
        new(request.Name, request.Type, request.Description, request.Options, request.DefaultMemberPermissions,
            request.Nsfw, request.IntegrationTypes, request.Contexts);
}

internal sealed record InteractionCallbackPayload(
    InteractionCallbackType Type,
    object? Data)
{
    public static InteractionCallbackPayload From(InteractionResponseRequest request) => new(request.Type,
        request.Type switch
        {
            InteractionCallbackType.ApplicationCommandAutocompleteResult =>
                new InteractionAutocompletePayload(request.Choices ?? []),
            InteractionCallbackType.Modal => request.Modal is { } modal
                ? InteractionModalPayload.From(modal)
                : throw new InvalidOperationException("A modal response requires a modal."),
            _ => request.Message is { } message ? InteractionMessagePayload.From(message) : null
        });
}

internal sealed record InteractionMessagePayload(
    string? Content,
    IReadOnlyList<DiscordEmbed>? Embeds,
    MessageFlags? Flags,
    bool Tts,
    IReadOnlyList<AttachmentPayload>? Attachments = null,
    IReadOnlyList<DiscordComponent>? Components = null)
{
    public static InteractionMessagePayload From(InteractionMessageRequest request) =>
        new(request.Content, request.Embeds,
            request.EffectiveFlags is MessageFlags.None ? null : request.EffectiveFlags, request.Tts,
            AttachmentPayload.Build(request.Files, request.KeptAttachments), request.Components);
}

internal sealed record InteractionModalPayload(
    string CustomId,
    string Title,
    IReadOnlyList<DiscordComponent> Components)
{
    public static InteractionModalPayload From(DiscordModal modal) =>
        new(modal.CustomId, modal.Title, modal.Components);
}

internal sealed record InteractionAutocompletePayload(
    IReadOnlyList<DiscordApplicationCommandChoice> Choices);

internal static class MemberModifyPayload
{
    public static Dictionary<string, object?> From(MemberModifyRequest request)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (request.Nickname is not null)
            payload["nick"] = request.Nickname;

        if (request.Roles is not null)
            payload["roles"] = request.Roles;

        if (request.Mute is { } mute)
            payload["mute"] = mute;

        if (request.Deaf is { } deaf)
            payload["deaf"] = deaf;

        if (request.VoiceChannelId is { } channelId)
            payload["channel_id"] = channelId;

        if (request.ClearTimeout)
            payload["communication_disabled_until"] = null;
        else if (request.CommunicationDisabledUntil is { } until)
            payload["communication_disabled_until"] = until;

        return payload;
    }
}

internal sealed record RoleCreatePayload(string Name, DiscordPermissions Permissions, int Color, bool Hoist,
    bool Mentionable, string? UnicodeEmoji, string? Icon)
{
    public static RoleCreatePayload From(RoleCreateRequest request) => new(request.Name, request.Permissions,
        request.Color, request.Hoist, request.Mentionable, request.UnicodeEmoji, request.IconData);
}

internal sealed record RoleModifyPayload(string? Name, DiscordPermissions? Permissions, int? Color, bool? Hoist,
    bool? Mentionable, string? UnicodeEmoji, string? Icon)
{
    public static RoleModifyPayload From(RoleModifyRequest request) => new(request.Name, request.Permissions,
        request.Color, request.Hoist, request.Mentionable, request.UnicodeEmoji, request.IconData);
}

internal sealed record BanCreatePayload(int DeleteMessageSeconds);

internal sealed record BulkDeletePayload(IReadOnlyList<string> Messages);

internal sealed record CreateDmPayload(string RecipientId);

internal sealed record InviteCreatePayload(int MaxAge, int MaxUses, bool Temporary, bool Unique,
    string? TargetUserId, int? TargetType)
{
    public static InviteCreatePayload From(InviteCreateRequest request) => new(
        (int)(request.MaxAge?.TotalSeconds ?? 86400),
        request.MaxUses ?? 0,
        request.Temporary,
        request.Unique,
        request.TargetUserId?.ToString(),
        request.TargetUserId is null ? null : 2);
}

internal sealed record PrunePayload(int Days, bool ComputePruneCount, IReadOnlyList<string> IncludeRoles)
{
    public static PrunePayload From(PruneRequest request) => new(
        request.Days,
        request.ReturnCount,
        request.IncludeRoles.Select(role => role.ToString()).ToArray());
}

internal sealed record GuildModifyPayload(string? Name, string? Description, string? OwnerId, string? AfkChannelId,
    int? AfkTimeout, string? SystemChannelId, string? RulesChannelId, string? PublicUpdatesChannelId,
    int? VerificationLevel, int? DefaultMessageNotifications, int? ExplicitContentFilter, string? PreferredLocale,
    string? Icon, string? Banner, string? Splash)
{
    public static GuildModifyPayload From(GuildModifyRequest request) => new(
        request.Name,
        request.Description,
        request.OwnerId?.ToString(),
        request.AfkChannelId?.ToString(),
        request.AfkTimeout,
        request.SystemChannelId?.ToString(),
        request.RulesChannelId?.ToString(),
        request.PublicUpdatesChannelId?.ToString(),
        (int?)request.VerificationLevel,
        (int?)request.DefaultMessageNotifications,
        (int?)request.ExplicitContentFilter,
        request.PreferredLocale,
        request.IconData,
        request.BannerData,
        request.SplashData);
}

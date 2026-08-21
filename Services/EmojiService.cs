using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class EmojiService : DiscordService
{
    public EmojiService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Emoji", logger, telemetry)
    {
    }

    public EmojiService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task<IReadOnlyList<DiscordGuildEmoji>> GetAllAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetAllAsync), $"guild {guildId}",
            () => Rest.GetGuildEmojisAsync(guildId, cancellationToken),
            emojis => $"Loaded {emojis.Count} emojis of guild {guildId}", LogLevel.Debug);

    public Task<DiscordGuildEmoji> GetAsync(Snowflake guildId, Snowflake emojiId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetAsync), $"emoji {emojiId} in guild {guildId}",
            () => Rest.GetGuildEmojiAsync(guildId, emojiId, cancellationToken),
            emoji => $"Loaded emoji {emoji.Name} ({emoji.Id}) of guild {guildId}", LogLevel.Debug);

    public async Task<DiscordGuildEmoji?> FindAsync(Snowflake guildId, string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await TrackAsync(nameof(FindAsync), $"emoji {name} in guild {guildId}",
            async () =>
            {
                var emojis = await Rest.GetGuildEmojisAsync(guildId, cancellationToken);

                foreach (var emoji in emojis)
                {
                    if (string.Equals(emoji.Name, name, StringComparison.Ordinal))
                        return emoji;
                }

                return null;
            },
            found => found is null
                ? $"Guild {guildId} has no emoji named {name}"
                : $"Found emoji {name} ({found.Id}) in guild {guildId}", LogLevel.Debug);
    }

    public async Task<string?> GetUrlAsync(Snowflake guildId, string name, int? size = null,
        EmojiFormat format = EmojiFormat.Auto, CancellationToken cancellationToken = default)
    {
        var emoji = await FindAsync(guildId, name, cancellationToken);

        return emoji?.UrlFor(size, format);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetUrlMapAsync(Snowflake guildId, int? size = null,
        EmojiFormat format = EmojiFormat.Auto, CancellationToken cancellationToken = default)
    {
        var emojis = await GetAllAsync(guildId, cancellationToken);
        var map = new Dictionary<string, string>(emojis.Count, StringComparer.Ordinal);

        foreach (var emoji in emojis)
            map[emoji.Name] = emoji.UrlFor(size, format);

        return map;
    }

    public Task<DiscordGuildEmoji> CreateAsync(Snowflake guildId, EmojiCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(CreateAsync), $"guild {guildId}",
            () => Rest.CreateGuildEmojiAsync(guildId, request, reason, cancellationToken),
            emoji => $"Created emoji {emoji.Name} ({emoji.Id}) in guild {guildId}{Because(reason)}");
    }

    public Task<DiscordGuildEmoji> CreateAsync(Snowflake guildId, EmojiFactory emoji, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        return CreateAsync(guildId, emoji.Build(), reason, cancellationToken);
    }

    public Task<DiscordGuildEmoji> CreateAsync(Snowflake guildId, string name, ReadOnlySpan<byte> image,
        string mediaType, IEnumerable<Snowflake>? roleIds = null, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var factory = EmojiFactory.Create(name).WithImage(image, mediaType);

        if (roleIds is not null)
            factory.RestrictTo(roleIds);

        return CreateAsync(guildId, factory.Build(), reason, cancellationToken);
    }

    public async Task<DiscordGuildEmoji> GetOrCreateAsync(Snowflake guildId, string name, byte[] image,
        string mediaType, IEnumerable<Snowflake>? roleIds = null, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        EmojiFactory.ValidateName(name);

        var request = BuildCreate(name, image, mediaType, roleIds);
        var created = false;

        var emoji = await TrackAsync(nameof(GetOrCreateAsync), $"emoji {name} in guild {guildId}",
            async () =>
            {
                var existing = await Rest.GetGuildEmojisAsync(guildId, cancellationToken);

                foreach (var candidate in existing)
                {
                    if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
                        return candidate;
                }

                created = true;

                return await Rest.CreateGuildEmojiAsync(guildId, request, reason, cancellationToken);
            },
            resolved => $"{(created ? "Created" : "Reused")} emoji {name} ({resolved.Id}) in guild {guildId}");

        Emit(new EmojiResolved(guildId.Value, emoji.Id.Value, created));

        return emoji;
    }

    public Task<DiscordGuildEmoji> ModifyAsync(Snowflake guildId, Snowflake emojiId, EmojiModifyRequest request,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(ModifyAsync), $"emoji {emojiId} in guild {guildId}",
            () => Rest.ModifyGuildEmojiAsync(guildId, emojiId, request, reason, cancellationToken),
            emoji => $"Modified emoji {emoji.Name} ({emoji.Id}) in guild {guildId}{Because(reason)}");
    }

    public Task<DiscordGuildEmoji> ModifyAsync(Snowflake guildId, Snowflake emojiId, Action<EmojiFactory> configure,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = EmojiFactory.Modify();
        configure(factory);

        return ModifyAsync(guildId, emojiId, factory.BuildModify(), reason, cancellationToken);
    }

    public Task<DiscordGuildEmoji> RenameAsync(Snowflake guildId, Snowflake emojiId, string name,
        string? reason = null, CancellationToken cancellationToken = default) =>
        ModifyAsync(guildId, emojiId, emoji => emoji.WithName(name), reason, cancellationToken);

    public Task<DiscordGuildEmoji> RestrictAsync(Snowflake guildId, Snowflake emojiId, IEnumerable<Snowflake> roleIds,
        string? reason = null, CancellationToken cancellationToken = default) =>
        ModifyAsync(guildId, emojiId, emoji => emoji.RestrictTo(roleIds), reason, cancellationToken);

    public Task<DiscordGuildEmoji> UnrestrictAsync(Snowflake guildId, Snowflake emojiId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(guildId, emojiId, emoji => emoji.Unrestricted(), reason, cancellationToken);

    public Task DeleteAsync(Snowflake guildId, Snowflake emojiId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteAsync), $"emoji {emojiId} in guild {guildId}",
            () => Rest.DeleteGuildEmojiAsync(guildId, emojiId, reason, cancellationToken),
            $"Deleted emoji {emojiId} from guild {guildId}{Because(reason)}");

    public async Task<bool> DeleteByNameAsync(Snowflake guildId, string name, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (await FindAsync(guildId, name, cancellationToken) is not { } emoji)
            return false;

        await DeleteAsync(guildId, emoji.Id, reason, cancellationToken);

        return true;
    }

    private static EmojiCreateRequest BuildCreate(string name, ReadOnlySpan<byte> image, string mediaType,
        IEnumerable<Snowflake>? roleIds)
    {
        var factory = EmojiFactory.Create(name).WithImage(image, mediaType);

        if (roleIds is not null)
            factory.RestrictTo(roleIds);

        return factory.Build();
    }
}

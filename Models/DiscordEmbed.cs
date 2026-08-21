namespace Crovus.Models;

public sealed record DiscordEmbed(string? Title, string? Description, string? Url, EmbedType Type,
    DateTimeOffset? Timestamp, int? Color,
    DiscordEmbedAuthor? Author, DiscordEmbedFooter? Footer,
    DiscordEmbedMedia? Image, DiscordEmbedMedia? Thumbnail, DiscordEmbedMedia? Video,
    DiscordEmbedProvider? Provider,
    IReadOnlyList<DiscordEmbedField> Fields);

public sealed record DiscordEmbedAuthor(string Name, string? Url, string? IconUrl, string? ProxyIconUrl);

public sealed record DiscordEmbedFooter(string Text, string? IconUrl, string? ProxyIconUrl);

public sealed record DiscordEmbedMedia(string? Url, string? ProxyUrl, int? Width, int? Height);

public sealed record DiscordEmbedProvider(string? Name, string? Url);

public sealed record DiscordEmbedField(string Name, string Value, bool Inline);

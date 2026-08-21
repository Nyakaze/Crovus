using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Crovus.Models;

public enum EmojiFormat
{
    Auto = 0,
    Png = 1,
    Gif = 2,
    WebP = 3,
    Jpeg = 4,
    Avif = 5
}

public static class EmojiParser
{
    public const string CdnBase = "https://cdn.discordapp.com/emojis";

    public const string TwemojiBase = "https://cdn.jsdelivr.net/gh/jdecked/twemoji@latest/assets";

    private const int MinimumSize = 16;
    private const int MaximumSize = 4096;

    public static DiscordEmoji Parse(string emoji) =>
        TryParse(emoji, out var parsed)
            ? parsed
            : throw new FormatException($"\"{emoji}\" is not a usable emoji.");

    public static bool TryParse([NotNullWhen(true)] string? emoji, [NotNullWhen(true)] out DiscordEmoji? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(emoji))
            return false;

        var value = emoji.Trim();

        if (TryParseMention(value, out result))
            return true;

        if (value.StartsWith('<'))
            return false;

        if (TryParseUrl(value, out var id, out var animated))
        {
            result = new DiscordEmoji(string.Empty, id, animated);
            return true;
        }

        if (TryParseRaw(value, out result))
            return true;

        result = new DiscordEmoji(value, null, false);

        return true;
    }

    public static bool TryParseMention([NotNullWhen(true)] string? emoji,
        [NotNullWhen(true)] out DiscordEmoji? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(emoji))
            return false;

        var value = emoji.Trim();

        if (!value.StartsWith('<') || !value.EndsWith('>'))
            return false;

        var body = value[1..^1];
        var animated = body.StartsWith('a');

        if (animated)
            body = body[1..];

        if (!body.StartsWith(':'))
            return false;

        body = body[1..];

        var separator = body.LastIndexOf(':');

        if (separator <= 0 || separator == body.Length - 1)
            return false;

        if (!ulong.TryParse(body[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            return false;

        result = new DiscordEmoji(body[..separator], new Snowflake(id), animated);

        return true;
    }

    public static IReadOnlyList<DiscordEmoji> ParseAll(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var found = new List<DiscordEmoji>();
        var index = 0;

        while (index < text.Length)
        {
            var start = text.IndexOf('<', index);

            if (start < 0)
                break;

            var end = text.IndexOf('>', start + 1);

            if (end < 0)
                break;

            if (TryParseMention(text[start..(end + 1)], out var emoji))
            {
                found.Add(emoji);
                index = end + 1;
            }
            else
            {
                index = start + 1;
            }
        }

        return found;
    }

    public static string Format(DiscordEmoji emoji)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        if (emoji.Id is not { } id)
            return emoji.Name;

        return emoji.Animated ? $"<a:{emoji.Name}:{id.Value}>" : $"<:{emoji.Name}:{id.Value}>";
    }

    public static string ToUrl(DiscordEmoji emoji, int? size = null, EmojiFormat format = EmojiFormat.Auto)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        if (emoji.Id is not { } id)
            throw new InvalidOperationException(
                $"\"{emoji.Name}\" is a unicode emoji and is not hosted on the Discord CDN.");

        return ToUrl(id, emoji.Animated, size, format);
    }

    public static string ToUrl(Snowflake emojiId, bool animated, int? size = null,
        EmojiFormat format = EmojiFormat.Auto)
    {
        var url = $"{CdnBase}/{emojiId.Value}.{Extension(format, animated)}";

        return size is { } requested ? $"{url}?size={ValidateSize(requested)}" : url;
    }

    public static bool TryGetUrl(DiscordEmoji? emoji, [NotNullWhen(true)] out string? url, int? size = null,
        EmojiFormat format = EmojiFormat.Auto)
    {
        url = null;

        if (emoji?.Id is not { } id)
            return false;

        url = ToUrl(id, emoji.Animated, size, format);

        return true;
    }

    public static bool TryParseUrl([NotNullWhen(true)] string? url, out Snowflake emojiId, out bool animated)
    {
        emojiId = default;
        animated = false;

        if (string.IsNullOrWhiteSpace(url))
            return false;

        var value = url.Trim();

        if (!value.StartsWith(CdnBase, StringComparison.OrdinalIgnoreCase))
            return false;

        var path = value[(CdnBase.Length + 1)..];

        if (path.IndexOf('?') is var query and >= 0)
            path = path[..query];

        var dot = path.LastIndexOf('.');

        if (dot <= 0)
            return false;

        if (!ulong.TryParse(path[..dot], NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            return false;

        emojiId = new Snowflake(id);
        animated = path[(dot + 1)..].Equals("gif", StringComparison.OrdinalIgnoreCase);

        return true;
    }

    public static string ToTwemojiUrl(DiscordEmoji emoji, bool svg = true)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        if (emoji.Id is not null)
            throw new InvalidOperationException(
                $"\"{emoji.Name}\" is a custom emoji and is served by the Discord CDN.");

        var codePoints = ToCodePoints(emoji.Name);

        if (codePoints.Length == 0)
            throw new InvalidOperationException("The emoji carries no code points.");

        return svg ? $"{TwemojiBase}/svg/{codePoints}.svg" : $"{TwemojiBase}/72x72/{codePoints}.png";
    }

    public static string ToCodePoints(string emoji)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var joined = emoji.Contains('\u200D');
        var points = new List<string>();

        foreach (var rune in emoji.EnumerateRunes())
        {
            if (!joined && rune.Value == 0xFE0F)
                continue;

            points.Add(rune.Value.ToString("x", CultureInfo.InvariantCulture));
        }

        return string.Join('-', points);
    }

    private static bool TryParseRaw(string value, [NotNullWhen(true)] out DiscordEmoji? result)
    {
        result = null;

        var separator = value.LastIndexOf(':');

        if (separator <= 0 || separator == value.Length - 1)
            return false;

        if (!ulong.TryParse(value[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            return false;

        var name = value[..separator];
        var animated = false;

        if (name.StartsWith("a:", StringComparison.Ordinal))
        {
            animated = true;
            name = name[2..];
        }

        if (name.Length == 0)
            return false;

        result = new DiscordEmoji(name, new Snowflake(id), animated);

        return true;
    }

    private static string Extension(EmojiFormat format, bool animated) => format switch
    {
        EmojiFormat.Auto => animated ? "gif" : "png",
        EmojiFormat.Png => "png",
        EmojiFormat.Gif => "gif",
        EmojiFormat.WebP => "webp",
        EmojiFormat.Jpeg => "jpg",
        EmojiFormat.Avif => "avif",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown emoji format.")
    };

    private static int ValidateSize(int size)
    {
        if (size is < MinimumSize or > MaximumSize)
            throw new ArgumentOutOfRangeException(nameof(size),
                $"An emoji size must be between {MinimumSize} and {MaximumSize} but was {size}.");

        if ((size & (size - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(size),
                $"An emoji size must be a power of two but was {size}.");

        return size;
    }
}

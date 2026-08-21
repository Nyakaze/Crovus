using System.Text.Json.Serialization;

namespace Crovus.Models;

public sealed record DiscordEmoji(string Name, Snowflake? Id, bool Animated)
{
    public static DiscordEmoji Unicode(string s) => new(s, null, false);

    public static DiscordEmoji Custom(string name, Snowflake id, bool animated = false) => new(name, id, animated);

    public static DiscordEmoji Parse(string emoji) => EmojiParser.Parse(emoji);

    public string ToReactionPath() => Id is null
        ? Uri.EscapeDataString(Name)
        : Uri.EscapeDataString($"{Name}:{Id.Value.Value}");

    [JsonIgnore]
    public bool IsCustom => Id is not null;

    [JsonIgnore]
    public bool IsUnicode => Id is null;

    [JsonIgnore]
    public string Mention => EmojiParser.Format(this);

    [JsonIgnore]
    public string? Url => Id is { } id ? EmojiParser.ToUrl(id, Animated) : null;

    public string? UrlFor(int? size = null, EmojiFormat format = EmojiFormat.Auto) =>
        Id is { } id ? EmojiParser.ToUrl(id, Animated, size, format) : null;

    public override string ToString() => EmojiParser.Format(this);
}

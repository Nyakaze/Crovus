using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public enum StickerType
{
    Standard = 1,
    Guild = 2
}

public enum StickerFormatType
{
    Png = 1,
    Apng = 2,
    Lottie = 3,
    Gif = 4
}

public sealed record DiscordSticker : IBoundEntity
{
    public required Snowflake Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string Tags { get; init; } = string.Empty;

    public Snowflake? PackId { get; init; }

    public Snowflake? GuildId { get; init; }

    public StickerType Type { get; init; } = StickerType.Guild;

    public StickerFormatType FormatType { get; init; } = StickerFormatType.Png;

    public bool Available { get; init; } = true;

    public int? SortValue { get; init; }

    public DiscordUser? Author { get; init; }

    [JsonIgnore]
    public bool IsAnimated => FormatType is StickerFormatType.Apng or StickerFormatType.Gif or StickerFormatType.Lottie;

    [JsonIgnore]
    public string Url => FormatType is StickerFormatType.Gif
        ? $"https://media.discordapp.net/stickers/{Id}.gif"
        : $"https://cdn.discordapp.com/stickers/{Id}.{(FormatType is StickerFormatType.Lottie ? "json" : "png")}";

    public DiscordSticker In(Snowflake guildId) => GuildId is null ? this with { GuildId = guildId } : this;

    public override string ToString() => Name;

    private EntityBinding _binding;

    public DiscordSticker Bind(ICrovusContext context)
    {
        var bound = this with { Author = Author?.Bind(context) };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}

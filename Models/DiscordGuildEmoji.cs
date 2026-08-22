using Crovus.Client;

namespace Crovus.Models;

public sealed record DiscordGuildEmoji(Snowflake Id, string Name, IReadOnlyList<Snowflake> Roles,
    DiscordUser? Creator, bool Animated, bool Managed, bool Available, bool RequireColons) : IBoundEntity
{
    public DiscordEmoji AsReaction() => new(Name, Id, Animated);

    public string Mention => EmojiParser.Format(AsReaction());

    public string Url => EmojiParser.ToUrl(Id, Animated);

    public string UrlFor(int? size = null, EmojiFormat format = EmojiFormat.Auto) =>
        EmojiParser.ToUrl(Id, Animated, size, format);

    public bool IsRestricted => Roles.Count > 0;

    private EntityBinding _binding;

    public DiscordGuildEmoji Bind(ICrovusContext context)
    {
        var bound = this with { Creator = Creator?.Bind(context) };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}

public sealed record EmojiCreateRequest(string Name, string ImageData, IReadOnlyList<Snowflake>? Roles = null);

public sealed record EmojiModifyRequest(string? Name = null, IReadOnlyList<Snowflake>? Roles = null);

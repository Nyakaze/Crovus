using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Factory;

public sealed class EmojiFactory
{
    private readonly List<Snowflake> _roles = [];

    private string? _name;
    private string? _imageData;
    private bool _rolesTouched;

    public static EmojiFactory Create(string name) => new EmojiFactory().WithName(name);

    public static EmojiFactory Modify() => new();

    public static EmojiFactory From(DiscordGuildEmoji emoji)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        var factory = new EmojiFactory { _name = emoji.Name };
        factory._roles.AddRange(emoji.Roles);

        return factory;
    }

    public EmojiFactory WithName(string name)
    {
        _name = ValidateName(name);
        return this;
    }

    public EmojiFactory WithImage(string dataUri)
    {
        if (string.IsNullOrWhiteSpace(dataUri))
            throw new ArgumentException("The emoji image is empty.", nameof(dataUri));

        _imageData = dataUri;

        return this;
    }

    public EmojiFactory WithImage(ReadOnlySpan<byte> image, string mediaType)
    {
        _imageData = ImageData(image, mediaType);
        return this;
    }

    public EmojiFactory RestrictTo(params Snowflake[] roleIds) => RestrictTo((IEnumerable<Snowflake>)roleIds);

    public EmojiFactory RestrictTo(IEnumerable<Snowflake> roleIds)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        _roles.Clear();

        foreach (var roleId in roleIds)
        {
            if (!_roles.Contains(roleId))
                _roles.Add(roleId);
        }

        _rolesTouched = true;

        return this;
    }

    public EmojiFactory Unrestricted()
    {
        _roles.Clear();
        _rolesTouched = true;

        return this;
    }

    public EmojiCreateRequest Build()
    {
        if (_name is not { } name)
            throw new InvalidOperationException("An emoji cannot be created without a name.");

        if (_imageData is not { } image)
            throw new InvalidOperationException("An emoji cannot be created without an image.");

        return new EmojiCreateRequest(name, image, _rolesTouched ? _roles.ToArray() : null);
    }

    public EmojiModifyRequest BuildModify()
    {
        if (_name is null && !_rolesTouched)
            throw new InvalidOperationException("The modification would not change anything.");

        return new EmojiModifyRequest(_name, _rolesTouched ? _roles.ToArray() : null);
    }

    public Task<DiscordGuildEmoji> CreateAsync(IDiscordRest rest, Snowflake guildId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.CreateGuildEmojiAsync(guildId, Build(), reason, cancellationToken);
    }

    public Task<DiscordGuildEmoji> ApplyAsync(IDiscordRest rest, Snowflake guildId, Snowflake emojiId,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.ModifyGuildEmojiAsync(guildId, emojiId, BuildModify(), reason, cancellationToken);
    }

    public static string ImageData(ReadOnlySpan<byte> image, string mediaType)
    {
        if (image.IsEmpty)
            throw new ArgumentException("The emoji image is empty.", nameof(image));

        if (image.Length > DiscordLimits.EmojiImageBytes)
            throw new ArgumentException(
                $"An emoji image must be at most {DiscordLimits.EmojiImageBytes} bytes but was {image.Length}.",
                nameof(image));

        if (string.IsNullOrWhiteSpace(mediaType))
            throw new ArgumentException("The emoji media type is empty.", nameof(mediaType));

        return $"data:{mediaType};base64,{Convert.ToBase64String(image)}";
    }

    internal static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("An emoji name must not be empty.", nameof(name));

        if (name.Length is < DiscordLimits.EmojiNameMin or > DiscordLimits.EmojiName)
            throw new ArgumentException(
                $"An emoji name must be between {DiscordLimits.EmojiNameMin} and {DiscordLimits.EmojiName} " +
                $"characters but was {name.Length}.", nameof(name));

        foreach (var character in name)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
                throw new ArgumentException(
                    $"An emoji name accepts letters, digits and underscores only but contained '{character}'.",
                    nameof(name));
        }

        return name;
    }
}

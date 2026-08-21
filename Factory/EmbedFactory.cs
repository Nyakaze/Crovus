using Crovus.Models;

namespace Crovus.Factory;

public sealed class EmbedFactory
{
    private readonly List<DiscordEmbedField> _fields = [];

    private string? _title;
    private string? _description;
    private string? _url;
    private EmbedType _type = EmbedType.Rich;
    private DateTimeOffset? _timestamp;
    private int? _color;
    private DiscordEmbedAuthor? _author;
    private DiscordEmbedFooter? _footer;
    private DiscordEmbedMedia? _image;
    private DiscordEmbedMedia? _thumbnail;

    public static EmbedFactory Create() => new();

    public static EmbedFactory From(DiscordEmbed embed)
    {
        ArgumentNullException.ThrowIfNull(embed);

        var factory = new EmbedFactory
        {
            _title = embed.Title,
            _description = embed.Description,
            _url = embed.Url,
            _type = embed.Type,
            _timestamp = embed.Timestamp,
            _color = embed.Color,
            _author = embed.Author,
            _footer = embed.Footer,
            _image = embed.Image,
            _thumbnail = embed.Thumbnail
        };

        factory._fields.AddRange(embed.Fields);

        return factory;
    }

    public int FieldCount => _fields.Count;

    public int Length =>
        (_title?.Length ?? 0) +
        (_description?.Length ?? 0) +
        (_footer?.Text.Length ?? 0) +
        (_author?.Name.Length ?? 0) +
        _fields.Sum(entry => entry.Name.Length + entry.Value.Length);

    public EmbedFactory WithTitle(string? title)
    {
        _title = Limit.Text(title, DiscordLimits.EmbedTitle, nameof(title));
        return this;
    }

    public EmbedFactory WithDescription(string? description)
    {
        _description = Limit.Text(description, DiscordLimits.EmbedDescription, nameof(description));
        return this;
    }

    public EmbedFactory WithUrl(string? url)
    {
        _url = url;
        return this;
    }

    public EmbedFactory WithType(EmbedType type)
    {
        _type = type;
        return this;
    }

    public EmbedFactory WithTimestamp(DateTimeOffset? timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public EmbedFactory WithCurrentTimestamp(TimeProvider? timeProvider = null)
    {
        _timestamp = (timeProvider ?? TimeProvider.System).GetUtcNow();
        return this;
    }

    public EmbedFactory WithColor(int? color)
    {
        _color = color;
        return this;
    }

    public EmbedFactory WithColor(byte red, byte green, byte blue)
    {
        _color = (red << 16) | (green << 8) | blue;
        return this;
    }

    public EmbedFactory WithAuthor(string? name, string? url = null, string? iconUrl = null)
    {
        _author = name is null
            ? null
            : new DiscordEmbedAuthor(Limit.Required(name, DiscordLimits.EmbedAuthorName, nameof(name)), url, iconUrl,
                null);

        return this;
    }

    public EmbedFactory WithAuthor(DiscordUser user, string? url = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        return WithAuthor(user.DisplayName, url, user.AvatarUrl);
    }

    public EmbedFactory WithFooter(string? text, string? iconUrl = null)
    {
        _footer = text is null
            ? null
            : new DiscordEmbedFooter(Limit.Required(text, DiscordLimits.EmbedFooterText, nameof(text)), iconUrl, null);

        return this;
    }

    public EmbedFactory WithImage(string? url)
    {
        _image = url is null ? null : new DiscordEmbedMedia(url, null, null, null);
        return this;
    }

    public EmbedFactory WithImage(DiscordFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return WithImage(file.AttachmentUrl);
    }

    public EmbedFactory WithThumbnail(string? url)
    {
        _thumbnail = url is null ? null : new DiscordEmbedMedia(url, null, null, null);
        return this;
    }

    public EmbedFactory WithThumbnail(DiscordFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return WithThumbnail(file.AttachmentUrl);
    }

    public EmbedFactory AddField(string name, string value, bool inline = false)
    {
        Limit.Count(_fields.Count + 1, DiscordLimits.EmbedFields, nameof(name));

        _fields.Add(new DiscordEmbedField(
            Limit.Required(name, DiscordLimits.EmbedFieldName, nameof(name)),
            Limit.Required(value, DiscordLimits.EmbedFieldValue, nameof(value)),
            inline));

        return this;
    }

    public EmbedFactory AddFieldWhen(bool condition, string name, string value, bool inline = false) =>
        condition ? AddField(name, value, inline) : this;

    public EmbedFactory AddFields(IEnumerable<DiscordEmbedField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        foreach (var field in fields)
            AddField(field.Name, field.Value, field.Inline);

        return this;
    }

    public EmbedFactory RemoveField(string name)
    {
        _fields.RemoveAll(entry => string.Equals(entry.Name, name, StringComparison.Ordinal));
        return this;
    }

    public EmbedFactory ClearFields()
    {
        _fields.Clear();
        return this;
    }

    public DiscordEmbed Build()
    {
        if (Length > DiscordLimits.EmbedTotal)
            throw new InvalidOperationException(
                $"The embed holds {Length} characters but Discord accepts at most {DiscordLimits.EmbedTotal}.");

        if (_title is null && _description is null && _fields.Count == 0 && _image is null && _author is null)
            throw new InvalidOperationException("The embed carries no visible content.");

        return new DiscordEmbed(_title, _description, _url, _type, _timestamp, _color, _author, _footer, _image,
            _thumbnail, null, null, _fields.ToArray());
    }

    public static implicit operator DiscordEmbed(EmbedFactory factory) => factory.Build();
}

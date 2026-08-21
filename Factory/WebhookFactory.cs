using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Factory;

public sealed class WebhookFactory
{
    private static readonly string[] ForbiddenNameParts = ["clyde", "discord"];

    private string? _name;
    private string? _avatarData;
    private Snowflake? _channelId;

    public static WebhookFactory Create(string name) => new WebhookFactory().WithName(name);

    public static WebhookFactory Modify() => new();

    public static WebhookFactory From(DiscordWebhook webhook)
    {
        ArgumentNullException.ThrowIfNull(webhook);

        return new WebhookFactory
        {
            _name = webhook.Name,
            _channelId = webhook.ChannelId
        };
    }

    public WebhookFactory WithName(string name)
    {
        _name = ValidateName(name);
        return this;
    }

    public WebhookFactory WithAvatar(string? dataUri)
    {
        _avatarData = dataUri;
        return this;
    }

    public WebhookFactory WithAvatar(ReadOnlySpan<byte> image, string mediaType)
    {
        _avatarData = AvatarData(image, mediaType);
        return this;
    }

    public WebhookFactory WithoutAvatar()
    {
        _avatarData = null;
        return this;
    }

    public WebhookFactory MoveTo(Snowflake channelId)
    {
        _channelId = channelId;
        return this;
    }

    public WebhookCreateRequest Build()
    {
        if (_name is not { } name)
            throw new InvalidOperationException("A webhook cannot be created without a name.");

        return new WebhookCreateRequest(name, _avatarData);
    }

    public WebhookModifyRequest BuildModify()
    {
        if (_name is null && _avatarData is null && _channelId is null)
            throw new InvalidOperationException("The modification would not change anything.");

        return new WebhookModifyRequest(_name, _avatarData, _channelId);
    }

    public Task<DiscordWebhook> CreateAsync(IDiscordRest rest, Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.CreateWebhookAsync(channelId, Build(), reason, cancellationToken);
    }

    public Task<DiscordWebhook> ApplyAsync(IDiscordRest rest, Snowflake webhookId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.ModifyWebhookAsync(webhookId, BuildModify(), reason, cancellationToken);
    }

    public static string AvatarData(ReadOnlySpan<byte> image, string mediaType)
    {
        if (image.IsEmpty)
            throw new ArgumentException("The avatar image is empty.", nameof(image));

        if (string.IsNullOrWhiteSpace(mediaType))
            throw new ArgumentException("The avatar media type is empty.", nameof(mediaType));

        return $"data:{mediaType};base64,{Convert.ToBase64String(image)}";
    }

    internal static string ValidateName(string name)
    {
        Limit.Required(name, DiscordLimits.WebhookName, nameof(name));

        foreach (var forbidden in ForbiddenNameParts)
        {
            if (name.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"A webhook name must not contain \"{forbidden}\".", nameof(name));
        }

        return name;
    }
}

public sealed class WebhookMessageFactory
{
    private readonly MessageFactory _message = MessageFactory.Create();

    private string? _username;
    private string? _avatarUrl;
    private string? _threadName;

    public static WebhookMessageFactory Create() => new();

    public static WebhookMessageFactory Create(string content) => new WebhookMessageFactory().WithContent(content);

    public static WebhookMessageFactory Impersonating(DiscordUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new WebhookMessageFactory().As(user.DisplayName, user.AvatarUrl);
    }

    public WebhookMessageFactory WithContent(string? content)
    {
        _message.WithContent(content);
        return this;
    }

    public WebhookMessageFactory Append(string? text)
    {
        _message.Append(text);
        return this;
    }

    public WebhookMessageFactory AppendLine(string? text = null)
    {
        _message.AppendLine(text);
        return this;
    }

    public WebhookMessageFactory AddEmbed(DiscordEmbed embed)
    {
        _message.AddEmbed(embed);
        return this;
    }

    public WebhookMessageFactory AddEmbed(EmbedFactory embed)
    {
        _message.AddEmbed(embed);
        return this;
    }

    public WebhookMessageFactory AddEmbed(Action<EmbedFactory> configure)
    {
        _message.AddEmbed(configure);
        return this;
    }

    public WebhookMessageFactory ClearEmbeds()
    {
        _message.ClearEmbeds();
        return this;
    }

    public WebhookMessageFactory AddButton(DiscordButton button)
    {
        _message.AddButton(button);
        return this;
    }

    public WebhookMessageFactory AddButton(string customId, string label, ButtonStyle style = ButtonStyle.Primary)
    {
        _message.AddButton(customId, label, style);
        return this;
    }

    public WebhookMessageFactory AddLink(string url, string label)
    {
        _message.AddLink(url, label);
        return this;
    }

    public WebhookMessageFactory AddSelect(DiscordSelectMenu select)
    {
        _message.AddSelect(select);
        return this;
    }

    public WebhookMessageFactory AddRow(params DiscordComponent[] components)
    {
        _message.AddRow(components);
        return this;
    }

    public WebhookMessageFactory WithComponents(Action<ComponentFactory> configure)
    {
        _message.WithComponents(configure);
        return this;
    }

    public WebhookMessageFactory WithoutComponents()
    {
        _message.WithoutComponents();
        return this;
    }


    public WebhookMessageFactory AddFile(DiscordFile file)
    {
        _message.AddFile(file);
        return this;
    }

    public WebhookMessageFactory AddFile(string fileName, ReadOnlyMemory<byte> content, string? description = null)
    {
        _message.AddFile(fileName, content, description);
        return this;
    }

    public WebhookMessageFactory AddFileText(string fileName, string content, string? description = null)
    {
        _message.AddFileText(fileName, content, description);
        return this;
    }

    public WebhookMessageFactory AddFiles(IEnumerable<DiscordFile> files)
    {
        _message.AddFiles(files);
        return this;
    }

    public WebhookMessageFactory AddImage(DiscordFile file, Action<EmbedFactory>? configure = null)
    {
        _message.AddImage(file, configure);
        return this;
    }

    public WebhookMessageFactory ClearFiles()
    {
        _message.ClearFiles();
        return this;
    }

    public WebhookMessageFactory AsTts(bool tts = true)
    {
        _message.AsTts(tts);
        return this;
    }

    public WebhookMessageFactory As(string? username, string? avatarUrl = null)
    {
        _username = Limit.Text(username, DiscordLimits.WebhookUsername, nameof(username));
        _avatarUrl = avatarUrl;

        return this;
    }

    public WebhookMessageFactory InNewPost(string threadName)
    {
        _threadName = Limit.Required(threadName, DiscordLimits.ThreadName, nameof(threadName));
        return this;
    }

    public WebhookExecuteRequest Build()
    {
        if (_message.IsEmpty)
            throw new InvalidOperationException("A webhook message needs content or at least one embed.");

        return _message.BuildWebhookExecute(_username, _avatarUrl, _threadName);
    }

    public Task<DiscordMessage?> SendAsync(IDiscordRest rest, DiscordWebhook webhook, Snowflake? threadId = null,
        bool wait = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.ExecuteWebhookAsync(webhook, Build(), threadId, wait, cancellationToken);
    }
}

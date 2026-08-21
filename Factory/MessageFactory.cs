using System.Text;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Factory;

public sealed class MessageFactory
{
    private readonly List<DiscordEmbed> _embeds = [];
    private readonly List<DiscordFile> _files = [];
    private readonly StringBuilder _content = new();

    private ComponentFactory? _components;
    private List<Snowflake>? _keptAttachments;
    private DiscordMessageReference? _reference;
    private bool _tts;

    public static MessageFactory Create() => new();

    public static MessageFactory Create(string content) => new MessageFactory().WithContent(content);

    public static MessageFactory From(DiscordMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var factory = new MessageFactory();
        factory._content.Append(message.Content);
        factory._embeds.AddRange(message.Embeds);

        if (message.HasComponents)
            factory._components = ComponentFactory.From(message);

        return factory;
    }

    public int ContentLength => _content.Length;

    public int EmbedCount => _embeds.Count;

    public int FileCount => _files.Count;

    public int RowCount => _components?.RowCount ?? 0;

    public bool IsEmpty => _content.Length == 0 && _embeds.Count == 0 && _files.Count == 0 && RowCount == 0;

    public MessageFactory WithContent(string? content)
    {
        _content.Clear();

        if (content is not null)
            _content.Append(Limit.Text(content, DiscordLimits.MessageContent, nameof(content)));

        return this;
    }

    public MessageFactory Append(string? text)
    {
        if (text is not null)
            _content.Append(text);

        return this;
    }

    public MessageFactory AppendLine(string? text = null)
    {
        if (text is not null)
            _content.Append(text);

        _content.Append('\n');

        return this;
    }

    public MessageFactory AppendWhen(bool condition, string text) => condition ? Append(text) : this;

    public MessageFactory Mention(DiscordUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return Append(user.Mention);
    }

    public MessageFactory AddEmbed(DiscordEmbed embed)
    {
        ArgumentNullException.ThrowIfNull(embed);
        Limit.Count(_embeds.Count + 1, DiscordLimits.MessageEmbeds, nameof(embed));

        _embeds.Add(embed);

        return this;
    }

    public MessageFactory AddEmbed(EmbedFactory embed)
    {
        ArgumentNullException.ThrowIfNull(embed);

        return AddEmbed(embed.Build());
    }

    public MessageFactory AddEmbed(Action<EmbedFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var embed = EmbedFactory.Create();
        configure(embed);

        return AddEmbed(embed.Build());
    }

    public MessageFactory AddEmbeds(IEnumerable<DiscordEmbed> embeds)
    {
        ArgumentNullException.ThrowIfNull(embeds);

        foreach (var embed in embeds)
            AddEmbed(embed);

        return this;
    }

    public MessageFactory ClearEmbeds()
    {
        _embeds.Clear();
        return this;
    }

    public MessageFactory AddFile(DiscordFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        Limit.Count(_files.Count + 1, DiscordLimits.MessageFiles, nameof(file));
        Limit.Text(file.Description, DiscordLimits.AttachmentDescription, nameof(file));

        _files.Add(file);

        return this;
    }

    public MessageFactory AddFile(string fileName, ReadOnlyMemory<byte> content, string? description = null) =>
        AddFile(DiscordFile.FromBytes(fileName, content, description));

    public MessageFactory AddFileText(string fileName, string content, string? description = null) =>
        AddFile(DiscordFile.FromText(fileName, content, description));

    public MessageFactory AddFiles(IEnumerable<DiscordFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        foreach (var file in files)
            AddFile(file);

        return this;
    }

    public MessageFactory AddImage(DiscordFile file, Action<EmbedFactory>? configure = null)
    {
        AddFile(file);

        var embed = EmbedFactory.Create().WithImage(file);
        configure?.Invoke(embed);

        return AddEmbed(embed.Build());
    }

    public MessageFactory ClearFiles()
    {
        _files.Clear();

        return this;
    }

    public MessageFactory KeepAttachments(IEnumerable<DiscordAttachment> attachments)
    {
        ArgumentNullException.ThrowIfNull(attachments);

        return KeepAttachments(attachments.Select(attachment => attachment.Id));
    }

    public MessageFactory KeepAttachments(IEnumerable<Snowflake> attachmentIds)
    {
        ArgumentNullException.ThrowIfNull(attachmentIds);

        _keptAttachments ??= [];
        _keptAttachments.AddRange(attachmentIds);

        return this;
    }

    public MessageFactory DropAttachments()
    {
        _keptAttachments = [];

        return this;
    }


    public MessageFactory AddButton(DiscordButton button)
    {
        Components().AddButton(button);

        return this;
    }

    public MessageFactory AddButton(string customId, string label, ButtonStyle style = ButtonStyle.Primary)
    {
        Components().AddButton(customId, label, style);

        return this;
    }

    public MessageFactory AddLink(string url, string label)
    {
        Components().AddLink(url, label);

        return this;
    }

    public MessageFactory AddSelect(DiscordSelectMenu select)
    {
        Components().AddSelect(select);

        return this;
    }

    public MessageFactory AddSelect(string customId, params DiscordSelectOption[] options)
    {
        Components().AddSelect(customId, options);

        return this;
    }

    public MessageFactory AddRow(params DiscordComponent[] components)
    {
        Components().AddRow(components);

        return this;
    }

    public MessageFactory NewRow()
    {
        Components().NewRow();

        return this;
    }

    public MessageFactory WithComponents(Action<ComponentFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Components());

        return this;
    }

    public MessageFactory WithComponents(IEnumerable<DiscordComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        foreach (var component in components)
            if (component is DiscordActionRow row)
                Components().AddRow(row);
            else
                Components().AddRow(component);

        return this;
    }

    public MessageFactory WithoutComponents()
    {
        Components().Clear();

        return this;
    }

    public MessageFactory AsTts(bool tts = true)
    {
        _tts = tts;
        return this;
    }

    public MessageFactory ReplyTo(DiscordMessage message, bool failIfNotExists = false)
    {
        ArgumentNullException.ThrowIfNull(message);

        _reference = new DiscordMessageReference(message.Id, message.ChannelId, message.GuildId,
            MessageReferenceType.Default, failIfNotExists);

        return this;
    }

    public MessageFactory ReplyTo(Snowflake messageId, Snowflake? channelId = null, Snowflake? guildId = null,
        bool failIfNotExists = false)
    {
        _reference = new DiscordMessageReference(messageId, channelId, guildId, MessageReferenceType.Default,
            failIfNotExists);

        return this;
    }

    public MessageFactory Forward(DiscordMessage message, bool failIfNotExists = false)
    {
        ArgumentNullException.ThrowIfNull(message);

        _reference = new DiscordMessageReference(message.Id, message.ChannelId, message.GuildId,
            MessageReferenceType.Forward, failIfNotExists);

        return this;
    }

    public MessageFactory WithoutReference()
    {
        _reference = null;
        return this;
    }

    public MessageCreateRequest Build() =>
        new(Content(), Embeds(), _reference, _tts, Files(), Rows());

    public MessageEditRequest BuildEdit() =>
        new(Content(), Embeds(), Files(), _keptAttachments?.ToArray(), Rows());

    public WebhookExecuteRequest BuildWebhookExecute(string? username = null, string? avatarUrl = null,
        string? threadName = null) =>
        new(Content(), Embeds(), username, avatarUrl, threadName, _tts, Files(), Rows());

    public Task<DiscordMessage> SendAsync(IDiscordRest rest, Snowflake channelId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.CreateMessageAsync(channelId, Build(), cancellationToken);
    }

    public Task<DiscordMessage> ApplyAsync(IDiscordRest rest, Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.EditMessageAsync(channelId, messageId, BuildEdit(), cancellationToken);
    }

    private string? Content()
    {
        if (_content.Length == 0)
            return null;

        if (_content.Length > DiscordLimits.MessageContent)
            throw new InvalidOperationException(
                $"The message holds {_content.Length} characters but Discord accepts at most " +
                $"{DiscordLimits.MessageContent}.");

        return _content.ToString();
    }

    private IReadOnlyList<DiscordEmbed>? Embeds() => _embeds.Count == 0 ? null : _embeds.ToArray();

    private IReadOnlyList<DiscordFile>? Files() => _files.Count == 0 ? null : _files.ToArray();

    private IReadOnlyList<DiscordComponent>? Rows() => _components?.Build();

    private ComponentFactory Components() => _components ??= ComponentFactory.Create();
}

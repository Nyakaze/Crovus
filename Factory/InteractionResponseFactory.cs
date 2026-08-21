using Crovus.Models;

namespace Crovus.Factory;

public sealed class InteractionResponseFactory
{
    private readonly List<DiscordEmbed> _embeds = [];
    private readonly List<DiscordFile> _files = [];

    private ComponentFactory? _components;
    private List<Snowflake>? _keptAttachments;
    private string? _content;
    private bool _ephemeral;
    private bool _tts;
    private MessageFlags _flags = MessageFlags.None;

    public static InteractionResponseFactory Create() => new();

    public static InteractionResponseFactory Create(string content) =>
        new InteractionResponseFactory().WithContent(content);

    public static InteractionResponseFactory Ephemeral(string content) =>
        Create(content).AsEphemeral();

    public int EmbedCount => _embeds.Count;

    public int FileCount => _files.Count;

    public int RowCount => _components?.RowCount ?? 0;

    public bool IsEmpty =>
        string.IsNullOrEmpty(_content) && _embeds.Count == 0 && _files.Count == 0 && RowCount == 0;

    public InteractionResponseFactory WithContent(string? content)
    {
        _content = Limit.Text(content, DiscordLimits.MessageContent, nameof(content));

        return this;
    }

    public InteractionResponseFactory AddEmbed(DiscordEmbed embed)
    {
        ArgumentNullException.ThrowIfNull(embed);
        Limit.Count(_embeds.Count + 1, DiscordLimits.MessageEmbeds, nameof(embed));

        _embeds.Add(embed);

        return this;
    }

    public InteractionResponseFactory AddEmbed(EmbedFactory embed)
    {
        ArgumentNullException.ThrowIfNull(embed);

        return AddEmbed(embed.Build());
    }

    public InteractionResponseFactory AddEmbed(Action<EmbedFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var embed = EmbedFactory.Create();
        configure(embed);

        return AddEmbed(embed.Build());
    }

    public InteractionResponseFactory AddEmbeds(IEnumerable<DiscordEmbed> embeds)
    {
        ArgumentNullException.ThrowIfNull(embeds);

        foreach (var embed in embeds)
            AddEmbed(embed);

        return this;
    }

    public InteractionResponseFactory ClearEmbeds()
    {
        _embeds.Clear();

        return this;
    }

    public InteractionResponseFactory AddFile(DiscordFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        Limit.Count(_files.Count + 1, DiscordLimits.MessageFiles, nameof(file));
        Limit.Text(file.Description, DiscordLimits.AttachmentDescription, nameof(file));

        _files.Add(file);

        return this;
    }

    public InteractionResponseFactory AddFile(string fileName, ReadOnlyMemory<byte> content,
        string? description = null) =>
        AddFile(DiscordFile.FromBytes(fileName, content, description));

    public InteractionResponseFactory AddFileText(string fileName, string content, string? description = null) =>
        AddFile(DiscordFile.FromText(fileName, content, description));

    public InteractionResponseFactory AddFiles(IEnumerable<DiscordFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        foreach (var file in files)
            AddFile(file);

        return this;
    }

    public InteractionResponseFactory AddImage(DiscordFile file, Action<EmbedFactory>? configure = null)
    {
        AddFile(file);

        var embed = EmbedFactory.Create().WithImage(file);
        configure?.Invoke(embed);

        return AddEmbed(embed.Build());
    }

    public InteractionResponseFactory ClearFiles()
    {
        _files.Clear();

        return this;
    }

    public InteractionResponseFactory KeepAttachments(IEnumerable<Snowflake> attachmentIds)
    {
        ArgumentNullException.ThrowIfNull(attachmentIds);

        _keptAttachments ??= [];
        _keptAttachments.AddRange(attachmentIds);

        return this;
    }

    public InteractionResponseFactory DropAttachments()
    {
        _keptAttachments = [];

        return this;
    }


    public InteractionResponseFactory AddButton(DiscordButton button)
    {
        Components().AddButton(button);

        return this;
    }

    public InteractionResponseFactory AddButton(string customId, string label, ButtonStyle style = ButtonStyle.Primary)
    {
        Components().AddButton(customId, label, style);

        return this;
    }

    public InteractionResponseFactory AddLink(string url, string label)
    {
        Components().AddLink(url, label);

        return this;
    }

    public InteractionResponseFactory AddSelect(DiscordSelectMenu select)
    {
        Components().AddSelect(select);

        return this;
    }

    public InteractionResponseFactory AddSelect(string customId, params DiscordSelectOption[] options)
    {
        Components().AddSelect(customId, options);

        return this;
    }

    public InteractionResponseFactory AddRow(params DiscordComponent[] components)
    {
        Components().AddRow(components);

        return this;
    }

    public InteractionResponseFactory NewRow()
    {
        Components().NewRow();

        return this;
    }

    public InteractionResponseFactory WithComponents(Action<ComponentFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Components());

        return this;
    }

    public InteractionResponseFactory WithComponents(IEnumerable<DiscordComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        foreach (var component in components)
            if (component is DiscordActionRow row)
                Components().AddRow(row);
            else
                Components().AddRow(component);

        return this;
    }

    public InteractionResponseFactory WithoutComponents()
    {
        Components().Clear();

        return this;
    }

    public InteractionResponseFactory AsEphemeral(bool ephemeral = true)
    {
        _ephemeral = ephemeral;

        return this;
    }

    public InteractionResponseFactory AsTts(bool tts = true)
    {
        _tts = tts;

        return this;
    }

    public InteractionResponseFactory AsSilent(bool silent = true) =>
        Toggle(MessageFlags.SuppressNotifications, silent);

    public InteractionResponseFactory SuppressEmbeds(bool suppress = true) =>
        Toggle(MessageFlags.SuppressEmbeds, suppress);

    public InteractionMessageRequest Build() =>
        new(_content, _embeds.Count == 0 ? null : _embeds.ToArray(), _ephemeral, _tts, _flags,
            _files.Count == 0 ? null : _files.ToArray(), _keptAttachments?.ToArray(), _components?.Build());

    public InteractionResponseRequest BuildResponse(
        InteractionCallbackType type = InteractionCallbackType.ChannelMessageWithSource) =>
        new(type, Build());

    private ComponentFactory Components() => _components ??= ComponentFactory.Create();

    private InteractionResponseFactory Toggle(MessageFlags flag, bool enabled)
    {
        _flags = enabled ? _flags | flag : _flags & ~flag;

        return this;
    }
}

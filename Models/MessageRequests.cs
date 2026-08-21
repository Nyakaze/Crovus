namespace Crovus.Models;

public sealed record MessageCreateRequest(string? Content = null, IReadOnlyList<DiscordEmbed>? Embeds = null,
    DiscordMessageReference? Reply = null, bool Tts = false, IReadOnlyList<DiscordFile>? Files = null,
    IReadOnlyList<DiscordComponent>? Components = null)
{
    public bool HasFiles => Files is { Count: > 0 };

    public bool HasComponents => Components is { Count: > 0 };

    public MessageCreateRequest Showing(params DiscordComponent[] components) =>
        this with { Components = [.. Components ?? [], .. components] };

    public MessageCreateRequest Attaching(DiscordFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return this with { Files = [.. Files ?? [], file] };
    }

    public MessageCreateRequest Attaching(IEnumerable<DiscordFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        return this with { Files = [.. Files ?? [], .. files] };
    }
}

public sealed record MessageEditRequest(string? Content = null, IReadOnlyList<DiscordEmbed>? Embeds = null,
    IReadOnlyList<DiscordFile>? Files = null, IReadOnlyList<Snowflake>? KeptAttachments = null,
    IReadOnlyList<DiscordComponent>? Components = null)
{
    public bool HasFiles => Files is { Count: > 0 };

    public bool HasComponents => Components is { Count: > 0 };

    public bool RewritesAttachments => HasFiles || KeptAttachments is not null;

    public MessageEditRequest Showing(params DiscordComponent[] components) =>
        this with { Components = [.. Components ?? [], .. components] };

    public MessageEditRequest WithoutComponents() => this with { Components = [] };

    public MessageEditRequest Attaching(DiscordFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return this with { Files = [.. Files ?? [], file] };
    }

    public MessageEditRequest Keeping(IEnumerable<Snowflake> attachmentIds)
    {
        ArgumentNullException.ThrowIfNull(attachmentIds);

        return this with { KeptAttachments = [.. KeptAttachments ?? [], .. attachmentIds] };
    }

    public MessageEditRequest Keeping(IEnumerable<DiscordAttachment> attachments)
    {
        ArgumentNullException.ThrowIfNull(attachments);

        return Keeping(attachments.Select(attachment => attachment.Id));
    }

    public MessageEditRequest DroppingAttachments() => this with { KeptAttachments = [] };
}

public sealed record MessageQuery
{
    public Snowflake? Before { get; init; }

    public Snowflake? After { get; init; }

    public Snowflake? Around { get; init; }

    public int? Limit { get; init; }

    public bool IsAnchored => Around is not null;

    public static MessageQuery Newest(int limit) => new() { Limit = limit };

    public static MessageQuery BeforeMessage(Snowflake messageId, int? limit = null) =>
        new() { Before = messageId, Limit = limit };

    public static MessageQuery AfterMessage(Snowflake messageId, int? limit = null) =>
        new() { After = messageId, Limit = limit };

    public static MessageQuery AroundMessage(Snowflake messageId, int limit = 50) =>
        new() { Around = messageId, Limit = limit };
}

public sealed record ReactionQuery
{
    public Snowflake? After { get; init; }

    public int? Limit { get; init; }
}

using System.Text;

namespace Crovus.Models;

public sealed record DiscordFile
{
    private const string SpoilerPrefix = "SPOILER_";

    public required string FileName { get; init; }

    public required ReadOnlyMemory<byte> Content { get; init; }

    public string? Description { get; init; }

    public string? ContentType { get; init; }

    public bool Spoiler { get; init; }

    public int Size => Content.Length;

    public bool IsEmpty => Content.Length == 0;

    public string UploadName =>
        Spoiler && !FileName.StartsWith(SpoilerPrefix, StringComparison.Ordinal)
            ? SpoilerPrefix + FileName
            : FileName;

    public string AttachmentUrl => $"attachment://{UploadName}";

    public static DiscordFile FromBytes(string fileName, ReadOnlyMemory<byte> content, string? description = null,
        string? contentType = null) =>
        new()
        {
            FileName = Named(fileName),
            Content = content,
            Description = description,
            ContentType = contentType
        };

    public static DiscordFile FromText(string fileName, string content, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        return FromBytes(fileName, Encoding.UTF8.GetBytes(content), description, "text/plain; charset=utf-8");
    }

    public static async Task<DiscordFile> FromStreamAsync(string fileName, Stream stream, string? description = null,
        string? contentType = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        return FromBytes(fileName, buffer.ToArray(), description, contentType);
    }

    public static async Task<DiscordFile> FromPathAsync(string path, string? fileName = null,
        string? description = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var content = await File.ReadAllBytesAsync(path, cancellationToken);

        return FromBytes(fileName ?? Path.GetFileName(path), content, description);
    }

    public DiscordFile AsSpoiler(bool spoiler = true) => this with { Spoiler = spoiler };

    public DiscordFile DescribedAs(string? description) => this with { Description = description };

    public DiscordFile Renamed(string fileName) => this with { FileName = Named(fileName) };

    public DiscordFile As(string contentType) => this with { ContentType = contentType };

    private static string Named(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return fileName;
    }
}

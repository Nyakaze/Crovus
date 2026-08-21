using System.Net.Http.Headers;
using System.Net.Http.Json;
using Crovus.Factory;
using Crovus.Json;
using Crovus.Models;

namespace Crovus.Rest;

internal static class AttachmentUpload
{
    private const string FallbackContentType = "application/octet-stream";

    public static HttpContent Build<TPayload>(TPayload payload, IReadOnlyList<DiscordFile> files)
    {
        var content = new MultipartFormDataContent();
        var json = JsonContent.Create(payload, options: DiscordJson.Options);

        json.Headers.ContentDisposition = Disposition("payload_json", null);
        content.Add(json);

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            var part = new ReadOnlyMemoryContent(file.Content);

            part.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType ?? ContentTypeOf(file.UploadName));
            part.Headers.ContentDisposition = Disposition($"files[{index}]", file.UploadName);
            content.Add(part);
        }

        return content;
    }

    public static void Validate(IReadOnlyList<DiscordFile> files, string field)
    {
        Limit.Count(files.Count, DiscordLimits.MessageFiles, field);

        var names = new HashSet<string>(files.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            ArgumentNullException.ThrowIfNull(file, field);
            ArgumentException.ThrowIfNullOrWhiteSpace(file.FileName, field);
            Limit.Text(file.Description, DiscordLimits.AttachmentDescription, field);

            if (!names.Add(file.UploadName))
                throw new ArgumentException($"{field} contains more than one file named {file.UploadName}.", field);
        }
    }

    public static long TotalBytes(IReadOnlyList<DiscordFile> files)
    {
        var total = 0L;

        foreach (var file in files)
            total += file.Size;

        return total;
    }

    private static ContentDispositionHeaderValue Disposition(string name, string? fileName) =>
        new("form-data")
        {
            Name = Quoted(name),
            FileName = fileName is null ? null : Quoted(fileName)
        };

    private static string Quoted(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    private static string ContentTypeOf(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".wav" => "audio/wav",
            ".txt" or ".log" or ".md" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            _ => FallbackContentType
        };
}

using System.Net;
using System.Text.Json;

namespace Crovus.Rest;

public sealed class DiscordRestException : Exception
{
    public DiscordRestException(string message, HttpStatusCode statusCode, int? errorCode = null,
        string? route = null, int attempt = 1, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Route = route;
        Attempt = attempt;
    }

    public HttpStatusCode StatusCode { get; }

    public int? ErrorCode { get; }

    public string? Route { get; }

    public int Attempt { get; }

    internal static async Task<DiscordRestException> FromResponseAsync(RouteKey route, HttpResponseMessage response,
        int attempt, CancellationToken cancellationToken)
    {
        var body = await ReadBodyAsync(response, cancellationToken);
        var (code, detail) = Parse(body);

        var message = detail is null
            ? $"{route} failed with {(int)response.StatusCode} {response.ReasonPhrase}."
            : $"{route} failed with {(int)response.StatusCode} {response.ReasonPhrase}: {detail}";

        return new DiscordRestException(message, response.StatusCode, code, route.ToString(), attempt);
    }

    private static async Task<string?> ReadBodyAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static (int? Code, string? Message) Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (null, null);

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind is not JsonValueKind.Object)
                return (null, body);

            var code = root.TryGetProperty("code", out var rawCode) && rawCode.ValueKind is JsonValueKind.Number
                ? rawCode.GetInt32()
                : (int?)null;

            var message = root.TryGetProperty("message", out var rawMessage)
                ? rawMessage.GetString()
                : null;

            return (code, string.IsNullOrWhiteSpace(message) ? body : message);
        }
        catch (JsonException)
        {
            return (null, body);
        }
    }
}

using Crovus.Models;

namespace Crovus.RichPresence;

public enum RichPresenceState
{
    Idle,
    Connecting,
    Connected,
    Disconnected,
    Stopped
}

public enum RichPresenceFailureKind
{
    DiscordNotRunning,
    AccessDenied,
    Unsupported,
    HandshakeRejected,
    HandshakeTimeout,
    ConnectionLost,
    RequestTimeout,
    ActivityRejected,
    ProtocolError
}

public sealed record RichPresenceFailure(RichPresenceFailureKind Kind, string Message, int? Code = null,
    Exception? Exception = null)
{
    public override string ToString() => Code is { } code ? $"{Kind} ({code}): {Message}" : $"{Kind}: {Message}";
}

public sealed record RichPresenceStateChange(RichPresenceState Previous, RichPresenceState Current,
    RichPresenceFailure? Failure);

public sealed record RichPresenceUser(Snowflake Id, string Username, string? GlobalName, string? Avatar)
{
    public string DisplayName => GlobalName ?? Username;
}

public sealed class RichPresenceException(RichPresenceFailure failure)
    : Exception(failure.Message, failure.Exception)
{
    public RichPresenceFailure Failure { get; } = failure;
}

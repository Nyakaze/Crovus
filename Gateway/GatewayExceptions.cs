namespace Crovus.Gateway;

public class GatewayException : Exception
{
    public GatewayException(string message, int? closeCode = null, Exception? innerException = null)
        : base(message, innerException) =>
        CloseCode = closeCode;

    public int? CloseCode { get; }
}

public sealed class GatewayFatalException(string message, int? closeCode = null)
    : GatewayException(message, closeCode);

internal sealed class GatewayReconnectSignal(string message, int? closeCode = null)
    : GatewayException(message, closeCode);

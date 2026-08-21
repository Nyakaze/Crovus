namespace Crovus.Gateway;

public enum GatewayState
{
    Disconnected,
    Connecting,
    Identifying,
    Resuming,
    Ready,
    Reconnecting
}

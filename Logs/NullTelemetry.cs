namespace Crovus.Logs;

public sealed class NullTelemetry : ITelemetry
{
    public static readonly NullTelemetry Instance = new();

    private NullTelemetry()
    {
    }

    public bool HasSubscribers => false;

    public void Emit(TelemetryEvent telemetryEvent)
    {
    }
}

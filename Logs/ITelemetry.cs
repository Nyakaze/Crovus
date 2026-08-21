namespace Crovus.Logs;

public interface ITelemetry
{
    bool HasSubscribers { get; }

    void Emit(TelemetryEvent telemetryEvent);
}

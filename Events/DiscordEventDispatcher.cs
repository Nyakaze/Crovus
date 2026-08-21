using System.Diagnostics;
using Crovus.Logs;

namespace Crovus.Events;

public sealed class DiscordEventDispatcher
{
    private const string LogCategory = "Client.Events";

    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private readonly Lock _gate = new();

    private Registration[] _registrations = [];

    public DiscordEventDispatcher(ILogger? logger = null, ITelemetry? telemetry = null)
    {
        _logger = (logger ?? NullLogger.Instance).ForCategory(LogCategory);
        _telemetry = telemetry ?? NullTelemetry.Instance;
    }

    public DiscordEventDispatcher(DiagnosticsHub diagnostics)
        : this(diagnostics, diagnostics)
    {
    }

    public int HandlerCount => Volatile.Read(ref _registrations).Length;

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler, string? name = null)
        where TEvent : DiscordEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(typeof(TEvent), name ?? Describe(handler.Method),
            (discordEvent, cancellationToken) => handler((TEvent)discordEvent, cancellationToken));
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler, string? name = null)
        where TEvent : DiscordEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(typeof(TEvent), name ?? Describe(handler.Method),
            (discordEvent, _) => handler((TEvent)discordEvent));
    }

    public IDisposable SubscribeAll(Func<DiscordEvent, CancellationToken, Task> handler, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Register(typeof(DiscordEvent), name ?? Describe(handler.Method), handler);
    }

    public async Task DispatchAsync(DiscordEvent discordEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(discordEvent);

        var registrations = Volatile.Read(ref _registrations);

        if (registrations.Length == 0)
            return;

        var start = Stopwatch.GetTimestamp();
        var invoked = 0;

        foreach (var registration in registrations)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (!registration.Accepts(discordEvent))
                continue;

            invoked++;

            await InvokeAsync(registration, discordEvent, cancellationToken);
        }

        if (invoked == 0)
            return;

        var duration = Stopwatch.GetElapsedTime(start);

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace(
                $"Dispatched {discordEvent.Name} to {invoked} handlers in {duration.TotalMilliseconds:F0}ms");

        if (_telemetry.HasSubscribers)
            _telemetry.Emit(new EventDispatched(discordEvent.Name, invoked, duration));
    }

    private async Task InvokeAsync(Registration registration, DiscordEvent discordEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            await registration.Invoke(discordEvent, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError($"Handler {registration.Name} failed on {discordEvent.Name}", exception);

            if (_telemetry.HasSubscribers)
                _telemetry.Emit(new EventHandlerFailed(discordEvent.Name, registration.Name,
                    exception.GetType().Name));
        }
    }

    private IDisposable Register(Type eventType, string name,
        Func<DiscordEvent, CancellationToken, Task> invoke)
    {
        var registration = new Registration(this, eventType, name, invoke);

        lock (_gate)
            _registrations = [.. _registrations, registration];

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug($"Subscribed {name} to {eventType.Name}");

        return registration;
    }

    private void Remove(Registration registration)
    {
        lock (_gate)
            _registrations = [.. _registrations.Where(existing => existing != registration)];

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug($"Unsubscribed {registration.Name} from {registration.EventType.Name}");
    }

    private static string Describe(System.Reflection.MethodInfo method) =>
        method.DeclaringType is { } owner ? $"{owner.Name}.{method.Name}" : method.Name;

    private sealed class Registration(DiscordEventDispatcher owner, Type eventType, string name,
        Func<DiscordEvent, CancellationToken, Task> invoke) : IDisposable
    {
        private volatile bool _disposed;

        public Type EventType { get; } = eventType;

        public string Name { get; } = name;

        public bool Accepts(DiscordEvent discordEvent) => !_disposed && EventType.IsInstanceOfType(discordEvent);

        public Task Invoke(DiscordEvent discordEvent, CancellationToken cancellationToken) =>
            _disposed ? Task.CompletedTask : invoke(discordEvent, cancellationToken);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            owner.Remove(this);
        }
    }
}

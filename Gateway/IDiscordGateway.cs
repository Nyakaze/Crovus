using Crovus.Models;

namespace Crovus.Gateway;

public interface IDiscordGateway : IAsyncDisposable
{
    GatewayState State { get; }

    string? SessionId { get; }

    int? LastSequence { get; }

    TimeSpan? Latency { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    ValueTask SendAsync(GatewayOpcode opcode, object? payload, CancellationToken cancellationToken = default);

    ValueTask UpdatePresenceAsync(PresenceUpdate presence, CancellationToken cancellationToken = default);

    ValueTask RequestGuildMembersAsync(GuildMembersRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<GatewayEvent> ReadEventsAsync(CancellationToken cancellationToken = default);
}

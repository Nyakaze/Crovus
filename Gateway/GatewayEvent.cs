using System.Text.Json;

namespace Crovus.Gateway;

public sealed record GatewayEvent(GatewayOpcode Opcode, JsonElement? Data, int? Sequence, string? Name)
{
    public bool IsDispatch => Opcode is GatewayOpcode.Dispatch;

    public T? Deserialize<T>(JsonSerializerOptions? options = null) =>
        Data is { } data ? data.Deserialize<T>(options) : default;
}

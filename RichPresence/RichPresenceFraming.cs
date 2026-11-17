using System.Buffers.Binary;

namespace Crovus.RichPresence;

public enum RichPresenceOpcode
{
    Handshake = 0,
    Frame = 1,
    Close = 2,
    Ping = 3,
    Pong = 4
}

public readonly record struct RichPresenceFrame(RichPresenceOpcode Opcode, ReadOnlyMemory<byte> Payload);

public static class RichPresenceFraming
{
    public const int HeaderSize = 8;
    public const int MaxPayloadBytes = 1 << 20;

    public static byte[] Encode(RichPresenceOpcode opcode, ReadOnlySpan<byte> payload)
    {
        var buffer = new byte[HeaderSize + payload.Length];

        BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)opcode);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4), payload.Length);
        payload.CopyTo(buffer.AsSpan(HeaderSize));

        return buffer;
    }

    public static async ValueTask WriteAsync(Stream stream, RichPresenceOpcode opcode, ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        await stream.WriteAsync(Encode(opcode, payload.Span), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async ValueTask<RichPresenceFrame> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[HeaderSize];
        await stream.ReadExactlyAsync(header, cancellationToken);

        var opcode = (RichPresenceOpcode)BinaryPrimitives.ReadInt32LittleEndian(header);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));

        if (length is < 0 or > MaxPayloadBytes)
            throw new InvalidDataException($"Discord sent a frame with an invalid length of {length} bytes.");

        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, cancellationToken);

        return new RichPresenceFrame(opcode, payload);
    }
}

using System.Buffers.Binary;
using WirelessMic.Shared.Constants;

namespace WirelessMic.Infrastructure.Audio;

/// <summary>
/// PCM ses paketlerini serileştirir ve ayrıştırır.
/// </summary>
public static class AudioPacketSerializer
{
    /// <summary>Paket oluşturur: Sequence(4) + Timestamp(8) + Length(2) + Payload.</summary>
    public static byte[] Serialize(int sequence, long timestampUtcTicks, ReadOnlySpan<byte> payload)
    {
        var packet = new byte[AudioProtocol.HeaderSize + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(0, 4), sequence);
        BinaryPrimitives.WriteInt64BigEndian(packet.AsSpan(4, 8), timestampUtcTicks);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(12, 2), (ushort)payload.Length);
        payload.CopyTo(packet.AsSpan(AudioProtocol.HeaderSize));
        return packet;
    }

    /// <summary>Paketi ayrıştırır.</summary>
    public static bool TryDeserialize(ReadOnlySpan<byte> data, out int sequence, out long timestampUtcTicks, out ReadOnlySpan<byte> payload)
    {
        sequence = 0;
        timestampUtcTicks = 0;
        payload = ReadOnlySpan<byte>.Empty;

        if (data.Length < AudioProtocol.HeaderSize)
            return false;

        var payloadLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(12, 2));

        if (data.Length < AudioProtocol.HeaderSize + payloadLength)
            return false;

        sequence = BinaryPrimitives.ReadInt32BigEndian(data.Slice(0, 4));
        timestampUtcTicks = BinaryPrimitives.ReadInt64BigEndian(data.Slice(4, 8));
        payload = data.Slice(AudioProtocol.HeaderSize, payloadLength);
        return true;
    }
}

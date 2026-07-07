namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Ses verisini sıkıştırır (Opus vb.).
/// </summary>
public interface IAudioEncoder
{
    /// <summary>PCM verisini kodlar.</summary>
    byte[] Encode(ReadOnlySpan<byte> pcmData);
}

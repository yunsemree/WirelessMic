namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Sıkıştırılmış ses verisini çözer.
/// </summary>
public interface IAudioDecoder
{
    /// <summary>Kodlanmış veriyi PCM'e çözer.</summary>
    byte[] Decode(ReadOnlySpan<byte> encodedData);
}

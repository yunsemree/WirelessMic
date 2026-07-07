namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Gelen ses paketleri için jitter buffer yönetir.
/// </summary>
public interface IAudioBuffer
{
    /// <summary>Paketi buffer'a ekler.</summary>
    void Enqueue(int sequence, ReadOnlySpan<byte> payload);

    /// <summary>Sıradaki paketi alır.</summary>
    bool TryDequeue(out byte[]? payload);
}

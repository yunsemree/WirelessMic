namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Hoparlör üzerinden mikrofon testi dinlemesini yönetir.
/// </summary>
public interface IAudioPlayer
{
    /// <summary>Test dinlemesinin açık olup olmadığını belirtir.</summary>
    bool IsMonitoringEnabled { get; }

    /// <summary>Test dinlemesini açar veya kapatır.</summary>
    Task SetMonitoringEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>PCM karesini test dinlemesine ekler (yalnızca dinleme açıksa).</summary>
    void EnqueueFrame(ReadOnlySpan<byte> pcmFrame);
}

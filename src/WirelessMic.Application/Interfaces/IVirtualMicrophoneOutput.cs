namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Sanal mikrofon çıkışını yönetir (VB-Cable).
/// </summary>
public interface IVirtualMicrophoneOutput
{
    /// <summary>Sanal mikrofon çıkışının aktif olup olmadığını belirtir.</summary>
    bool IsActive { get; }

    /// <summary>VB-Cable hazır mı?</summary>
    bool IsVirtualCableReady { get; }

    /// <summary>Sanal mikrofon çıkışını başlatır.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Sanal mikrofon çıkışını durdurur.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>PCM karesini sanal mikrofona gönderir.</summary>
    void EnqueueFrame(ReadOnlySpan<byte> pcmFrame);
}

namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Mikrofon ses yakalama işlemlerini yönetir.
/// </summary>
public interface IMicrophoneService
{
    /// <summary>Yeni PCM karesi yakalandığında tetiklenir.</summary>
    event EventHandler<ReadOnlyMemory<byte>>? FrameCaptured;

    /// <summary>Mikrofon kaydını başlatır.</summary>
    Task StartCaptureAsync(CancellationToken cancellationToken = default);

    /// <summary>Mikrofon kaydını durdurur.</summary>
    Task StopCaptureAsync(CancellationToken cancellationToken = default);

    /// <summary>Kayıt durumunu belirtir.</summary>
    bool IsCapturing { get; }
}

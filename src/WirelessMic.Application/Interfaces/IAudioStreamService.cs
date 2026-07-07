namespace WirelessMic.Application.Interfaces;

/// <summary>
/// UDP üzerinden PCM ses akışını yönetir.
/// </summary>
public interface IAudioStreamService
{
    /// <summary>Ses akışını başlatır (telefon: gönder, masaüstü: al).</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Ses akışını durdurur.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Akışın aktif olup olmadığını belirtir.</summary>
    bool IsStreaming { get; }

    /// <summary>Yakalanan PCM karesini gönderim kuyruğuna ekler (telefon).</summary>
    void SubmitCapturedFrame(ReadOnlySpan<byte> pcmFrame);
}

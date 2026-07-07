using WirelessMic.Domain.Enums;

namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Telefon ve masaüstü arasındaki bağlantıyı yönetir.
/// </summary>
public interface IConnectionManager
{
    /// <summary>Bağlantı durumu değiştiğinde tetiklenir.</summary>
    event EventHandler<DTO.ConnectionStateChangedEventArgs>? StateChanged;

    /// <summary>Mevcut bağlantı durumu.</summary>
    ConnectionState State { get; }

    /// <summary>Aktif bağlantı olup olmadığını belirtir.</summary>
    bool IsConnected { get; }

    /// <summary>Uzak sunucu adresi (telefon modu).</summary>
    string? RemoteHost { get; }

    /// <summary>Aktif oturum kimliği.</summary>
    string? SessionId { get; }

    /// <summary>Bağlı istemci sayısı (masaüstü modu).</summary>
    int ConnectedClientCount { get; }

    /// <summary>Uzak sunucuya bağlanır.</summary>
    Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);

    /// <summary>Bağlantıyı keser.</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Gecikme ölçümü yapar (ms).</summary>
    Task<double> MeasureLatencyAsync(CancellationToken cancellationToken = default);

    /// <summary>Bağlantı sunucusunu başlatır (masaüstü modu).</summary>
    Task StartServerAsync(CancellationToken cancellationToken = default);

    /// <summary>Bağlantı sunucusunu durdurur.</summary>
    Task StopServerAsync(CancellationToken cancellationToken = default);
}

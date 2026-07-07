using WirelessMic.Domain.Enums;

namespace WirelessMic.Application.DTO;

/// <summary>
/// Bağlantı durumu değişikliği bilgisi.
/// </summary>
public sealed class ConnectionStateChangedEventArgs : EventArgs
{
    public required ConnectionState State { get; init; }

    public string? RemoteHost { get; init; }

    public string? Message { get; init; }
}

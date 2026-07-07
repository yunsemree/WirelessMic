namespace WirelessMic.Domain.Enums;

/// <summary>
/// Bağlantı durumunu tanımlar.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}

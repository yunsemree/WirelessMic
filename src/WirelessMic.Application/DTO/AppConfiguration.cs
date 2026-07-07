namespace WirelessMic.Application.DTO;

using WirelessMic.Shared.Constants;

/// <summary>
/// Uygulama yapılandırma ayarları.
/// </summary>
public sealed class AppConfiguration
{
    public DiscoveryConfiguration Discovery { get; set; } = new();

    public AudioConfiguration Audio { get; set; } = new();

    public ConnectionConfiguration Connection { get; set; } = new();
}

/// <summary>
/// Keşif yapılandırması.
/// </summary>
public sealed class DiscoveryConfiguration
{
    public int Port { get; set; } = 9876;

    public int TimeoutMs { get; set; } = 3000;

    public int RetryCount { get; set; } = 3;
}

/// <summary>
/// Ses yapılandırması.
/// </summary>
public sealed class AudioConfiguration
{
    public int SampleRate { get; set; } = 48000;

    public int BitsPerSample { get; set; } = 16;

    public int Channels { get; set; } = 1;

    public int FrameDurationMs { get; set; } = 20;

    public int BufferSizeMs { get; set; } = 60;

    public int StreamPort { get; set; } = AudioProtocol.StreamPort;
}

/// <summary>
/// Bağlantı yapılandırması.
/// </summary>
public sealed class ConnectionConfiguration
{
    public int Port { get; set; } = ConnectionProtocol.DefaultPort;

    public int ConnectTimeoutMs { get; set; } = ConnectionProtocol.DefaultConnectTimeoutMs;

    public int HeartbeatIntervalMs { get; set; } = 5000;

    public int HeartbeatTimeoutMs { get; set; } = ConnectionProtocol.DefaultHeartbeatTimeoutMs;

    public int ReconnectDelayMs { get; set; } = 2000;

    public int MaxReconnectAttempts { get; set; } = 10;
}

namespace WirelessMic.Shared.Constants;

/// <summary>
/// UDP bağlantı kontrol protokolü sabitleri.
/// </summary>
public static class ConnectionProtocol
{
    public const int DefaultPort = 9877;
    public const string Connect = "CONNECT";
    public const string ConnectOk = "CONNECT_OK";
    public const string ConnectFail = "CONNECT_FAIL";
    public const string Disconnect = "DISCONNECT";
    public const string Ping = "PING";
    public const string Pong = "PONG";
    public const string Heartbeat = "HEARTBEAT";
    public const string HeartbeatAck = "HEARTBEAT_ACK";
    public const int DefaultConnectTimeoutMs = 5000;
    public const int DefaultHeartbeatTimeoutMs = 3000;
}

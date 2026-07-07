using System.Text;
using WirelessMic.Shared.Constants;

namespace WirelessMic.Infrastructure.Networking;

/// <summary>
/// UDP bağlantı kontrol mesajlarını oluşturur ve ayrıştırır.
/// </summary>
public static class ConnectionMessageFormatter
{
    /// <summary>Bağlantı isteği oluşturur.</summary>
    public static byte[] CreateConnect(string clientName) =>
        Encoding.UTF8.GetBytes($"{ConnectionProtocol.Connect}\n{clientName}");

    /// <summary>Başarılı bağlantı yanıtı oluşturur.</summary>
    public static byte[] CreateConnectOk(string sessionId) =>
        Encoding.UTF8.GetBytes($"{ConnectionProtocol.ConnectOk}\n{sessionId}");

    /// <summary>Başarısız bağlantı yanıtı oluşturur.</summary>
    public static byte[] CreateConnectFail(string reason) =>
        Encoding.UTF8.GetBytes($"{ConnectionProtocol.ConnectFail}\n{reason}");

    /// <summary>Ping mesajı oluşturur.</summary>
    public static byte[] CreatePing(long timestampTicks) =>
        Encoding.UTF8.GetBytes($"{ConnectionProtocol.Ping}\n{timestampTicks}");

    /// <summary>Pong mesajı oluşturur.</summary>
    public static byte[] CreatePong(long timestampTicks) =>
        Encoding.UTF8.GetBytes($"{ConnectionProtocol.Pong}\n{timestampTicks}");

    /// <summary>Heartbeat mesajı oluşturur.</summary>
    public static byte[] CreateHeartbeat(string sessionId) =>
        Encoding.UTF8.GetBytes($"{ConnectionProtocol.Heartbeat}\n{sessionId}");

    /// <summary>Heartbeat onayı oluşturur.</summary>
    public static byte[] CreateHeartbeatAck(string sessionId) =>
        Encoding.UTF8.GetBytes($"{ConnectionProtocol.HeartbeatAck}\n{sessionId}");

    /// <summary>Bağlantı kesme mesajı oluşturur.</summary>
    public static byte[] CreateDisconnect(string sessionId) =>
        Encoding.UTF8.GetBytes($"{ConnectionProtocol.Disconnect}\n{sessionId}");

    /// <summary>Mesaj türünü ve satırlarını ayrıştırır.</summary>
    public static bool TryParse(ReadOnlySpan<byte> data, out string messageType, out string[] lines)
    {
        messageType = string.Empty;
        lines = [];

        var text = Encoding.UTF8.GetString(data).Trim();
        var parts = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            return false;

        messageType = parts[0];
        lines = parts;
        return true;
    }
}

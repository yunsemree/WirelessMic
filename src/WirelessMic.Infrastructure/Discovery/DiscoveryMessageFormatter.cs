using System.Text;
using WirelessMic.Application.DTO;
using WirelessMic.Shared.Constants;

namespace WirelessMic.Infrastructure.Discovery;

/// <summary>
/// UDP keşif protokolü mesajlarını oluşturur ve ayrıştırır.
/// </summary>
public static class DiscoveryMessageFormatter
{
    /// <summary>Keşif isteği oluşturur.</summary>
    public static byte[] CreateRequest() =>
        Encoding.UTF8.GetBytes(DiscoveryProtocol.DiscoverRequest);

    /// <summary>Sunucu yanıtı oluşturur.</summary>
    public static byte[] CreateResponse(string computerName, string ipAddress, string version)
    {
        var message = string.Join('\n',
            DiscoveryProtocol.DiscoverResponsePrefix,
            computerName,
            ipAddress,
            version);

        return Encoding.UTF8.GetBytes(message);
    }

    /// <summary>Gelen verinin keşif isteği olup olmadığını kontrol eder.</summary>
    public static bool IsDiscoverRequest(ReadOnlySpan<byte> data)
    {
        var text = Encoding.UTF8.GetString(data).Trim();
        return text.Equals(DiscoveryProtocol.DiscoverRequest, StringComparison.Ordinal);
    }

    /// <summary>Sunucu yanıtını ayrıştırır.</summary>
    public static bool TryParseResponse(ReadOnlySpan<byte> data, out DiscoveredServerDto? server)
    {
        server = null;
        var text = Encoding.UTF8.GetString(data).Trim();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length < 4)
            return false;

        if (!lines[0].Equals(DiscoveryProtocol.DiscoverResponsePrefix, StringComparison.Ordinal))
            return false;

        server = new DiscoveredServerDto
        {
            ComputerName = lines[1],
            IpAddress = lines[2],
            Version = lines[3]
        };

        return true;
    }
}

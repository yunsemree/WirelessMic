using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WirelessMic.Infrastructure.Networking;

/// <summary>
/// Yerel ağ adresi yardımcıları.
/// </summary>
public static class NetworkAddressHelper
{
    /// <summary>Yerel IPv4 adresini döndürür.</summary>
    public static string GetLocalIpAddress()
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
                continue;

            if (networkInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback
                or NetworkInterfaceType.Tunnel)
                continue;

            foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily == AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(address.Address))
                {
                    return address.Address.ToString();
                }
            }
        }

        return "127.0.0.1";
    }

    /// <summary>Uzak adresle aynı alt ağdaki yerel IPv4 adresini döndürür.</summary>
    public static string GetLocalIpAddressForRemote(IPAddress remoteAddress)
    {
        if (remoteAddress.AddressFamily != AddressFamily.InterNetwork)
            return GetLocalIpAddress();

        var remoteBytes = remoteAddress.GetAddressBytes();

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                if (IsSameSubnet(address.Address, remoteAddress, address.IPv4Mask))
                    return address.Address.ToString();
            }
        }

        return GetLocalIpAddress();
    }

    private static bool IsSameSubnet(IPAddress local, IPAddress remote, IPAddress? mask)
    {
        if (mask is null)
            return false;

        var localBytes = local.GetAddressBytes();
        var remoteBytes = remote.GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();

        for (var i = 0; i < localBytes.Length; i++)
        {
            if ((localBytes[i] & maskBytes[i]) != (remoteBytes[i] & maskBytes[i]))
                return false;
        }

        return true;
    }
}

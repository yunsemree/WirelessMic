namespace WirelessMic.Shared.Constants;

/// <summary>
/// UDP ağ keşif protokolü sabitleri.
/// </summary>
public static class DiscoveryProtocol
{
    public const int DiscoveryPort = 9876;
    public const string DiscoverRequest = "DISCOVER_MIC_SERVER";
    public const string DiscoverResponsePrefix = "MIC_SERVER";
    public const int DefaultTimeoutMs = 3000;
    public const int DefaultRetryCount = 3;
}

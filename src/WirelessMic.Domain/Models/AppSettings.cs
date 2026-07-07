namespace WirelessMic.Domain.Models;

/// <summary>
/// Uygulama ayarları modeli.
/// </summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = "System";

    public int AudioQualityBitrateKbps { get; set; } = 48;

    public int BufferSizeMs { get; set; } = 60;

    public bool AutoReconnect { get; set; } = true;

    public bool AutoDiscovery { get; set; } = true;

    public bool UseCompression { get; set; } = false;
}

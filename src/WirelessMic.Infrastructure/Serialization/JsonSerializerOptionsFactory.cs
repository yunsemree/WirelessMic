using System.Text.Json;

namespace WirelessMic.Infrastructure.Serialization;

/// <summary>
/// Paylaşılan JSON serileştirme ayarları.
/// </summary>
public static class JsonSerializerOptionsFactory
{
    /// <summary>Varsayılan JSON serileştirme seçenekleri.</summary>
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}

namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Uygulama dosya yollarını sağlar.
/// </summary>
public interface IAppPaths
{
    /// <summary>Uygulama veri dizini.</summary>
    string AppDataDirectory { get; }

    /// <summary>Ayarlar dosyasının tam yolu.</summary>
    string SettingsFilePath { get; }

    /// <summary>Log dosyalarının dizini.</summary>
    string LogsDirectory { get; }
}

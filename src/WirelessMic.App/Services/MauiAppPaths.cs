using WirelessMic.Application.Interfaces;
using WirelessMic.Shared.Constants;

namespace WirelessMic.App.Services;

/// <summary>
/// MAUI dosya yolu sağlayıcısı.
/// </summary>
public sealed class MauiAppPaths : IAppPaths
{
    public string AppDataDirectory => FileSystem.AppDataDirectory;

    public string SettingsFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, AppConstants.SettingsFileName);

    public string LogsDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, "logs");
}

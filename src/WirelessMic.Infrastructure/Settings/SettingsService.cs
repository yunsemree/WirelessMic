using System.Text.Json;
using Microsoft.Extensions.Logging;
using WirelessMic.Application.Interfaces;
using WirelessMic.Domain.Models;
using WirelessMic.Infrastructure.Serialization;

namespace WirelessMic.Infrastructure.Settings;

/// <summary>
/// JSON dosyası tabanlı ayar yönetimi.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly IAppPaths _appPaths;
    private readonly ILogger<SettingsService> _logger;
    private AppSettings _settings = new();

    public SettingsService(IAppPaths appPaths, ILogger<SettingsService> logger)
    {
        _appPaths = appPaths;
        _logger = logger;
    }

    /// <inheritdoc />
    public AppSettings GetSettings() => _settings;

    /// <inheritdoc />
    public async Task LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        var path = _appPaths.SettingsFilePath;

        if (!File.Exists(path))
        {
            _settings = new AppSettings();
            _logger.LogInformation("Ayar dosyası bulunamadı, varsayılan ayarlar kullanılıyor: {Path}", path);
            return;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                JsonSerializerOptionsFactory.Default,
                cancellationToken);

            _settings = loaded ?? new AppSettings();
            _logger.LogInformation("Ayarlar yüklendi: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ayarlar yüklenirken hata oluştu: {Path}", path);
            _settings = new AppSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var path = _appPaths.SettingsFilePath;
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            settings,
            JsonSerializerOptionsFactory.Default,
            cancellationToken);

        _settings = settings;
        _logger.LogInformation("Ayarlar kaydedildi: {Path}", path);
    }
}

using WirelessMic.Domain.Models;

namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Uygulama ayarlarını yönetir.
/// </summary>
public interface ISettingsService
{
    /// <summary>Mevcut ayarları döndürür.</summary>
    AppSettings GetSettings();

    /// <summary>Ayarları kaydeder.</summary>
    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Ayarları yeniden yükler.</summary>
    Task LoadSettingsAsync(CancellationToken cancellationToken = default);
}

namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Uygulama içi sayfa navigasyonu sağlar.
/// </summary>
public interface INavigationService
{
    /// <summary>Belirtilen rotaya gider.</summary>
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null, bool animate = true);

    /// <summary>Bir önceki sayfaya döner.</summary>
    Task GoBackAsync(bool animate = true);
}

using WirelessMic.Domain.Enums;

namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Cihaz rolü ve platform tespiti sağlar.
/// </summary>
public interface IDeviceRoleService
{
    /// <summary>Mevcut cihazın rolünü döndürür (Desktop veya Phone).</summary>
    DeviceRole GetDeviceRole();

    /// <summary>Mevcut platformu döndürür.</summary>
    PlatformType GetPlatformType();

    /// <summary>Cihazın masaüstü olup olmadığını belirtir.</summary>
    bool IsDesktop { get; }

    /// <summary>Cihazın telefon olup olmadığını belirtir.</summary>
    bool IsPhone { get; }
}

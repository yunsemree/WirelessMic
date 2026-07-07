using WirelessMic.Domain.Enums;

namespace WirelessMic.Infrastructure.Platforms;

/// <summary>
/// Çalışma zamanı platform tespiti ile cihaz rolünü belirler.
/// </summary>
public sealed class DeviceRoleService : Application.Interfaces.IDeviceRoleService
{
    private readonly DeviceRole _deviceRole;
    private readonly PlatformType _platformType;

    public DeviceRoleService()
    {
        _platformType = DetectPlatform();
        _deviceRole = _platformType == PlatformType.Windows
            ? DeviceRole.Desktop
            : DeviceRole.Phone;
    }

    /// <inheritdoc />
    public DeviceRole GetDeviceRole() => _deviceRole;

    /// <inheritdoc />
    public PlatformType GetPlatformType() => _platformType;

    /// <inheritdoc />
    public bool IsDesktop => _deviceRole == DeviceRole.Desktop;

    /// <inheritdoc />
    public bool IsPhone => _deviceRole == DeviceRole.Phone;

    private static PlatformType DetectPlatform()
    {
        if (OperatingSystem.IsWindows())
            return PlatformType.Windows;

        if (OperatingSystem.IsAndroid())
            return PlatformType.Android;

        if (OperatingSystem.IsIOS())
            return PlatformType.iOS;

        return PlatformType.Unknown;
    }
}

using WirelessMic.Domain.Enums;
using WirelessMic.Infrastructure.Platforms;

namespace WirelessMic.Tests;

public class DeviceRoleServiceTests
{
    [Fact]
    public void GetDeviceRole_OnWindows_ReturnsDesktop()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var service = new DeviceRoleService();

        Assert.Equal(DeviceRole.Desktop, service.GetDeviceRole());
        Assert.Equal(PlatformType.Windows, service.GetPlatformType());
        Assert.True(service.IsDesktop);
        Assert.False(service.IsPhone);
    }

    [Fact]
    public void GetPlatformType_ReturnsKnownPlatform()
    {
        var service = new DeviceRoleService();
        var platform = service.GetPlatformType();

        Assert.NotEqual(PlatformType.Unknown, platform);
    }
}

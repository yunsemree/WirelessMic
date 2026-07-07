using Microsoft.Extensions.DependencyInjection;
using WirelessMic.Application.Interfaces;
using WirelessMic.Infrastructure.Audio;
using WirelessMic.Infrastructure.Discovery;
using WirelessMic.Infrastructure.Networking;
using WirelessMic.Infrastructure.Platforms;
using WirelessMic.Infrastructure.Settings;

namespace WirelessMic.Infrastructure.DependencyInjection;

/// <summary>
/// Infrastructure katmanı DI kayıtları.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Infrastructure katmanı servislerini kaydeder.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceRoleService, DeviceRoleService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INetworkDiscovery, UdpNetworkDiscovery>();
        services.AddSingleton<ILatencyMonitor, LatencyMonitor>();
        services.AddSingleton<IConnectionManager, UdpConnectionManager>();
        services.AddSingleton<IAudioStreamService, UdpAudioStreamService>();

        return services;
    }
}

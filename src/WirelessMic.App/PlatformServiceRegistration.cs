using Microsoft.Extensions.DependencyInjection;
using WirelessMic.App.Services;
using WirelessMic.Application.Interfaces;
using WirelessMic.Infrastructure.Platforms;

namespace WirelessMic.App;

/// <summary>
/// Platforma özel servis kayıtları.
/// </summary>
public static class PlatformServiceRegistration
{
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
#if ANDROID
        services.AddSingleton<IPermissionService, MauiPermissionService>();
        services.AddSingleton<IMicrophoneService, Platforms.Android.Services.AndroidMicrophoneService>();
        services.AddSingleton<IVirtualMicrophoneOutput, NoOpVirtualMicrophoneOutput>();
        services.AddSingleton<IAudioPlayer, NoOpAudioPlayer>();
#elif WINDOWS
        services.AddSingleton<IPermissionService, DesktopPermissionService>();
        services.AddSingleton<IMicrophoneService, NoOpMicrophoneService>();
        services.AddSingleton<IVirtualMicrophoneOutput, Platforms.Windows.Services.WindowsVirtualMicrophoneOutput>();
        services.AddSingleton<IAudioPlayer, Platforms.Windows.Services.WindowsAudioMonitor>();
#elif IOS
        services.AddSingleton<IPermissionService, MauiPermissionService>();
        services.AddSingleton<IMicrophoneService, NoOpMicrophoneService>();
        services.AddSingleton<IVirtualMicrophoneOutput, NoOpVirtualMicrophoneOutput>();
        services.AddSingleton<IAudioPlayer, NoOpAudioPlayer>();
#else
        services.AddSingleton<IPermissionService, DesktopPermissionService>();
        services.AddSingleton<IMicrophoneService, NoOpMicrophoneService>();
        services.AddSingleton<IVirtualMicrophoneOutput, NoOpVirtualMicrophoneOutput>();
        services.AddSingleton<IAudioPlayer, NoOpAudioPlayer>();
#endif

        return services;
    }
}

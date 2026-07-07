using WirelessMic.Application.Interfaces;

namespace WirelessMic.App.Services;

/// <summary>
/// MAUI izin servisi (Android / iOS).
/// </summary>
public sealed class MauiPermissionService : IPermissionService
{
    public async Task<bool> RequestMicrophonePermissionAsync(CancellationToken cancellationToken = default)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status == PermissionStatus.Granted)
            return true;

        status = await Permissions.RequestAsync<Permissions.Microphone>();
        return status == PermissionStatus.Granted;
    }

    public async Task<bool> HasMicrophonePermissionAsync(CancellationToken cancellationToken = default)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        return status == PermissionStatus.Granted;
    }
}

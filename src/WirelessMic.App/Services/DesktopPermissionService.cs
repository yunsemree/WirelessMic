using WirelessMic.Application.Interfaces;

namespace WirelessMic.App.Services;

/// <summary>
/// Masaüstü platformu için izin servisi.
/// </summary>
public sealed class DesktopPermissionService : IPermissionService
{
    public Task<bool> RequestMicrophonePermissionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<bool> HasMicrophonePermissionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

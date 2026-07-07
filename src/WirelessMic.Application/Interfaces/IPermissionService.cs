namespace WirelessMic.Application.Interfaces;

/// <summary>
/// Platform izinlerini yönetir (mikrofon vb.).
/// </summary>
public interface IPermissionService
{
    /// <summary>Mikrofon iznini ister.</summary>
    Task<bool> RequestMicrophonePermissionAsync(CancellationToken cancellationToken = default);

    /// <summary>Mikrofon izninin verilip verilmediğini kontrol eder.</summary>
    Task<bool> HasMicrophonePermissionAsync(CancellationToken cancellationToken = default);
}

using WirelessMic.Application.Interfaces;

namespace WirelessMic.Infrastructure.Platforms;

/// <summary>
/// Test dinlemesi desteklenmeyen platformlar için no-op implementasyon.
/// </summary>
public sealed class NoOpAudioPlayer : IAudioPlayer
{
    public bool IsMonitoringEnabled => false;

    public Task SetMonitoringEnabledAsync(bool enabled, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void EnqueueFrame(ReadOnlySpan<byte> pcmFrame)
    {
    }
}

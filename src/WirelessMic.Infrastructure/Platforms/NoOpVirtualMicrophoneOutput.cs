using WirelessMic.Application.Interfaces;

namespace WirelessMic.Infrastructure.Platforms;

/// <summary>
/// Sanal mikrofon desteklenmeyen platformlar için no-op implementasyon.
/// </summary>
public sealed class NoOpVirtualMicrophoneOutput : IVirtualMicrophoneOutput
{
    public bool IsActive => false;

    public bool IsVirtualCableReady => false;

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void EnqueueFrame(ReadOnlySpan<byte> pcmFrame)
    {
    }
}

using WirelessMic.Application.Interfaces;

namespace WirelessMic.Infrastructure.Platforms;

/// <summary>
/// Mikrofon desteklenmeyen platformlar için no-op implementasyon.
/// </summary>
public sealed class NoOpMicrophoneService : IMicrophoneService
{
    /// <inheritdoc />
    public event EventHandler<ReadOnlyMemory<byte>>? FrameCaptured;

    /// <inheritdoc />
    public bool IsCapturing => false;

    /// <inheritdoc />
    public Task StartCaptureAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task StopCaptureAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

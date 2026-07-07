using Microsoft.Extensions.Logging;
using NAudio.Wave;
using WirelessMic.Application.Interfaces;
using WirelessMic.Shared.Constants;

namespace WirelessMic.App.Platforms.Windows.Services;

/// <summary>
/// Hoparlör üzerinden mikrofon testi dinlemesi.
/// </summary>
public sealed class WindowsAudioMonitor : IAudioPlayer, IDisposable
{
    private readonly ILogger<WindowsAudioMonitor> _logger;
    private readonly object _sync = new();
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _buffer;
    private bool _isMonitoringEnabled;

    public WindowsAudioMonitor(ILogger<WindowsAudioMonitor> logger)
    {
        _logger = logger;
    }

    public bool IsMonitoringEnabled => _isMonitoringEnabled;

    public Task SetMonitoringEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_isMonitoringEnabled == enabled)
                return Task.CompletedTask;

            if (enabled)
                StartMonitorInternal();
            else
                StopMonitorInternal();

            _isMonitoringEnabled = enabled;
        }

        _logger.LogInformation("Mikrofon testi dinlemesi: {State}", enabled ? "açık" : "kapalı");
        return Task.CompletedTask;
    }

    public void EnqueueFrame(ReadOnlySpan<byte> pcmFrame)
    {
        if (pcmFrame.IsEmpty)
            return;

        lock (_sync)
        {
            if (!_isMonitoringEnabled || _buffer is null)
                return;

            _buffer.AddSamples(pcmFrame.ToArray(), 0, pcmFrame.Length);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            StopMonitorInternal();
            _isMonitoringEnabled = false;
        }
    }

    private void StartMonitorInternal()
    {
        if (_waveOut is not null)
            return;

        var format = new WaveFormat(
            AudioConstants.SampleRate,
            AudioConstants.BitsPerSample,
            AudioConstants.Channels);

        _buffer = new BufferedWaveProvider(format)
        {
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromSeconds(1)
        };

        _waveOut = new WaveOutEvent { DeviceNumber = -1 };
        _waveOut.Init(_buffer);
        _waveOut.Play();
    }

    private void StopMonitorInternal()
    {
        if (_waveOut is not null)
        {
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }

        _buffer = null;
    }
}

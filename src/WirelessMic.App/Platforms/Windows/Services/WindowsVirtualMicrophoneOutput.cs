using Microsoft.Extensions.Logging;
using NAudio.Wave;
using WirelessMic.Application.Interfaces;
using WirelessMic.Shared.Constants;

namespace WirelessMic.App.Platforms.Windows.Services;

/// <summary>
/// VB-Cable üzerinden sanal mikrofon çıkışı.
/// </summary>
public sealed class WindowsVirtualMicrophoneOutput : IVirtualMicrophoneOutput, IDisposable
{
    private readonly ILogger<WindowsVirtualMicrophoneOutput> _logger;
    private readonly object _sync = new();
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _buffer;
    private int _deviceNumber = -1;
    private bool _isActive;
    private bool _isVirtualCableReady;

    public WindowsVirtualMicrophoneOutput(ILogger<WindowsVirtualMicrophoneOutput> logger)
    {
        _logger = logger;
    }

    public bool IsActive => _isActive;

    public bool IsVirtualCableReady => _isVirtualCableReady;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_isActive)
                return Task.CompletedTask;

            _deviceNumber = FindVirtualCableDeviceNumber();
            _isVirtualCableReady = _deviceNumber >= 0;

            if (!_isVirtualCableReady)
            {
                _logger.LogWarning("VB-Cable bulunamadı. Sanal mikrofon çıkışı başlatılamadı.");
                return Task.CompletedTask;
            }

            var format = new WaveFormat(
                AudioConstants.SampleRate,
                AudioConstants.BitsPerSample,
                AudioConstants.Channels);

            _buffer = new BufferedWaveProvider(format)
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromSeconds(2)
            };

            _waveOut = new WaveOutEvent { DeviceNumber = _deviceNumber };
            _waveOut.Init(_buffer);
            _waveOut.Play();
            _isActive = true;

            var deviceName = WaveOut.GetCapabilities(_deviceNumber).ProductName.Trim();
            _logger.LogInformation("Sanal mikrofon çıkışı başlatıldı: {Device}", deviceName);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            StopInternal();
        }

        return Task.CompletedTask;
    }

    public void EnqueueFrame(ReadOnlySpan<byte> pcmFrame)
    {
        if (pcmFrame.IsEmpty)
            return;

        lock (_sync)
        {
            if (!_isActive || _buffer is null)
                return;

            _buffer.AddSamples(pcmFrame.ToArray(), 0, pcmFrame.Length);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            StopInternal();
        }
    }

    private void StopInternal()
    {
        if (_waveOut is not null)
        {
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }

        _buffer = null;
        _isActive = false;
    }

    private static int FindVirtualCableDeviceNumber()
    {
        for (var i = 0; i < WaveOut.DeviceCount; i++)
        {
            var name = WaveOut.GetCapabilities(i).ProductName.Trim();
            if (IsVirtualCableDevice(name))
                return i;
        }

        return -1;
    }

    private static bool IsVirtualCableDevice(string deviceName) =>
        deviceName.Contains(AudioOutputConstants.VbCableInputKeyword, StringComparison.OrdinalIgnoreCase)
        || (deviceName.Contains(AudioOutputConstants.VbCableBrandKeyword, StringComparison.OrdinalIgnoreCase)
            && deviceName.Contains("CABLE", StringComparison.OrdinalIgnoreCase)
            && deviceName.Contains("Input", StringComparison.OrdinalIgnoreCase));
}

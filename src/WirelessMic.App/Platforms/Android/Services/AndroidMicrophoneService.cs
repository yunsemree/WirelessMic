using Android.Content;
using Android.Media;
using Microsoft.Extensions.Logging;
using WirelessMic.Application.Interfaces;
using WirelessMic.Shared.Constants;

namespace WirelessMic.App.Droid.Services;

/// <summary>
/// Android mikrofon yakalama servisi.
/// </summary>
public sealed class AndroidMicrophoneService : IMicrophoneService
{
    private readonly ILogger<AndroidMicrophoneService> _logger;
    private AudioRecord? _audioRecord;
    private CancellationTokenSource? _cts;
    private Task? _captureTask;

    public AndroidMicrophoneService(ILogger<AndroidMicrophoneService> logger)
    {
        _logger = logger;
    }

    public event EventHandler<ReadOnlyMemory<byte>>? FrameCaptured;

    public bool IsCapturing => _captureTask is { IsCompleted: false };

    public Task StartCaptureAsync(CancellationToken cancellationToken = default)
    {
        if (IsCapturing)
            return Task.CompletedTask;

        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(MicrophoneForegroundService));
        context.StartForegroundService(intent);

        var channelConfig = ChannelIn.Mono;
        var audioFormat = Encoding.Pcm16bit;
        var minBuffer = AudioRecord.GetMinBufferSize(AudioConstants.SampleRate, channelConfig, audioFormat);
        var bufferSize = Math.Max(minBuffer, AudioConstants.FrameSizeBytes * 4);

        _audioRecord = new AudioRecord(
            AudioSource.Mic,
            AudioConstants.SampleRate,
            channelConfig,
            audioFormat,
            bufferSize);

        if (_audioRecord.State != State.Initialized)
        {
            _logger.LogError("AudioRecord başlatılamadı.");
            _audioRecord.Dispose();
            _audioRecord = null;
            return Task.CompletedTask;
        }

        _audioRecord.StartRecording();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // AudioRecord.Read bloke edicidir ve veri geldiği sürece döngü await'e ulaşmaz;
        // UI thread'ini dondurup ANR'a yol açmamak için arka plan thread'inde çalıştırılır.
        _captureTask = Task.Run(() => CaptureLoopAsync(_cts.Token), _cts.Token);

        _logger.LogInformation("Android mikrofon kaydı başlatıldı.");
        return Task.CompletedTask;
    }

    public async Task StopCaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
            await _cts.CancelAsync();

        if (_captureTask is not null)
        {
            try
            {
                await _captureTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        if (_audioRecord is not null)
        {
            _audioRecord.Stop();
            _audioRecord.Release();
            _audioRecord.Dispose();
            _audioRecord = null;
        }

        _cts?.Dispose();
        _cts = null;
        _captureTask = null;

        var context = global::Android.App.Application.Context;
        context.StopService(new Intent(context, typeof(MicrophoneForegroundService)));

        _logger.LogInformation("Android mikrofon kaydı durduruldu.");
    }

    private async Task CaptureLoopAsync(CancellationToken cancellationToken)
    {
        var frameBuffer = new byte[AudioConstants.FrameSizeBytes];

        while (!cancellationToken.IsCancellationRequested && _audioRecord is not null)
        {
            var read = _audioRecord.Read(frameBuffer, 0, frameBuffer.Length);
            if (read > 0)
            {
                FrameCaptured?.Invoke(this, frameBuffer.AsMemory(0, read));
            }
            else
            {
                await Task.Delay(5, cancellationToken);
            }
        }
    }
}

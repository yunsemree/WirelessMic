using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WirelessMic.Application.DTO;
using WirelessMic.Application.Interfaces;
using WirelessMic.Infrastructure.Audio;
using WirelessMic.Shared.Constants;

namespace WirelessMic.Infrastructure.Audio;

/// <summary>
/// UDP üzerinden PCM ses akışı servisi.
/// </summary>
public sealed class UdpAudioStreamService : IAudioStreamService
{
    private readonly AppConfiguration _configuration;
    private readonly IDeviceRoleService _deviceRoleService;
    private readonly IConnectionManager _connectionManager;
    private readonly IVirtualMicrophoneOutput _virtualMicrophone;
    private readonly IAudioPlayer _audioMonitor;
    private readonly ILogger<UdpAudioStreamService> _logger;

    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private int _sequence;
    private IPEndPoint? _sendEndpoint;

    public UdpAudioStreamService(
        IOptions<AppConfiguration> configuration,
        IDeviceRoleService deviceRoleService,
        IConnectionManager connectionManager,
        IVirtualMicrophoneOutput virtualMicrophone,
        IAudioPlayer audioMonitor,
        ILogger<UdpAudioStreamService> logger)
    {
        _configuration = configuration.Value;
        _deviceRoleService = deviceRoleService;
        _connectionManager = connectionManager;
        _virtualMicrophone = virtualMicrophone;
        _audioMonitor = audioMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsStreaming => _receiveTask is { IsCompleted: false } || _udpClient is not null;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_udpClient is not null)
            return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _udpClient = new UdpClient();
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        if (_deviceRoleService.IsDesktop)
        {
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _configuration.Audio.StreamPort));
            _receiveTask = ReceiveLoopAsync(_cts.Token);
            _logger.LogInformation("Ses alıcı başlatıldı. Port: {Port}", _configuration.Audio.StreamPort);
        }
        else if (!string.IsNullOrEmpty(_connectionManager.RemoteHost))
        {
            _sendEndpoint = new IPEndPoint(
                IPAddress.Parse(_connectionManager.RemoteHost),
                _configuration.Audio.StreamPort);

            _receiveTask = ReceiveLoopAsync(_cts.Token);
            _logger.LogInformation("Ses gönderici hazır. Hedef: {Endpoint}", _sendEndpoint);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
            await _cts.CancelAsync();

        if (_receiveTask is not null)
        {
            try
            {
                await _receiveTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Beklenen durum
            }
        }

        _udpClient?.Dispose();
        _udpClient = null;
        _cts?.Dispose();
        _cts = null;
        _receiveTask = null;
        _sendEndpoint = null;
        _sequence = 0;

        _logger.LogInformation("Ses akışı durduruldu.");
    }

    /// <inheritdoc />
    public void SubmitCapturedFrame(ReadOnlySpan<byte> pcmFrame)
    {
        if (_udpClient is null || _sendEndpoint is null || pcmFrame.IsEmpty)
            return;

        var sequence = Interlocked.Increment(ref _sequence);
        var packet = AudioPacketSerializer.Serialize(sequence, DateTime.UtcNow.Ticks, pcmFrame);

        try
        {
            _udpClient.Send(packet, _sendEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ses paketi gönderilemedi");
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_udpClient is null)
            return;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken);

                if (!AudioPacketSerializer.TryDeserialize(
                        result.Buffer,
                        out _,
                        out _,
                        out var payload))
                {
                    continue;
                }

                if (_deviceRoleService.IsDesktop)
                {
                    _virtualMicrophone.EnqueueFrame(payload);

                    if (_audioMonitor.IsMonitoringEnabled)
                    {
                        _audioMonitor.EnqueueFrame(payload);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Ses alımında soket hatası");
                break;
            }
        }
    }
}

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WirelessMic.Application.DTO;
using WirelessMic.Application.Interfaces;
using WirelessMic.Infrastructure.Networking;
using WirelessMic.Shared.Constants;

namespace WirelessMic.Infrastructure.Discovery;

/// <summary>
/// UDP broadcast tabanlı ağ keşif servisi.
/// </summary>
public sealed class UdpNetworkDiscovery : INetworkDiscovery
{
    private readonly AppConfiguration _configuration;
    private readonly ILogger<UdpNetworkDiscovery> _logger;
    private readonly object _listenerLock = new();
    private CancellationTokenSource? _listenerCts;
    private Task? _listenerTask;

    public UdpNetworkDiscovery(
        IOptions<AppConfiguration> configuration,
        ILogger<UdpNetworkDiscovery> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsListening => _listenerTask is { IsCompleted: false };

    /// <inheritdoc />
    public async Task<IReadOnlyList<DiscoveredServerDto>> DiscoverServersAsync(
        CancellationToken cancellationToken = default)
    {
        var discovery = _configuration.Discovery;
        var servers = new ConcurrentDictionary<string, DiscoveredServerDto>();

        using var client = new UdpClient();
        client.EnableBroadcast = true;
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, discovery.Port);
        var requestPayload = DiscoveryMessageFormatter.CreateRequest();

        _logger.LogInformation(
            "Keşif başlatıldı. Port: {Port}, Timeout: {TimeoutMs}ms, Retry: {RetryCount}",
            discovery.Port,
            discovery.TimeoutMs,
            discovery.RetryCount);

        for (var attempt = 1; attempt <= discovery.RetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Keşif denemesi {Attempt}/{RetryCount}", attempt, discovery.RetryCount);

            await client.SendAsync(requestPayload, broadcastEndpoint, cancellationToken);

            var attemptDeadline = DateTime.UtcNow.AddMilliseconds(discovery.TimeoutMs);

            while (DateTime.UtcNow < attemptDeadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var remainingMs = (int)Math.Max(1, (attemptDeadline - DateTime.UtcNow).TotalMilliseconds);

                using var receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                receiveCts.CancelAfter(remainingMs);

                try
                {
                    var result = await client.ReceiveAsync(receiveCts.Token);

                    if (!DiscoveryMessageFormatter.TryParseResponse(result.Buffer, out var server)
                        || server is null)
                    {
                        continue;
                    }

                    servers.TryAdd(server.IpAddress, server);

                    _logger.LogInformation(
                        "Sunucu keşfedildi: {ComputerName} ({IpAddress}) v{Version}",
                        server.ComputerName,
                        server.IpAddress,
                        server.Version);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    _logger.LogDebug(ex, "Keşif sırasında soket hatası");
                    break;
                }
            }
        }

        var results = servers.Values.OrderBy(s => s.ComputerName).ToList();

        _logger.LogInformation("Keşif tamamlandı. {Count} sunucu bulundu.", results.Count);

        return results;
    }

    /// <inheritdoc />
    public Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        lock (_listenerLock)
        {
            if (_listenerTask is { IsCompleted: false })
                return Task.CompletedTask;

            _listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listenerTask = ListenLoopAsync(_listenerCts.Token);
        }

        _logger.LogInformation("Keşif dinleyicisi başlatıldı. Port: {Port}", _configuration.Discovery.Port);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopListeningAsync(CancellationToken cancellationToken = default)
    {
        Task? listenerTask;

        lock (_listenerLock)
        {
            if (_listenerCts is null)
                return;

            _listenerCts.Cancel();
            listenerTask = _listenerTask;
        }

        if (listenerTask is not null)
        {
            try
            {
                await listenerTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Beklenen durum
            }
        }

        lock (_listenerLock)
        {
            _listenerCts?.Dispose();
            _listenerCts = null;
            _listenerTask = null;
        }

        _logger.LogInformation("Keşif dinleyicisi durduruldu.");
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        using var client = new UdpClient(_configuration.Discovery.Port);
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        var computerName = Environment.MachineName;

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;

            try
            {
                result = await client.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Keşif dinleyicisinde soket hatası");
                break;
            }

            if (!DiscoveryMessageFormatter.IsDiscoverRequest(result.Buffer))
                continue;

            var localIp = NetworkAddressHelper.GetLocalIpAddressForRemote(result.RemoteEndPoint.Address);
            var response = DiscoveryMessageFormatter.CreateResponse(
                computerName,
                localIp,
                AppConstants.AppVersion);

            try
            {
                await client.SendAsync(response, result.RemoteEndPoint, cancellationToken);

                _logger.LogDebug(
                    "Keşif yanıtı gönderildi: {ComputerName} ({IpAddress}) -> {Remote}",
                    computerName,
                    localIp,
                    result.RemoteEndPoint);
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                _logger.LogWarning(ex, "Keşif yanıtı gönderilemedi");
            }
        }
    }
}

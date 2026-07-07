using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WirelessMic.Application.DTO;
using WirelessMic.Application.Interfaces;
using WirelessMic.Domain.Enums;
using WirelessMic.Shared.Constants;

namespace WirelessMic.Infrastructure.Networking;

/// <summary>
/// UDP tabanlı bağlantı yöneticisi (heartbeat, ping, yeniden bağlanma).
/// </summary>
public sealed class UdpConnectionManager : IConnectionManager
{
    private readonly AppConfiguration _configuration;
    private readonly IDeviceRoleService _deviceRoleService;
    private readonly ISettingsService _settingsService;
    private readonly ILatencyMonitor _latencyMonitor;
    private readonly ILogger<UdpConnectionManager> _logger;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _clientIoLock = new(1, 1);
    private readonly ConcurrentDictionary<string, ClientSession> _clients = new();

    private UdpClient? _client;
    private UdpClient? _server;
    private IPEndPoint? _remoteEndpoint;
    private string? _sessionId;
    private string? _remoteHost;
    private int _connectionPort;

    private CancellationTokenSource? _clientCts;
    private Task? _heartbeatTask;
    private CancellationTokenSource? _serverCts;
    private Task? _serverTask;

    private ConnectionState _state = ConnectionState.Disconnected;

    public UdpConnectionManager(
        IOptions<AppConfiguration> configuration,
        IDeviceRoleService deviceRoleService,
        ISettingsService settingsService,
        ILatencyMonitor latencyMonitor,
        ILogger<UdpConnectionManager> logger)
    {
        _configuration = configuration.Value;
        _deviceRoleService = deviceRoleService;
        _settingsService = settingsService;
        _latencyMonitor = latencyMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public ConnectionState State => _state;

    /// <inheritdoc />
    public bool IsConnected => _deviceRoleService.IsDesktop
        ? _serverTask is { IsCompleted: false }
        : _state == ConnectionState.Connected;

    /// <inheritdoc />
    public string? RemoteHost => _remoteHost;

    /// <inheritdoc />
    public string? SessionId => _sessionId;

    /// <inheritdoc />
    public int ConnectedClientCount => _clients.Count;

    /// <inheritdoc />
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        if (_deviceRoleService.IsDesktop)
            throw new InvalidOperationException("Masaüstü modunda ConnectAsync kullanılamaz.");

        await DisconnectClientAsync(cancellationToken).ConfigureAwait(false);

        await ConnectInternalAsync(host, port, cancellationToken).ConfigureAwait(false);
    }

    private async Task ConnectInternalAsync(string host, int port, CancellationToken cancellationToken)
    {
        SetState(ConnectionState.Connecting, host, "Bağlanılıyor...");

        _remoteHost = host;
        _connectionPort = port;
        _remoteEndpoint = new IPEndPoint(IPAddress.Parse(host), port);

        _client = new UdpClient();
        _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        var clientName = Environment.MachineName;
        var connectPayload = ConnectionMessageFormatter.CreateConnect(clientName);

        await _client.SendAsync(connectPayload, _remoteEndpoint, cancellationToken).ConfigureAwait(false);

        var response = await ReceiveMessageAsync(
            _client,
            _configuration.Connection.ConnectTimeoutMs,
            cancellationToken).ConfigureAwait(false);

        if (response is null
            || !ConnectionMessageFormatter.TryParse(response.Value.Buffer, out var messageType, out var lines))
        {
            await CleanupClientAsync().ConfigureAwait(false);
            SetState(ConnectionState.Disconnected, host, "Bağlantı yanıtı alınamadı");
            throw new TimeoutException("Sunucudan bağlantı yanıtı alınamadı.");
        }

        if (messageType == ConnectionProtocol.ConnectFail)
        {
            var reason = lines.Length > 1 ? lines[1] : "Bilinmeyen hata";
            await CleanupClientAsync().ConfigureAwait(false);
            SetState(ConnectionState.Disconnected, host, reason);
            throw new InvalidOperationException($"Bağlantı reddedildi: {reason}");
        }

        if (messageType != ConnectionProtocol.ConnectOk || lines.Length < 2)
        {
            await CleanupClientAsync().ConfigureAwait(false);
            SetState(ConnectionState.Disconnected, host, "Geçersiz bağlantı yanıtı");
            throw new InvalidOperationException("Geçersiz bağlantı yanıtı alındı.");
        }

        _sessionId = lines[1];

        // Durum, heartbeat döngüsü başlatılmadan ÖNCE Connected yapılmalı; aksi halde
        // döngü ilk kontrolünde _state == Connected olmadığından hemen çıkar ve
        // hiç heartbeat gönderilmez (dolayısıyla gecikme de ölçülmez).
        SetState(ConnectionState.Connected, host, "Bağlandı");

        _clientCts = new CancellationTokenSource();
        _heartbeatTask = HeartbeatLoopAsync(_clientCts.Token);

        _logger.LogInformation("Bağlantı kuruldu: {Host}:{Port}, Session: {SessionId}", host, port, _sessionId);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_deviceRoleService.IsPhone)
        {
            await DisconnectClientAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await StopServerAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<double> MeasureLatencyAsync(CancellationToken cancellationToken = default)
    {
        if (_client is null || _remoteEndpoint is null || _state != ConnectionState.Connected)
            return -1;

        await _clientIoLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var timestamp = Stopwatch.GetTimestamp();
            var pingPayload = ConnectionMessageFormatter.CreatePing(timestamp);

            await _client.SendAsync(pingPayload, _remoteEndpoint, cancellationToken).ConfigureAwait(false);

            var response = await ReceiveMessageAsync(
                _client,
                _configuration.Connection.HeartbeatTimeoutMs,
                cancellationToken).ConfigureAwait(false);

            if (response is null
                || !ConnectionMessageFormatter.TryParse(response.Value.Buffer, out var messageType, out var lines)
                || messageType != ConnectionProtocol.Pong
                || lines.Length < 2
                || !long.TryParse(lines[1], out var returnedTicks)
                || returnedTicks != timestamp)
            {
                return -1;
            }

            var latencyMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            _latencyMonitor.UpdateLatency(latencyMs);

            _logger.LogDebug("Gecikme ölçüldü: {LatencyMs:F1} ms", latencyMs);

            return latencyMs;
        }
        finally
        {
            _clientIoLock.Release();
        }
    }

    /// <inheritdoc />
    public Task StartServerAsync(CancellationToken cancellationToken = default)
    {
        if (!_deviceRoleService.IsDesktop)
            throw new InvalidOperationException("Sunucu yalnızca masaüstü modunda başlatılabilir.");

        lock (_sync)
        {
            if (_serverTask is { IsCompleted: false })
                return Task.CompletedTask;

            _serverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _serverTask = ServerLoopAsync(_serverCts.Token);
        }

        SetState(ConnectionState.Connected, null, $"Bağlantı sunucusu aktif (port {_configuration.Connection.Port})");

        _logger.LogInformation("Bağlantı sunucusu başlatıldı. Port: {Port}", _configuration.Connection.Port);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopServerAsync(CancellationToken cancellationToken = default)
    {
        Task? serverTask;

        lock (_sync)
        {
            if (_serverCts is null)
                return;

            _serverCts.Cancel();
            serverTask = _serverTask;
        }

        if (serverTask is not null)
        {
            try
            {
                await serverTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Beklenen durum
            }
        }

        lock (_sync)
        {
            _serverCts?.Dispose();
            _serverCts = null;
            _serverTask = null;
            _server?.Dispose();
            _server = null;
            _clients.Clear();
        }

        SetState(ConnectionState.Disconnected, null, "Bağlantı sunucusu durduruldu");

        _logger.LogInformation("Bağlantı sunucusu durduruldu.");
    }

    private async Task ServerLoopAsync(CancellationToken cancellationToken)
    {
        _server = new UdpClient(_configuration.Connection.Port);
        _server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;

            try
            {
                result = await _server.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Bağlantı sunucusunda soket hatası");
                break;
            }

            if (!ConnectionMessageFormatter.TryParse(result.Buffer, out var messageType, out var lines))
                continue;

            try
            {
                await HandleServerMessageAsync(messageType, lines, result.RemoteEndPoint, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sunucu mesajı işlenirken hata: {MessageType}", messageType);
            }
        }
    }

    private async Task HandleServerMessageAsync(
        string messageType,
        string[] lines,
        IPEndPoint remoteEndPoint,
        CancellationToken cancellationToken)
    {
        switch (messageType)
        {
            case ConnectionProtocol.Connect:
                await HandleConnectRequestAsync(lines, remoteEndPoint, cancellationToken).ConfigureAwait(false);
                break;

            case ConnectionProtocol.Ping:
                await HandlePingAsync(lines, remoteEndPoint, cancellationToken).ConfigureAwait(false);
                break;

            case ConnectionProtocol.Heartbeat:
                await HandleHeartbeatAsync(lines, remoteEndPoint, cancellationToken).ConfigureAwait(false);
                break;

            case ConnectionProtocol.Disconnect:
                HandleDisconnectRequest(lines, remoteEndPoint);
                break;
        }
    }

    private async Task HandleConnectRequestAsync(
        string[] lines,
        IPEndPoint remoteEndPoint,
        CancellationToken cancellationToken)
    {
        if (_server is null)
            return;

        var clientName = lines.Length > 1 ? lines[1] : "Unknown";
        var sessionId = Guid.NewGuid().ToString("N");

        var session = new ClientSession
        {
            SessionId = sessionId,
            ClientName = clientName,
            Endpoint = remoteEndPoint,
            ConnectedAt = DateTimeOffset.UtcNow
        };

        _clients[sessionId] = session;

        var response = ConnectionMessageFormatter.CreateConnectOk(sessionId);
        await _server.SendAsync(response, remoteEndPoint, cancellationToken).ConfigureAwait(false);

        SetState(ConnectionState.Connected, remoteEndPoint.Address.ToString(),
            $"{clientName} bağlandı");

        _logger.LogInformation(
            "İstemci bağlandı: {ClientName} ({Address}), Session: {SessionId}",
            clientName,
            remoteEndPoint,
            sessionId);
    }

    private async Task HandlePingAsync(
        string[] lines,
        IPEndPoint remoteEndPoint,
        CancellationToken cancellationToken)
    {
        if (_server is null || lines.Length < 2 || !long.TryParse(lines[1], out var timestamp))
            return;

        var response = ConnectionMessageFormatter.CreatePong(timestamp);
        await _server.SendAsync(response, remoteEndPoint, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleHeartbeatAsync(
        string[] lines,
        IPEndPoint remoteEndPoint,
        CancellationToken cancellationToken)
    {
        if (_server is null || lines.Length < 2)
            return;

        var sessionId = lines[1];

        if (!_clients.ContainsKey(sessionId))
            return;

        // İstemci ölçtüğü RTT'yi eklediyse masaüstünde göstermek için kaydet.
        if (lines.Length > 2
            && double.TryParse(lines[2], System.Globalization.CultureInfo.InvariantCulture, out var clientLatencyMs)
            && clientLatencyMs >= 0)
        {
            _latencyMonitor.UpdateLatency(clientLatencyMs);
        }

        var response = ConnectionMessageFormatter.CreateHeartbeatAck(sessionId);
        await _server.SendAsync(response, remoteEndPoint, cancellationToken).ConfigureAwait(false);
    }

    private void HandleDisconnectRequest(string[] lines, IPEndPoint remoteEndPoint)
    {
        if (lines.Length < 2)
            return;

        var sessionId = lines[1];

        if (_clients.TryRemove(sessionId, out var session))
        {
            SetState(ConnectionState.Connected, remoteEndPoint.Address.ToString(),
                $"{session.ClientName} bağlantısı kesildi");

            _logger.LogInformation("İstemci ayrıldı: {ClientName}, Session: {SessionId}", session.ClientName, sessionId);
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        var settings = _settingsService.GetSettings();
        var connection = _configuration.Connection;
        var lastLatencyMs = -1d;

        while (!cancellationToken.IsCancellationRequested && _state == ConnectionState.Connected)
        {
            try
            {
                await Task.Delay(connection.HeartbeatIntervalMs, cancellationToken).ConfigureAwait(false);

                if (_client is null || _remoteEndpoint is null || _sessionId is null)
                    break;

                await _clientIoLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    var heartbeatStarted = Stopwatch.GetTimestamp();

                    // Ölçülen son RTT'yi heartbeat ile birlikte gönder; sunucu bunu gösterir.
                    var payload = ConnectionMessageFormatter.CreateHeartbeat(_sessionId, lastLatencyMs);
                    await _client.SendAsync(payload, _remoteEndpoint, cancellationToken).ConfigureAwait(false);

                    var response = await ReceiveMessageAsync(
                        _client,
                        connection.HeartbeatTimeoutMs,
                        cancellationToken).ConfigureAwait(false);

                    var heartbeatOk = response is not null
                        && ConnectionMessageFormatter.TryParse(response.Value.Buffer, out var type, out var lines)
                        && type == ConnectionProtocol.HeartbeatAck
                        && lines.Length > 1
                        && lines[1] == _sessionId;

                    if (!heartbeatOk)
                    {
                        _logger.LogWarning("Heartbeat yanıtı alınamadı");
                        await HandleConnectionLostAsync(settings.AutoReconnect, cancellationToken).ConfigureAwait(false);
                        break;
                    }

                    var latencyMs = Stopwatch.GetElapsedTime(heartbeatStarted).TotalMilliseconds;
                    _latencyMonitor.UpdateLatency(latencyMs);
                    lastLatencyMs = latencyMs;
                }
                finally
                {
                    _clientIoLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Heartbeat döngüsünde hata");
                await HandleConnectionLostAsync(settings.AutoReconnect, cancellationToken).ConfigureAwait(false);
                break;
            }
        }
    }

    private async Task HandleConnectionLostAsync(bool autoReconnect, CancellationToken cancellationToken)
    {
        var host = _remoteHost;
        var port = _connectionPort;

        await CleanupClientAsync().ConfigureAwait(false);

        if (!autoReconnect || string.IsNullOrEmpty(host))
        {
            SetState(ConnectionState.Disconnected, host, "Bağlantı kesildi");
            return;
        }

        SetState(ConnectionState.Reconnecting, host, "Yeniden bağlanılıyor...");

        var connection = _configuration.Connection;

        for (var attempt = 1; attempt <= connection.MaxReconnectAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "Yeniden bağlanma denemesi {Attempt}/{MaxAttempts}",
                attempt,
                connection.MaxReconnectAttempts);

            try
            {
                await Task.Delay(connection.ReconnectDelayMs, cancellationToken).ConfigureAwait(false);
                await ConnectInternalAsync(host, port, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Yeniden bağlanma denemesi başarısız");
            }
        }

        SetState(ConnectionState.Disconnected, host, "Yeniden bağlanılamadı");
    }

    private async Task DisconnectClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null && _remoteEndpoint is not null && _sessionId is not null)
        {
            try
            {
                var payload = ConnectionMessageFormatter.CreateDisconnect(_sessionId);
                await _client.SendAsync(payload, _remoteEndpoint, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Disconnect mesajı gönderilemedi");
            }
        }

        await CleanupClientAsync().ConfigureAwait(false);
        SetState(ConnectionState.Disconnected, null, "Bağlantı kesildi");

        _logger.LogInformation("İstemci bağlantısı kesildi");
    }

    private async Task CleanupClientAsync()
    {
        if (_clientCts is not null)
        {
            await _clientCts.CancelAsync().ConfigureAwait(false);

            if (_heartbeatTask is not null)
            {
                try
                {
                    await _heartbeatTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Beklenen durum
                }
            }

            _clientCts.Dispose();
            _clientCts = null;
            _heartbeatTask = null;
        }

        _client?.Dispose();
        _client = null;
        _sessionId = null;
        _remoteEndpoint = null;
    }

    private async Task<UdpReceiveResult?> ReceiveMessageAsync(
        UdpClient client,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            return await client.ReceiveAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private void SetState(ConnectionState state, string? remoteHost, string? message)
    {
        _state = state;
        _remoteHost = remoteHost ?? _remoteHost;

        StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            State = state,
            RemoteHost = remoteHost ?? _remoteHost,
            Message = message
        });
    }

    private sealed class ClientSession
    {
        public required string SessionId { get; init; }

        public required string ClientName { get; init; }

        public required IPEndPoint Endpoint { get; init; }

        public DateTimeOffset ConnectedAt { get; init; }
    }
}

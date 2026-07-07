using System.Buffers;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WirelessMic.Application.DTO;
using WirelessMic.Application.Interfaces;
using WirelessMic.Domain.Audio;
using WirelessMic.Domain.Enums;
using WirelessMic.Shared.Constants;

namespace WirelessMic.App.ViewModels;

/// <summary>
/// Ana sayfa ViewModel'i.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IDeviceRoleService _deviceRoleService;
    private readonly ISettingsService _settingsService;
    private readonly INetworkDiscovery _networkDiscovery;
    private readonly IConnectionManager _connectionManager;
    private readonly ILatencyMonitor _latencyMonitor;
    private readonly IPermissionService _permissionService;
    private readonly IMicrophoneService _microphoneService;
    private readonly IAudioStreamService _audioStreamService;
    private readonly IVirtualMicrophoneOutput _virtualMicrophone;
    private readonly IAudioPlayer _audioMonitor;
    private readonly ILogger<MainViewModel> _logger;
    private readonly AppConfiguration _configuration;

    [ObservableProperty]
    private string _title = AppConstants.AppName;

    [ObservableProperty]
    private string _deviceRoleText = string.Empty;

    [ObservableProperty]
    private string _platformText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Hazır";

    [ObservableProperty]
    private bool _isDesktop;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DiscoverServersCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConnectToServerCommand))]
    [NotifyPropertyChangedFor(nameof(ShowRescan))]
    private bool _isPhone;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DiscoverServersCommand))]
    [NotifyPropertyChangedFor(nameof(ConnectionHeadline))]
    [NotifyPropertyChangedFor(nameof(ShowRescan))]
    private bool _isDiscovering;

    [ObservableProperty]
    private bool _isListening;

    [ObservableProperty]
    private bool _hasDiscoveredServers;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectToServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyPropertyChangedFor(nameof(ConnectionHeadline))]
    private DiscoveredServerDto? _selectedServer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectToServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyPropertyChangedFor(nameof(ConnectionHeadline))]
    [NotifyPropertyChangedFor(nameof(IsLive))]
    [NotifyPropertyChangedFor(nameof(IsStandby))]
    [NotifyPropertyChangedFor(nameof(ShowRescan))]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectionHeadline))]
    [NotifyPropertyChangedFor(nameof(ShowRescan))]
    private bool _isConnecting;

    [ObservableProperty]
    private string _latencyText = string.Empty;

    [ObservableProperty]
    private string _latencyValueText = "--";

    [ObservableProperty]
    private string _localIpText = "--";

    [ObservableProperty]
    private string _bitrateText = "--";

    [ObservableProperty]
    private bool _autoReconnectEnabled = true;

    [ObservableProperty]
    private bool _gainBoostEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLive))]
    [NotifyPropertyChangedFor(nameof(IsStandby))]
    private bool _isMicrophoneActive;

    /// <summary>Durum kartı başlığı (ör. "Connected to PC-102").</summary>
    public string ConnectionHeadline =>
        IsConnected
            ? $"Connected to {SelectedServer?.ComputerName ?? _connectionManager.RemoteHost ?? "device"}"
            : IsConnecting
                ? "Connecting..."
                : IsDiscovering
                    ? "Searching..."
                    : "Disconnected";

    /// <summary>Canlı yayın (bağlı ve mikrofon aktif) göstergesi.</summary>
    public bool IsLive => IsConnected && IsMicrophoneActive;

    /// <summary>Canlı yayın dışı durum (görsel için).</summary>
    public bool IsStandby => !IsLive;

    /// <summary>Otomatik bağlantı sonuçsuz kaldığında yeniden tarama butonu görünür.</summary>
    public bool ShowRescan => IsPhone && !IsConnected && !IsConnecting && !IsDiscovering;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToggleMicrophoneTestCommand))]
    private int _connectedClientCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToggleMicrophoneTestCommand))]
    [NotifyPropertyChangedFor(nameof(ShowVirtualMicrophoneHint))]
    private bool _isVbCableReady;

    [ObservableProperty]
    private string _virtualMicrophoneHint = string.Empty;

    /// <summary>Masaüstü ipucu yalnızca VB-Cable eksikken (kritik uyarı) gösterilir.</summary>
    public bool ShowVirtualMicrophoneHint => IsDesktop && !IsVbCableReady;

    [ObservableProperty]
    private bool _isMicrophoneTestEnabled;

    [ObservableProperty]
    private string _microphoneTestButtonText = "Mikrofon Testi";

    public ObservableCollection<DiscoveredServerDto> DiscoveredServers { get; } = [];

    public MainViewModel(
        IDeviceRoleService deviceRoleService,
        ISettingsService settingsService,
        INetworkDiscovery networkDiscovery,
        IConnectionManager connectionManager,
        ILatencyMonitor latencyMonitor,
        IPermissionService permissionService,
        IMicrophoneService microphoneService,
        IAudioStreamService audioStreamService,
        IVirtualMicrophoneOutput virtualMicrophone,
        IAudioPlayer audioMonitor,
        IOptions<AppConfiguration> configuration,
        ILogger<MainViewModel> logger)
    {
        _deviceRoleService = deviceRoleService;
        _settingsService = settingsService;
        _networkDiscovery = networkDiscovery;
        _connectionManager = connectionManager;
        _latencyMonitor = latencyMonitor;
        _permissionService = permissionService;
        _microphoneService = microphoneService;
        _audioStreamService = audioStreamService;
        _virtualMicrophone = virtualMicrophone;
        _audioMonitor = audioMonitor;
        _configuration = configuration.Value;
        _logger = logger;

        _connectionManager.StateChanged += OnConnectionStateChanged;
        _microphoneService.FrameCaptured += OnMicrophoneFrameCaptured;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await _settingsService.LoadSettingsAsync();

        var role = _deviceRoleService.GetDeviceRole();
        var platform = _deviceRoleService.GetPlatformType();
        var settings = _settingsService.GetSettings();

        IsDesktop = role == DeviceRole.Desktop;
        IsPhone = role == DeviceRole.Phone;
        DeviceRoleText = role == DeviceRole.Desktop ? "Masaüstü Modu" : "Telefon Modu";
        PlatformText = platform.ToString();
        AutoReconnectEnabled = settings.AutoReconnect;
        GainBoostEnabled = settings.GainBoost;
        LocalIpText = ResolveLocalIp();
        BitrateText = CalculateBitrateKbps().ToString();

        _logger.LogInformation(
            "Uygulama başlatıldı. Rol: {Role}, Platform: {Platform}, Keşif portu: {Port}",
            role,
            platform,
            _configuration.Discovery.Port);

        if (IsDesktop)
        {
            await StartDesktopServicesAsync();
        }
        else
        {
            // Telefon: açılışta otomatik bağlanma yok. Kullanıcı merkez butona
            // dokununca keşif + bağlanma akışı başlar.
            StatusText = "Bağlanmak için mikrofona dokunun";
        }
    }

    /// <summary>
    /// Telefonda açılışta ağı tarar ve bulunan ilk bilgisayara otomatik bağlanır.
    /// Cihaz bulunamazsa kısa aralıklarla birkaç kez yeniden tarar.
    /// </summary>
    private async Task AutoConnectFlowAsync()
    {
        const int maxScans = 4;

        for (var scan = 1; scan <= maxScans && !IsConnected && !IsConnecting; scan++)
        {
            await DiscoverServersAsync();

            if (DiscoveredServers.Count > 0)
            {
                SelectedServer = DiscoveredServers[0];
                await ConnectToServerAsync();

                if (IsConnected)
                    return;
            }

            if (scan < maxScans && !IsConnected)
                await Task.Delay(1500);
        }
    }

    private static string ResolveLocalIp()
    {
        try
        {
            using var socket = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Dgram,
                0);

            // Gerçek paket göndermeden yerel yönlendirme adresini belirler.
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is System.Net.IPEndPoint endpoint)
                return endpoint.Address.ToString();
        }
        catch
        {
            // Ağ yoksa sessizce geç.
        }

        return "--";
    }

    [RelayCommand(CanExecute = nameof(CanDiscover))]
    public async Task DiscoverServersAsync()
    {
        if (IsDiscovering)
            return;

        try
        {
            IsDiscovering = true;
            StatusText = "Bilgisayarlar aranıyor...";
            DiscoveredServers.Clear();
            HasDiscoveredServers = false;
            SelectedServer = null;

            var servers = await _networkDiscovery.DiscoverServersAsync();

            foreach (var server in servers)
                DiscoveredServers.Add(server);

            HasDiscoveredServers = DiscoveredServers.Count > 0;
            StatusText = HasDiscoveredServers
                ? $"{DiscoveredServers.Count} bilgisayar bulundu"
                : "Bilgisayar bulunamadı. Aynı ağda olduğunuzdan emin olun.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keşif sırasında hata oluştu");
            StatusText = "Keşif başarısız oldu";
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    public async Task ConnectToServerAsync()
    {
        if (SelectedServer is null)
            return;

        try
        {
            IsConnecting = true;
            StatusText = $"{SelectedServer.ComputerName} bilgisayarına bağlanılıyor...";

            if (!await _permissionService.RequestMicrophonePermissionAsync())
            {
                StatusText = "Mikrofon izni gerekli";
                return;
            }

            await _connectionManager.ConnectAsync(
                SelectedServer.IpAddress,
                _configuration.Connection.Port);

            await _audioStreamService.StartAsync();
            await _microphoneService.StartCaptureAsync();

            IsMicrophoneActive = _microphoneService.IsCapturing;
            UpdateConnectionUi();
            StatusText = IsMicrophoneActive
                ? "Bağlandı — mikrofon aktif"
                : "Bağlandı";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bağlantı kurulamadı");
            StatusText = "Bağlantı kurulamadı";
            IsConnected = false;
            await StopAudioPipelineAsync();
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    public async Task RescanAsync()
    {
        if (IsPhone && !IsConnecting && !IsDiscovering && !IsConnected)
            await AutoConnectFlowAsync();
    }

    /// <summary>
    /// Merkez mikrofon butonuna dokunulduğunda çalışır: bağlıysa bağlantıyı keser,
    /// değilse otomatik keşif + bağlanma akışını başlatır.
    /// </summary>
    [RelayCommand]
    public async Task MicTapAsync()
    {
        if (!IsPhone)
            return;

        if (IsConnected)
        {
            await DisconnectAsync();
            return;
        }

        if (!IsConnecting && !IsDiscovering)
            await AutoConnectFlowAsync();
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    public async Task DisconnectAsync()
    {
        try
        {
            // Ses hattı teardown'ı hata verse bile bağlantı MUTLAKA kesilmeli.
            await StopAudioPipelineAsync();
            await _connectionManager.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bağlantı kesilirken hata oluştu");
        }
        finally
        {
            UpdateConnectionUi();
            StatusText = "Bağlantı kesildi";
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleMicrophoneTest))]
    public async Task ToggleMicrophoneTestAsync()
    {
        try
        {
            var enabled = !IsMicrophoneTestEnabled;
            await _audioMonitor.SetMonitoringEnabledAsync(enabled);
            IsMicrophoneTestEnabled = _audioMonitor.IsMonitoringEnabled;
            MicrophoneTestButtonText = IsMicrophoneTestEnabled
                ? "Mikrofon Testini Kapat"
                : "Mikrofon Testi";

            StatusText = IsMicrophoneTestEnabled
                ? "Mikrofon testi açık — ses hoparlörden dinleniyor"
                : "Mikrofon testi kapalı — ses yalnızca sanal mikrofona gidiyor";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mikrofon testi değiştirilemedi");
            StatusText = "Mikrofon testi başlatılamadı";
        }
    }

    private bool CanDiscover() => IsPhone && !IsDiscovering && !IsConnected;

    private bool CanConnect() =>
        IsPhone && SelectedServer is not null && !IsConnected && !IsConnecting;

    private bool CanDisconnect() => IsPhone && IsConnected;

    private bool CanToggleMicrophoneTest() =>
        IsDesktop && IsVbCableReady && ConnectedClientCount > 0;

    private async Task StartDesktopServicesAsync()
    {
        try
        {
            await _networkDiscovery.StartListeningAsync();
            IsListening = _networkDiscovery.IsListening;

            await _connectionManager.StartServerAsync();
            await _virtualMicrophone.StartAsync();
            await _audioStreamService.StartAsync();

            IsVbCableReady = _virtualMicrophone.IsVirtualCableReady;
            UpdateVirtualMicrophoneUi();

            ConnectedClientCount = _connectionManager.ConnectedClientCount;
            StatusText = IsListening
                ? IsVbCableReady
                    ? "Sanal mikrofon hazır"
                    : "VB-Cable bulunamadı — sanal mikrofon çalışmıyor"
                : "Servisler başlatılamadı";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Masaüstü servisleri başlatılamadı");
            StatusText = "Servisler başlatılamadı";
        }
    }

    private void UpdateVirtualMicrophoneUi()
    {
        // Hazır durumda teknik/log gibi uzun açıklama gösterilmez; yalnızca
        // VB-Cable eksikse kısa ve kritik bir uyarı verilir.
        VirtualMicrophoneHint = IsVbCableReady
            ? string.Empty
            : "VB-Cable kurulu değil. vb-audio.com/Cable adresinden kurup uygulamayı yeniden başlatın.";
    }

    private async Task StopAudioPipelineAsync()
    {
        // Her iki servis de bağımsızca durdurulmalı; biri hata verirse diğeri etkilenmesin.
        try
        {
            await _microphoneService.StopCaptureAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mikrofon durdurulurken hata oluştu");
        }

        try
        {
            await _audioStreamService.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ses akışı durdurulurken hata oluştu");
        }

        IsMicrophoneActive = false;
    }

    private void OnMicrophoneFrameCaptured(object? sender, ReadOnlyMemory<byte> frame)
    {
        if (!GainBoostEnabled)
        {
            _audioStreamService.SubmitCapturedFrame(frame.Span);
            return;
        }

        // Kazanç yerinde uygulanacağı için karenin değiştirilebilir bir kopyası gerekir.
        // SubmitCapturedFrame span'i eşzamanlı olarak tükettiğinden tampon hemen iade edilebilir.
        byte[] buffer = ArrayPool<byte>.Shared.Rent(frame.Length);
        try
        {
            Span<byte> pcm = buffer.AsSpan(0, frame.Length);
            frame.Span.CopyTo(pcm);
            PcmGainProcessor.ApplyGainInPlace(pcm, PcmGainProcessor.GainBoostFactor);
            _audioStreamService.SubmitCapturedFrame(pcm);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int CalculateBitrateKbps() =>
        AudioConstants.SampleRate * AudioConstants.BitsPerSample * AudioConstants.Channels / 1000;

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsConnecting = e.State == ConnectionState.Connecting
                || e.State == ConnectionState.Reconnecting;
            IsConnected = _connectionManager.IsConnected && _deviceRoleService.IsPhone
                || _connectionManager.ConnectedClientCount > 0;

            ConnectedClientCount = _connectionManager.ConnectedClientCount;
            IsMicrophoneActive = _microphoneService.IsCapturing;

            if (ConnectedClientCount == 0 && IsMicrophoneTestEnabled)
            {
                _ = DisableMicrophoneTestAsync();
            }

            ToggleMicrophoneTestCommand.NotifyCanExecuteChanged();

            if (!string.IsNullOrWhiteSpace(e.Message))
                StatusText = e.Message;

            UpdateLatencyText();
        });
    }

    private async Task DisableMicrophoneTestAsync()
    {
        await _audioMonitor.SetMonitoringEnabledAsync(false);
        IsMicrophoneTestEnabled = false;
        MicrophoneTestButtonText = "Mikrofon Testi";
    }

    private void UpdateConnectionUi()
    {
        IsConnected = _connectionManager.State == ConnectionState.Connected
            && _deviceRoleService.IsPhone;
        ConnectedClientCount = _connectionManager.ConnectedClientCount;
        IsMicrophoneActive = _microphoneService.IsCapturing;
        ToggleMicrophoneTestCommand.NotifyCanExecuteChanged();
        UpdateLatencyText();
    }

    private void UpdateLatencyText()
    {
        var latency = _latencyMonitor.CurrentLatencyMs;
        LatencyText = latency > 0 ? $"Gecikme: {latency:F0} ms" : string.Empty;
        LatencyValueText = latency > 0 ? $"{latency:F0}" : "--";
    }

    /// <summary>
    /// UI zamanlayıcısı tarafından çağrılır; bağlıyken gecikme gibi gerçek zamanlı
    /// değerleri (heartbeat aralarında da) tazeler.
    /// </summary>
    public void RefreshLiveStats() => UpdateLatencyText();

    /// <summary>
    /// Auto-Reconnect anahtarı değiştiğinde ayarı kalıcı olarak kaydeder.
    /// </summary>
    partial void OnAutoReconnectEnabledChanged(bool value)
    {
        var settings = _settingsService.GetSettings();
        if (settings.AutoReconnect == value)
            return;

        settings.AutoReconnect = value;
        _ = _settingsService.SaveSettingsAsync(settings);
    }

    /// <summary>
    /// Gain Boost anahtarı değiştiğinde ayarı kalıcı olarak kaydeder. Kazanç,
    /// bir sonraki yakalanan kareden itibaren <see cref="OnMicrophoneFrameCaptured"/>
    /// içinde uygulanır.
    /// </summary>
    partial void OnGainBoostEnabledChanged(bool value)
    {
        var settings = _settingsService.GetSettings();
        if (settings.GainBoost == value)
            return;

        settings.GainBoost = value;
        _ = _settingsService.SaveSettingsAsync(settings);
    }
}

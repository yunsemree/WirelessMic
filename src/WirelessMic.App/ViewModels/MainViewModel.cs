using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WirelessMic.Application.DTO;
using WirelessMic.Application.Interfaces;
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
    private bool _isPhone;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DiscoverServersCommand))]
    private bool _isDiscovering;

    [ObservableProperty]
    private bool _isListening;

    [ObservableProperty]
    private bool _hasDiscoveredServers;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectToServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private DiscoveredServerDto? _selectedServer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectToServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private string _latencyText = string.Empty;

    [ObservableProperty]
    private bool _isMicrophoneActive;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToggleMicrophoneTestCommand))]
    private int _connectedClientCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToggleMicrophoneTestCommand))]
    private bool _isVbCableReady;

    [ObservableProperty]
    private string _virtualMicrophoneHint = string.Empty;

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

        _logger.LogInformation(
            "Uygulama başlatıldı. Rol: {Role}, Platform: {Platform}, Keşif portu: {Port}",
            role,
            platform,
            _configuration.Discovery.Port);

        if (IsDesktop)
        {
            await StartDesktopServicesAsync();
        }
        else if (settings.AutoDiscovery)
        {
            await DiscoverServersAsync();
        }
        else
        {
            StatusText = "Keşif için 'Ara' butonuna basın";
        }
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

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    public async Task DisconnectAsync()
    {
        try
        {
            await StopAudioPipelineAsync();
            await _connectionManager.DisconnectAsync();
            UpdateConnectionUi();
            StatusText = "Bağlantı kesildi";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bağlantı kesilirken hata oluştu");
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
                    ? $"Sanal mikrofon hazır. Keşif ({_configuration.Discovery.Port}), bağlantı ({_configuration.Connection.Port}), ses ({_configuration.Audio.StreamPort}) aktif"
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
        VirtualMicrophoneHint = IsVbCableReady
            ? "Ses VB-Cable üzerinden harici mikrofon olarak aktarılır. OBS/Discord'da 'CABLE Output (VB-Audio Virtual Cable)' seçin. Hoparlörden dinlemek için mikrofon testini kullanın."
            : "VB-Cable kurulu değil. https://vb-audio.com/Cable/ adresinden kurun ve uygulamayı yeniden başlatın.";
    }

    private async Task StopAudioPipelineAsync()
    {
        await _microphoneService.StopCaptureAsync();
        await _audioStreamService.StopAsync();
        IsMicrophoneActive = false;
    }

    private void OnMicrophoneFrameCaptured(object? sender, ReadOnlyMemory<byte> frame) =>
        _audioStreamService.SubmitCapturedFrame(frame.Span);

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
    }
}

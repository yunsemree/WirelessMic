using System.ComponentModel;
using WirelessMic.App.Controls;
using WirelessMic.App.ViewModels;

namespace WirelessMic.App;

/// <summary>
/// Ana sayfa.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly MicVisualizerDrawable _visualizer = new();
    private IDispatcherTimer? _animationTimer;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Visualizer.Drawable = _visualizer;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        await _viewModel.InitializeCommand.ExecuteAsync(null);
        UpdateVisualState();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsConnected)
            or nameof(MainViewModel.IsConnecting)
            or nameof(MainViewModel.IsDiscovering)
            or nameof(MainViewModel.IsLive))
        {
            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        var connected = _viewModel.IsConnected;
        var connecting = (_viewModel.IsConnecting || _viewModel.IsDiscovering) && !connected;

        _visualizer.Active = connected;
        _visualizer.Connecting = connecting;

        if (connected || connecting)
            StartAnimation();
        else
            StopAnimation();
    }

    private void StartAnimation()
    {
        if (_animationTimer is null)
        {
            _animationTimer = Dispatcher.CreateTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(33);
            _animationTimer.Tick += OnAnimationTick;
        }

        if (!_animationTimer.IsRunning)
            _animationTimer.Start();
    }

    private void StopAnimation()
    {
        _animationTimer?.Stop();
        _visualizer.Phase = 0;
        Visualizer.Invalidate();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        // ~2.5 sn periyotlu nefes / pulse
        _visualizer.Phase += 0.08;
        Visualizer.Invalidate();

        // Bağlıyken gecikme gibi canlı değerleri tazele
        _viewModel.RefreshLiveStats();
    }

    // Dokunma geri bildirimi: kullanıcı butona dokunduğunu anlasın diye kısa bir "bounce".
    private async void OnVisualizerTapped(object? sender, TappedEventArgs e)
    {
        await Visualizer.ScaleToAsync(0.93, 90, Easing.CubicOut);
        await Visualizer.ScaleToAsync(1.0, 90, Easing.CubicIn);
    }
}

namespace WirelessMic.App;

/// <summary>
/// Uygulama kök sınıfı.
/// </summary>
public partial class App : IApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell());
}

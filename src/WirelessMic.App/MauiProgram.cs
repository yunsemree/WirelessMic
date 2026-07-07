using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WirelessMic.Application.DependencyInjection;
using WirelessMic.Application.DTO;
using WirelessMic.Application.Interfaces;
using WirelessMic.App.Services;
using WirelessMic.App.ViewModels;
using WirelessMic.Infrastructure.DependencyInjection;
using WirelessMic.Infrastructure.Logging;

namespace WirelessMic.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var appPaths = new MauiAppPaths();
        builder.Services.AddSingleton<IAppPaths>(appPaths);

        var configuration = BuildConfiguration();
        builder.Services.AddSingleton<IConfiguration>(configuration);
        builder.Services.Configure<AppConfiguration>(configuration);

        builder.Services.AddPlatformServices();
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<MainPage>();

        SerilogConfiguration.ConfigureSerilog(appPaths, builder.Logging);

        return builder.Build();
    }

    private static IConfiguration BuildConfiguration()
    {
        using var stream = typeof(MauiProgram).Assembly
            .GetManifestResourceStream("WirelessMic.App.appsettings.json")
            ?? throw new InvalidOperationException("appsettings.json bulunamadı.");

        return new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
    }
}

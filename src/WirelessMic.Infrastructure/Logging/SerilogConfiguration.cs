using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using WirelessMic.Application.Interfaces;

namespace WirelessMic.Infrastructure.Logging;

/// <summary>
/// Serilog yapılandırması.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>Serilog logger'ını yapılandırır ve Microsoft.Extensions.Logging'e bağlar.</summary>
    public static void ConfigureSerilog(IAppPaths appPaths, ILoggingBuilder loggingBuilder)
    {
        Directory.CreateDirectory(appPaths.LogsDirectory);

        var logPath = Path.Combine(appPaths.LogsDirectory, "wirelessmic-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "WirelessMic")
            .WriteTo.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(Log.Logger, dispose: true);
    }
}

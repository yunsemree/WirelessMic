using Microsoft.Extensions.DependencyInjection;

namespace WirelessMic.Application.DependencyInjection;

/// <summary>
/// Application katmanı DI kayıtları.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Application katmanı servislerini kaydeder.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}

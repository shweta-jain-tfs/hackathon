using Microsoft.Extensions.DependencyInjection;

namespace Pharos.Server.Integration.Tests.Common;

internal static class CustomWebApplicationHelper
{
    public static void ReplaceScopedService<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.ReplaceService<TService, TImplementation>(false);
    }

    public static void ReplaceSingletonService<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.ReplaceService<TService, TImplementation>(true);
    }

    public static void ReplaceSingletonService<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService
    {
        services.RemoveService<TService>();
        services.AddSingleton<TService, TImplementation>(implementationFactory);
    }

    public static void RemoveService<TService>(this IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TService));

        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }

    public static void RemoveService<TService, TImplementation>(this IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TService) && d.ImplementationType == typeof(TImplementation));

        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }

    private static void ReplaceService<TService, TImplementation>(this IServiceCollection services, bool singleton)
        where TService : class
        where TImplementation : class, TService
    {
        services.RemoveService<TService>();

        if (singleton)
        {
            services.AddSingleton<TService, TImplementation>();
        }
        else
        {
            services.AddScoped<TService, TImplementation>();
        }
    }
}
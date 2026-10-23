using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Pharos.Server.Infrastructure.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a singleton service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="THostedService"/> to the
    /// specified <see cref="IServiceCollection"/> and then adds an <see cref="IHostedService"/> registration for <typeparamref name="THostedService"/>.
    /// Use this method to avoid double instantiation of the <typeparamref name="THostedService"/> instance.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="THostedService">An <see cref="IHostedService"/> to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
    /// <returns>The original <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHostedServiceAsSingleton<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
        where TService : class
        where THostedService : class, TService, IHostedService
    {
        services.AddSingleton<TService, THostedService>();

        services.AddSingleton(serviceProvider => (THostedService)serviceProvider.GetRequiredService<TService>());
        services.AddHostedService<BackgroundServiceStarter<THostedService>>();

        return services;
    }

    /// <summary>
    /// Adds a singleton service of the type specified in <typeparamref name="THostedService"/> to the
    /// specified <see cref="IServiceCollection"/> and then adds an <see cref="IHostedService"/> registration for <typeparamref name="THostedService"/>.
    /// Use this method to avoid double instantiation of the <typeparamref name="THostedService"/> instance.
    /// </summary>
    /// <typeparam name="THostedService">An <see cref="IHostedService"/> to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
    /// <returns>The original <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHostedServiceAsSingleton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
        where THostedService : class, IHostedService
    {
        services.AddSingleton<THostedService>();
        services.AddHostedService<BackgroundServiceStarter<THostedService>>();

        return services;
    }

    /// <summary>
    /// Represents a help class for correct starting of hosted services 
    /// that will also be injected as singleton services to other services.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class BackgroundServiceStarter<T>(T backgroundService) : IHostedService
        where T : IHostedService
    {
        private readonly T _backgroundService = backgroundService;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _backgroundService.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _backgroundService.StopAsync(cancellationToken);
        }
    }
}

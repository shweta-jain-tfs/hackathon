using Microsoft.Extensions.DependencyInjection;

namespace Pharos.Client.Common.ViewModels;

public static class ConfigureServices
{
    public static IServiceCollection AddCommonViewModels(this IServiceCollection services)
    {
        services.AddScoped<InstrumentFilterViewModel>();

        return services;
    }
}
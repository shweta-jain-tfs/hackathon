using Microsoft.Extensions.DependencyInjection;
using Pharos.Common.Web.Components.Loader;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Services;

namespace Pharos.Common.Web
{
    public static class ConfigureService
    {
        public static IServiceCollection AddCommonWebProviders(this IServiceCollection services)
        {
            services.AddSingleton<LoaderService>();
            services.AddTransient<DisplayLoaderAutomaticallyHttpMessageHandler>();
            services.AddScoped<IModalService, ModalService>();
            services.AddScoped<ModalState>();
            services.AddScoped<IComponentStateService, ComponentStateService>();

            return services;
        }
    }
}
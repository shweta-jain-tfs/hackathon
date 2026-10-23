using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Infrastructure.Database;
using Pharos.Server.SystemErrors.Infrastructure.Hubs;

namespace Pharos.Server.SystemErrors;

public class SystemErrorsModule : IServerAppModule
{
    public static IServerAppModule Instance { get; } = new SystemErrorsModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddDbContext<SystemErrorsDbContext>();
        services.AddScoped<ISystemErrorsDbContext>(provider => provider.GetRequiredService<SystemErrorsDbContext>());
        services.AddScoped<SystemErrorsDbContextInitializer>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);

        services.AddScoped<SystemErrorHub>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<SystemErrorHub>(HubRoutes.SystemError);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<SystemErrorsDbContextInitializer>();
    }
}
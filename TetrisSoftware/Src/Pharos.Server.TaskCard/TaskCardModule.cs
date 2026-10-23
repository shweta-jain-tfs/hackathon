using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.TaskCards;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.TaskCard.Application.Interfaces;
using Pharos.Server.TaskCard.Application.Services;
using Pharos.Server.TaskCard.Infrastructure.Database;
using Pharos.Server.TaskCard.Infrastructure.Hubs;

namespace Pharos.Server.TaskCard;

public class TaskCardModule : IServerAppModule
{
    private TaskCardModule()
    {
    }

    public static IServerAppModule Instance { get; } = new TaskCardModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.ConfigureSettings<TaskCardSettings>(configuration);

        services.AddSingleton<ITaskCardService, TaskCardService>();
        services.AddSingleton<ITaskCardRules, TaskCardRules>();
        services.AddSingleton<ISignalTowerManager, SignalTowerManager>();
        services.AddHostedServiceAsSingleton<TaskCardManager>();

        services.AddDbContext<TaskCardDbContext>();
        services.AddScoped<ITaskCardDbContext>(provider => provider.GetRequiredService<TaskCardDbContext>());
        services.AddScoped<TaskCardDbContextInitializer>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);

        services.AddScoped<ITaskCardHub, TaskCardHub>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<TaskCardHub>(HubRoutes.TaskCard);
    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<TaskCardDbContextInitializer>();
    }
}

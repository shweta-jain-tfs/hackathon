using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Mapping;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Application.Services.ResultCalculationService;
using Pharos.Server.Result.Application.Services.ResultExportService;
using Pharos.Server.Result.CurveFit;
using Pharos.Server.Result.Infrastructure.Database;
using Pharos.Server.Result.Infrastructure.Hubs;

namespace Pharos.Server.Result;

public class ResultModule : IServerAppModule
{
    private ResultModule()
    {
    }

    public static IServerAppModule Instance { get; } = new ResultModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddScoped<LisTestResultMapper>();

        services.ConfigureSettings<ResultSettings>(configuration);

        services.AddDbContext<ResultDbContext>();
        services.AddScoped<IResultDbContext>(provider => provider.GetRequiredService<ResultDbContext>());
        services.AddScoped<ResultDbContextInitializer>();
        services.AddScoped<IResultFormatter, ResultFormatter>();
        services.AddScoped<IResponseCalculationService, ResponseCalculationService>();
        services.AddScoped<IResultCalculationService, ResultCalculationService>();
        services.AddScoped<ICurveFitterApi, CurveFitterApi>();
        services.AddScoped<IResultExportService, ResultExportService>();
        services.AddScoped<IResultHub, ResultHub>();
        services.AddScoped<AnalyticalRunHub>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<ResultHub>(HubRoutes.Result);
        app.MapHub<AnalyticalRunHub>(HubRoutes.Run);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ResultDbContextInitializer>();
    }
}

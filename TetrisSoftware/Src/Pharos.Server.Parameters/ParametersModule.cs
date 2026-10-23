using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Queries;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Parameters.Application.Commands.ImportArticles;
using Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;
using Pharos.Server.Parameters.Application.Commands.ImportPanels;
using Pharos.Server.Parameters.Application.Commands.ImportVersion;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Parsers;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Parameters.Infrastructure.Database;
using Pharos.Server.Parameters.Infrastructure.Hubs;

namespace Pharos.Server.Parameters;

public class ParametersModule : IServerAppModule
{
    private ParametersModule()
    {
    }

    public static IServerAppModule Instance { get; } = new ParametersModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();


    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddSingleton<IMethodUpdatePackageFactory, MethodUpdatePackageFactory>();

        services.AddDbContext<ParametersDbContext>();
        services.AddScoped<IParametersDbContext>(provider => provider.GetRequiredService<ParametersDbContext>());
        services.AddScoped<ParametersDbContextInitializer>();

        services.AddScoped<IArticlesService, ArticlesService>();
        services.AddScoped<IMethodsService, MethodsService>();
        services.AddScoped<IExportParameterService, ExportParameterService>();
        services.AddScoped<IQualityControlService, QualityControlService>();
        services.AddScoped<IIsQualityControlNeededService, IsQualityControlNeededService>();

        services.AddSingleton<IParameterFileContentParser, ParameterFileContentParser>();
        services.AddSingleton<IParameterFileParser, ParameterFileParser>();
        services.AddTransient<UpdateMethodParameterHandler>();
        services.AddTransient<UpdatePanelParametersHandler>();
        services.AddTransient<UpdateArticleParameterHandler>();
        services.AddTransient<UpdateVersionCommandHandler>();

        services.AddTransient<ArticleParametersHandler>();
        services.AddTransient<PanelParametersHandler>();
        services.AddTransient<MethodParametersHandler>();
        services.AddTransient<MethodTestParametersHandler>();
        services.AddTransient<VersionParametersHandler>();

        services.AddSingleton<IAlphanumericStringComparerProvider, AlphanumericStringComparerProvider>();

        // Register the search provider for parameters
        services.AddScoped<ISearchProvider, ParametersSearch>();

        services.AddScoped<IDilutionValidator, DilutionValidator>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
        services.AddScoped<IBarcodeFormatHub, BarcodeFormatHub>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<BarcodeFormatHub>(HubRoutes.BarcodeFormat);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ParametersDbContextInitializer>();
    }
}

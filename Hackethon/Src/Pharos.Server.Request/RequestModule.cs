using System.Reflection;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Request.Application.Commands;
using Pharos.Server.Request.Application.Commands.RequestIssue;
using Pharos.Server.Request.Application.Handlers;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Mapping;
using Pharos.Server.Request.Application.Parsers;
using Pharos.Server.Request.Application.Services;
using Pharos.Server.Request.Infrastructure.Database;
using Pharos.Server.Request.Infrastructure.Hubs;

namespace Pharos.Server.Request;

public class RequestModule : IServerAppModule
{
    private RequestModule()
    {
    }

    public static IServerAppModule Instance { get; } = new RequestModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddSingleton<RequestMapper>();

        services.ConfigureSettings<RequestSettings>(configuration);
        services.ConfigureSettings<TubeTypesSettings>(configuration);

        services.AddDbContext<RequestDbContext>();
        services.AddScoped<IRequestDbContext>(provider => provider.GetRequiredService<RequestDbContext>());
        services.AddScoped<RequestDbContextInitializer>();

        services.AddScoped<IRequestsService, RequestsService>();
        services.AddScoped<IRequestImportService, RequestImportService>();
        services.AddTransient<IRequestImportHandler, RequestImportHandler>();
        services.AddTransient<IRequestImportFileContentParser, RequestImportFileContentParser>();
        services.AddTransient<ISaveImportedRequests, SaveImportedRequests>();
        services.AddScoped<ITestDisplayNameProvider, TestDisplayNameProvider>();
        services.AddTransient<IRequestPreProcessor<ProcessAssayResultCommand>, PreProcessAssayResultCommand>();
        services.AddScoped<IRequestGeneratorValidationService, RequestGeneratorValidationService>();
        services.AddScoped<NotReportedRequestIssueEvaluator>();

        services.AddSingleton<ISampleIdFormatter, SampleIdFormatter>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
        services.AddScoped<IRequestHub, RequestHub>();
        services.AddScoped<TubeDefinitionHub>();

        // Register the search provider for requests
        services.AddScoped<ISearchProvider, RequestSearch>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<RequestHub>(HubRoutes.Request);
        app.MapHub<TubeDefinitionHub>(HubRoutes.TubeDefinition);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<RequestDbContextInitializer>();
    }
}
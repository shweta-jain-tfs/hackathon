using FluentValidation;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pharos.Server.Helpers;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Logging;
using Pharos.Server.Infrastructure.Testing;

namespace Pharos.Server;

public static class Modules
{
    /// <summary>
    /// The global list of application modules. For correct application startup
    /// every module must be added to this collection.
    /// </summary>
    public static readonly IServerAppModule[] ModuleList =
    [
        AuditTrail.AuditTrailModule.Instance,
        Identity.IdentityModule.Instance,
        Instrument.InstrumentModule.Instance,
        LabCommunity.LabCommunityModule.Instance,
        Lis.LisModule.Instance,
        Lis.Astm.LisAstmModule.Instance,
        Lis.HL7.LisHL7Module.Instance,
        Maintenance.MaintenanceModule.Instance,
        Parameters.ParametersModule.Instance,
        Quality.QualityModule.Instance,
        Reagents.ReagentsModule.Instance,
        Reflex.ReflexModule.Instance,
        Request.RequestModule.Instance,
        Result.ResultModule.Instance,
        Result.Flr.ResultFlrModule.Instance,
        Sample.SampleModule.Instance,
        Settings.SettingsModule.Instance,
        SystemErrors.SystemErrorsModule.Instance,
        Search.SearchModule.Instance,
        TaskCard.TaskCardModule.Instance,
        LoadList.LoadListModule.Instance,
        SystemUpdate.SystemUpdateModule.Instance,
        LogDump.LogDumpModule.Instance
    ];

    public static IServiceCollection AddModuleServices(this IServiceCollection services, IConfiguration configuration, IMvcBuilder? mvcBuilder)
    {
        foreach (var module in ModuleList)
        {
            services.AddValidatorsFromAssembly(module.Assembly, includeInternalTypes: true);
        }

        services.AddMediatR(config =>
        {
            var assemblies = ModuleList.Select(m => m.Assembly).Append(typeof(Program).Assembly).ToArray();
            config.UseDelayedPublishing = true;
            config.RegisterServicesFromAssemblies(assemblies);
            config.NotificationHandlingStrategyType = typeof(LogNotificationHandlingStrategy);
        });

        // The order of the following pipeline behaviors is significant. They are executed in the order they are registered.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>), ServiceLifetime.Transient));

        services.AddMemoryCache();

        // configure services for individual application modules
        foreach (var module in ModuleList)
        {
            module.AddServices(services, configuration, mvcBuilder);
        }

        services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static async Task InitializeDatabase(this IServiceProvider services)
    {
        RecreateObjectsMode recreateMode;
        if (TestEnvironment.IsXUnitDetected())
        {
            // We always want to recreate database objects when running xUnit tests
            recreateMode = RecreateObjectsMode.MigrationsPending;
        }
        else
        {
            // TODO: change to RecreateObjectsMode.Never when Pharos gets under the change control and we start adding migrations to the baseline schema
            recreateMode = RecreateObjectsMode.MigrationsPendingForBaseline;
        }

        // Initialize the database schema
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Initializing the database");
        foreach (var module in ModuleList)
        {
            var initializer = module.GetDbContextInitializer(scope.ServiceProvider);
            if (initializer == null)
            {
                continue;
            }

            var moduleName = GetModuleName(module);
            logger.LogInformation("Initializing database schema for {Module} module.", moduleName);
            await initializer.InitializeAsync(recreateMode);
        }
        logger.LogInformation("Database initialization complete.");
    }

    public static async Task SeedDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        foreach (var module in ModuleList)
        {
            var initializer = module.GetDbContextInitializer(scope.ServiceProvider);
            if (initializer == null)
            {
                continue;
            }

            // xUnit tests have their own seeding
            if (!TestEnvironment.IsXUnitDetected())
            {
                var moduleName = GetModuleName(module);
                logger.LogInformation("Seeding database for {Module} module.", moduleName);
                await initializer.SeedAsync();
            }
        }
    }

    public static void UseModules(this WebApplication app, IEndpointConventionBuilder endpointConventionBuilder)
    {
        foreach (var module in ModuleList)
        {
            module.UseModule(app, endpointConventionBuilder);
        }
    }

    private static string? GetModuleName(IServerAppModule module)
    {
        var moduleName = module.Assembly.GetName().Name?.Replace("Pharos.Server.", string.Empty);
        return moduleName;
    }
}
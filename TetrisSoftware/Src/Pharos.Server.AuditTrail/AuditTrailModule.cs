using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Application.Services;
using Pharos.Server.AuditTrail.Infrastructure.Database;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.AuditTrail;

public class AuditTrailModule : IServerAppModule
{
    private AuditTrailModule()
    {

    }

    public static IServerAppModule Instance { get; } = new AuditTrailModule();
    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddDbContext<AuditTrailDbContext>(options =>
            options.UseSqlServer(
                configuration.GetAuditTrailConnectionString(),
                sqlOptions => sqlOptions.MigrationsAssembly(GetType().Assembly.FullName)
            ));
        services.AddScoped<IAuditTrailDbContext>(provider => provider.GetRequiredService<AuditTrailDbContext>());
        services.AddScoped<AuditTrailDbContextInitializer>();
        services.AddScoped<AuditDataProtectionService>();
        services.AddScoped<DescriptionTemplateService>();
        services.AddScoped<IAuditTrailCleanupService, AuditTrailCleanupService>();
        services.AddHostedService<AuditTrailCleanupBackgroundWorker>();

        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<AuditTrailDbContextInitializer>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {

    }
}

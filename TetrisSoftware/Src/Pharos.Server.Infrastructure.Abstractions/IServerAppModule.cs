using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Infrastructure;

/// <summary>
/// Represents an abstraction for the server-side application module.
/// </summary>
public interface IServerAppModule : IAppModule
{
    /// <summary>
    /// Registers module services and adds API controllers implemented by module (if any).
    /// </summary>
    void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder);

    /// <summary>
    /// Maps module endpoints (if they are provided) to the pipeline.
    /// </summary>
    void UseModule(IHost webApplication, object endpointConventionBuilder);

    /// <summary>
    /// Gets the instance of the database context initializer for this module (if the module has it).
    /// </summary>
    IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider);
}
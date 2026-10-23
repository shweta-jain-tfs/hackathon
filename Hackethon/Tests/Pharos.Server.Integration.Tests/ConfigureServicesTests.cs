using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Settings.General;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Integration.Tests;

public class ConfigureServicesTests
{
    private readonly ServiceCollection _services = new();

    [Fact]
    public void InternalCommandValidatorsAreRegistered()
    {
        using var configuration = new ConfigurationManager();
        var defaultSettings = DefaultSettingsProvider.GetDefaultSettings([typeof(GeneralSettings).Assembly]);
        configuration.AddInMemoryCollection(defaultSettings);
        _services.AddModuleServices(configuration, null);

        var validatorType = typeof(IValidator<>);

        var foundInternalValidator = false;

        foreach (var service in _services)
        {
            if (service.IsKeyedService)
            {
                continue;
            }
            var implementationType = service.ImplementationType;
            if (implementationType == null || implementationType.IsVisible)
            {
                continue;
            }
            if (service.ImplementationType?.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorType) != null)
            {
                foundInternalValidator = true;
            }
        }
        Assert.True(foundInternalValidator);
    }
}
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

internal sealed class EntityConfigurationSource(IEnumerable<Assembly> assemblies, Type stringLocalizerResourceType) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var defaultSettings = DefaultSettingsProvider.GetDefaultSettings(assemblies);

        EntityConfigurationProvider.StringLocalizerResourceType = stringLocalizerResourceType;
        return EntityConfigurationProvider.Create(defaultSettings);
    }
}

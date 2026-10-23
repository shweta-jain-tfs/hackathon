using Pharos.Server.Application.Settings;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Infrastructure.Tests;

public class VisibilityTests
{
    [Fact]
    public void AbstractionsProjectIsNotPolluted()
    {
        var abstractionsAssembly = typeof(IDateTime).Assembly;
        var types = abstractionsAssembly.GetTypes();
        foreach (var type in types)
        {
            var isAbstract = type.IsAbstract;
            var isInterface = type.IsInterface;
            var isEnum = type.IsEnum;
            var isOptionsClass = type.IsClass && type.IsSealed
                && (type.Name.EndsWith(SettingsServiceCollectionExtensions.OptionsSuffix)
                || type.Name.EndsWith(SettingsServiceCollectionExtensions.SettingsSuffix));
            var isNestedPrivate = type.IsNestedPrivate;

            Assert.True(isAbstract || isInterface || isEnum || isOptionsClass || isNestedPrivate,
                $"Type {type.FullName} cannot be put in {abstractionsAssembly.GetName().Name}. Only interfaces, enums, and abstract or options classes allowed here.");
        }
    }

    [Fact]
    public void ImplementationIsInternal()
    {
        var implementationAssembly = typeof(ConfigureServices).Assembly;
        var types = implementationAssembly.GetTypes();
        foreach (var type in types)
        {
            const string configureServices = nameof(ConfigureServices);

            Assert.False(type.IsVisible
                && type.Name != configureServices
                && type.Name != nameof(JobHub)
                && type.Name != nameof(HealthHub)
                && !(type.FullName?.Contains("Logging.") ?? false),
                $"Type {type.FullName} is public. It is not allowed in {implementationAssembly.GetName().Name}.");
        }
    }

    [Fact]
    public void HubsAreAbstract()
    {
        var hubsAssembly = typeof(HubBase<,>).Assembly;
        var types = hubsAssembly.GetTypes();
        foreach (var type in types)
        {
            var isAbstract = type.IsAbstract;
            var isInterface = type.IsInterface;
            var isEnum = type.IsEnum;
            var isNestedPrivate = type.IsNestedPrivate;

            Assert.True(isAbstract || isInterface || isEnum || isNestedPrivate,
                $"Type {type.FullName} cannot be put in {hubsAssembly.GetName().Name}. Only interfaces, enums, and abstract or options classes allowed here.");
        }
    }
}

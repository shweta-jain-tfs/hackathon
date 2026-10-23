using Microsoft.Extensions.DependencyInjection;

namespace Pharos.Client.App.Settings.Actions;

public class SettingsActionTests
{
    [Fact]
    public void AddServices_RegistersSettingsActions()
    {
        var services = new ServiceCollection();

        SettingsModule.Instance.AddServices(services);

        services
            .Where(descriptor => descriptor.ServiceType == typeof(ISettingsAction))
            .Select(descriptor => descriptor.ImplementationType)
            .Should()
            .BeEquivalentTo([typeof(ExportSettingsAction), typeof(ImportSettingsAction)]);
    }

    [Fact]
    public void OperationState_AllowsOnlyOneRunningAction()
    {
        var operationState = new SettingsActionOperationState();

        operationState.TryBegin().Should().BeTrue();
        operationState.TryBegin().Should().BeFalse();

        operationState.End();

        operationState.TryBegin().Should().BeTrue();
    }
}

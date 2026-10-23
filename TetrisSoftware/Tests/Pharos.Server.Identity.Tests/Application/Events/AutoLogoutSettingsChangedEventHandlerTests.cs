using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Infrastructure.Hubs;

namespace Pharos.Server.Identity.Application.Events;

public class AutoLogoutSettingsChangedEventHandlerTests
{
    [Theory]
    [InlineData(false, 10)]
    [InlineData(true, 20)]
    public async Task AutoLogoutChanged_NotifiesConnectedClients(bool enabled, int timeoutMinutes)
    {
        var hub = new Mock<ICurrentUserServerHub>();
        var sut = new AutoLogoutSettingsChangedEventHandler(hub.Object);
        var newSettings = new SecuritySettings
        {
            AutoLogoutSettings = new AutoLogoutSettings
            {
                AutoLogoutEnabled = enabled,
                AutoLogoutTimeoutMinutes = timeoutMinutes
            }
        };

        await sut.Handle(new SettingsChangedEvent<SecuritySettings>(new SecuritySettings(), newSettings),
            TestContext.Current.CancellationToken);

        hub.Verify(x => x.AutoLogoutSettingsChanged(), Times.Once);
    }

    [Fact]
    public async Task OtherSecuritySettingsChanged_DoesNotResetAutoLogout()
    {
        var hub = new Mock<ICurrentUserServerHub>();
        var sut = new AutoLogoutSettingsChangedEventHandler(hub.Object);
        var newSettings = new SecuritySettings { EnablePinLogin = true };

        await sut.Handle(new SettingsChangedEvent<SecuritySettings>(new SecuritySettings(), newSettings),
            TestContext.Current.CancellationToken);

        hub.Verify(x => x.AutoLogoutSettingsChanged(), Times.Never);
    }
}

using MediatR;
using Moq;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Events;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Events
{
    public static class LabCommunitySettingsChangedEventHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly Mock<ISender> _senderMock;
            private readonly LabCommunitySettingsChangedEventHandler _sut;
            private readonly Mock<ILabCommunitySchedulerService> _schedulerServiceMock;

            public TheHandleMethod()
            {
                _senderMock = new Mock<ISender>();
                var dbContextMock = new Mock<ILabCommunityDbContext>();
                dbContextMock.Setup(db => db.GetConfigurationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Configuration?)null);

                _schedulerServiceMock = new Mock<ILabCommunitySchedulerService>();

                _sut = new LabCommunitySettingsChangedEventHandler(
                    _senderMock.Object,
                    _schedulerServiceMock.Object);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Trait("ModuleRequirement", "MR_5791")]
            [Fact]
            public async Task SendsEnableCommand_WhenEnableChangedToTrue()
            {
                // Arrange
                var oldSettings = new LabCommunitySettings
                {
                    Enable = false
                };
                var newSettings = new LabCommunitySettings
                {
                    Enable = true
                };
                var notification = new SettingsChangedEvent<LabCommunitySettings>(oldSettings, newSettings);

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _senderMock.Verify(x => x.Send(
                    It.IsAny<EnableLabCommunityCommand>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5791")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task ConfiguresLabCommunityJobs_WhenEnableChangedToTrue_AndLabCommunityIsSuccessfullyEnabled()
            {
                // Arrange
                var oldSettings = new LabCommunitySettings
                {
                    Enable = false
                };
                var newSettings = new LabCommunitySettings
                {
                    Enable = true
                };
                var notification = new SettingsChangedEvent<LabCommunitySettings>(oldSettings, newSettings);

                _senderMock.Setup(m => m.Send(It.IsAny<EnableLabCommunityCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _schedulerServiceMock.Verify(s => s.ConfigureAllFromCurrentConfiguration(
                    newSettings.LaboratoryCode,
                    It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5791")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task StopsLabCommunityJobs_WhenEnableChangedToFalse()
            {
                // Arrange
                var oldSettings = new LabCommunitySettings
                {
                    Enable = true
                };
                var newSettings = new LabCommunitySettings
                {
                    Enable = false
                };
                var notification = new SettingsChangedEvent<LabCommunitySettings>(oldSettings, newSettings);

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _schedulerServiceMock.Verify(s => s.StopAll(It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5791")]
            [Fact]
            public async Task DoesNotSendEnableCommand_WhenEnableChangedToFalse()
            {
                // Arrange
                var oldSettings = new LabCommunitySettings
                {
                    Enable = true
                };
                var newSettings = new LabCommunitySettings
                {
                    Enable = false
                };
                var notification = new SettingsChangedEvent<LabCommunitySettings>(oldSettings, newSettings);

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _senderMock.Verify(x => x.Send(
                    It.IsAny<EnableLabCommunityCommand>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task SendsEnableCommand_WhenEnabledSettingsChanged()
            {
                // Arrange
                var oldSettings = new LabCommunitySettings
                {
                    Enable = true,
                    Username = "old-user"
                };
                var newSettings = new LabCommunitySettings
                {
                    Enable = true,
                    Username = "new-user"
                };
                var notification = new SettingsChangedEvent<LabCommunitySettings>(oldSettings, newSettings);

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _senderMock.Verify(x => x.Send(
                    It.IsAny<EnableLabCommunityCommand>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5791")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task ConfiguresAllJobs_WhenLabCommunityEnabledSuccessfully()
            {
                // Arrange
                var newSettings = new LabCommunitySettings
                {
                    Enable = true
                };

                _senderMock.Setup(m => m.Send(It.IsAny<EnableLabCommunityCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

                var notification = new SettingsChangedEvent<LabCommunitySettings>(new LabCommunitySettings(), newSettings);

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _schedulerServiceMock.Verify(s => s.ConfigureAllFromCurrentConfiguration(
                    newSettings.LaboratoryCode, It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task ConfiguresAllJobs_WhenAnyLabCommunitySettingChanged_AndLabCommunityEnabledSuccessfully()
            {
                // Arrange
                var oldSettings = new LabCommunitySettings
                {
                    Enable = true,
                    Username = "old-user"
                };
                var newSettings = new LabCommunitySettings
                {
                    Enable = true,
                    Username = "new-user"
                };
                _senderMock.Setup(m => m.Send(It.IsAny<EnableLabCommunityCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
                var notification = new SettingsChangedEvent<LabCommunitySettings>(oldSettings, newSettings);

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _schedulerServiceMock.Verify(s => s.ConfigureAllFromCurrentConfiguration(
                    newSettings.LaboratoryCode, It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Trait("ModuleRequirement", "MR_5791")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task StopsAllJobs_WhenLabCommunityEnablementFailed()
            {
                // Arrange
                var newSettings = new LabCommunitySettings
                {
                    Enable = true
                };

                _senderMock.Setup(m => m.Send(It.IsAny<EnableLabCommunityCommand>())).ReturnsAsync(false);

                var notification = new SettingsChangedEvent<LabCommunitySettings>(new LabCommunitySettings(), newSettings);

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _schedulerServiceMock.Verify(s => s.StopAll(It.IsAny<CancellationToken>()), Times.Once);
            }
        }
    }
}
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Events;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Events
{
    public static class ApplicationStartedEventHandlerTests
    {
        public class TheHandleMethod
        {
            private const string _laboratoryCode = "123456";

            private readonly ApplicationStartedEventHandler _sut;
            private readonly Mock<ILabCommunitySchedulerService> _schedulerService;
            private readonly Mock<IOptionsSnapshot<LabCommunitySettings>> _settingsSnapshotMock;

            public TheHandleMethod()
            {
                _settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();

                var dbContextMock = new Mock<ILabCommunityDbContext>();
                dbContextMock.Setup(db => db.GetConfigurationAsync(_laboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Configuration?)null);
                _schedulerService = new Mock<ILabCommunitySchedulerService>();

                _sut = new ApplicationStartedEventHandler(
                    _settingsSnapshotMock.Object,
                    _schedulerService.Object);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5791")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task ConfiguresLabCommunityJobs_WhenLabCommunityEnabled()
            {
                // Arrange
                var settings = new LabCommunitySettings { Enable = true, LaboratoryCode = _laboratoryCode };
                _settingsSnapshotMock.Setup(s => s.Value).Returns(settings);

                // Act
                await _sut.Handle(new ApplicationStartedEvent(), TestContext.Current.CancellationToken);

                // Assert
                _schedulerService.Verify(m => m.ConfigureAllFromCurrentConfiguration(_laboratoryCode, It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5791")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task DoesNotConfigureLabCommunityJobs_WhenLabCommunityDisabled()
            {
                // Arrange
                var settings = new LabCommunitySettings { Enable = false, LaboratoryCode = _laboratoryCode };
                _settingsSnapshotMock.Setup(s => s.Value).Returns(settings);

                // Act
                await _sut.Handle(new ApplicationStartedEvent(), TestContext.Current.CancellationToken);

                // Assert
                _schedulerService.Verify(m => m.ConfigureAllFromCurrentConfiguration(_laboratoryCode, It.IsAny<CancellationToken>()), Times.Never);
            }
        }
    }
}
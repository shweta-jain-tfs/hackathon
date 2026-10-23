using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.CheckIn;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Commands
{
    public static class CheckInToLabCommunityCommandHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly Mock<ILabCommunityCheckInService> _checkInServiceMock;
            private readonly Mock<ILabCommunityCheckInState> _checkInStateMock;
            private readonly Mock<ILabCommunityConnectionHub> _connectionHubMock;
            private readonly CheckInToLabCommunityCommandHandler _sut;
            private readonly LabCommunitySettings _settings;
            private readonly DateTimeOffset _now;

            public TheHandleMethod()
            {
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                _checkInServiceMock = new Mock<ILabCommunityCheckInService>();
                _checkInStateMock = new Mock<ILabCommunityCheckInState>();
                _connectionHubMock = new Mock<ILabCommunityConnectionHub>();
                _connectionHubMock.Setup(h => h.LabCommunityStatusChanged()).Returns(Task.CompletedTask);
                var settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();

                var dateTimeMock = new Mock<IDateTime>();
                _now = new DateTimeOffset(2026, 5, 5, 12, 0, 0, TimeSpan.Zero);
                dateTimeMock.Setup(d => d.UtcNow).Returns(_now);

                var loggerMock = new Mock<ILogger<CheckInToLabCommunityCommandHandler>>();

                _settings = new LabCommunitySettings
                {
                    LaboratoryCode = "123456"
                };

                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Enrollment());
                _checkInServiceMock.Setup(s => s.CheckIn(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

                settingsSnapshotMock.Setup(s => s.Value).Returns(_settings);

                _sut = new CheckInToLabCommunityCommandHandler(
                    _dbContextMock.Object,
                    _checkInServiceMock.Object,
                    _checkInStateMock.Object,
                    settingsSnapshotMock.Object,
                    dateTimeMock.Object,
                    _connectionHubMock.Object,
                    loggerMock.Object);
            }

            [Fact]
            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5825")]
            public async Task SkipsCheckIn_WhenNoActiveEnrollmentExists()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);

                // Act
                await _sut.Handle(new CheckInToLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _checkInServiceMock.Verify(
                    s => s.CheckIn(It.IsAny<CancellationToken>()),
                    Times.Never);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public async Task MarksFailedAndNotifiesClient_WhenNoActiveEnrollmentExists()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);

                // Act
                await _sut.Handle(new CheckInToLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _checkInStateMock.Verify(s => s.MarkFailed(_now), Times.Once);
                _connectionHubMock.Verify(h => h.LabCommunityStatusChanged(), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public async Task SendsCheckIn_WhenActiveEnrollmentExists()
            {
                // Act
                await _sut.Handle(new CheckInToLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _checkInServiceMock.Verify(s => s.CheckIn(It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public async Task MarksSuccessfulAndNotifiesClient_WhenActiveEnrollmentExists()
            {
                // Act
                await _sut.Handle(new CheckInToLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _checkInStateMock.Verify(s => s.MarkSucceeded(_now), Times.Once);
                _connectionHubMock.Verify(h => h.LabCommunityStatusChanged(), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public async Task MarksFailedAndNotifiesClient_WhenCheckInFails()
            {
                // Arrange
                _checkInServiceMock.Setup(s => s.CheckIn(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

                // Act
                await _sut.Handle(new CheckInToLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _checkInStateMock.Verify(s => s.MarkFailed(_now), Times.Once);
                _connectionHubMock.Verify(h => h.LabCommunityStatusChanged(), Times.Once);
            }
        }
    }
}
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.CheckIn;
using Pharos.Server.LabCommunity.Application.Services;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class LabCommunityConnectionStatusServiceTests
    {
        public class TheIsConnectedMethod
        {
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly Mock<ILabCommunityCheckInState> _checkInStateMock;
            private readonly LabCommunityConnectionStatusService _sut;
            private readonly LabCommunitySettings _settings;

            public TheIsConnectedMethod()
            {
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                _checkInStateMock = new Mock<ILabCommunityCheckInState>();
                var settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();
                var checkInOptions = Options.Create(new LabCommunityDefaultConfiguration { CheckInIntervalSeconds = 120 });
                var dateTimeMock = new Mock<IDateTime>();

                _settings = new LabCommunitySettings
                {
                    Enable = true,
                    LaboratoryCode = "123456"
                };

                settingsSnapshotMock.Setup(s => s.Value).Returns(_settings);

                _sut = new LabCommunityConnectionStatusService(
                    _dbContextMock.Object,
                    settingsSnapshotMock.Object,
                    checkInOptions,
                    _checkInStateMock.Object,
                    dateTimeMock.Object);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Fact]
            public async Task ReturnsFalse_WhenDisabled()
            {
                // Arrange
                _settings.Enable = false;

                // Act
                var result = await _sut.IsConnected(TestContext.Current.CancellationToken);

                // Assert
                Assert.False(result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Fact]
            public async Task ReturnsFalse_WhenNoActiveEnrollment()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);

                // Act
                var result = await _sut.IsConnected(TestContext.Current.CancellationToken);

                // Assert
                Assert.False(result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Fact]
            public async Task ReturnsFalse_WhenNoSuccessfulCheckIn()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Enrollment());

                // Act
                var result = await _sut.IsConnected(TestContext.Current.CancellationToken);

                // Assert
                Assert.False(result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Fact]
            public async Task ReturnsFalse_WhenActiveEnrollmentHasInvalidCredentials()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Enrollment { ValidCredentials = false });

                _checkInStateMock.Setup(s => s.HasRecentSuccessfulCheckIn(It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()))
                    .Returns(true);

                // Act
                var result = await _sut.IsConnected(TestContext.Current.CancellationToken);

                // Assert
                Assert.False(result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Fact]
            public async Task ReturnsTrue_WhenActiveEnrollmentAndRecentSuccessfulCheckInExist()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Enrollment());

                _checkInStateMock.Setup(s => s.HasRecentSuccessfulCheckIn(It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()))
                    .Returns(true);

                // Act
                var result = await _sut.IsConnected(TestContext.Current.CancellationToken);

                // Assert
                Assert.True(result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public async Task UsesLatestConfiguredCheckInInterval_WhenAvailable()
            {
                // Arrange
                TimeSpan? observedCheckInInterval = null;

                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Enrollment());

                _dbContextMock.Setup(db => db.GetConfigurationAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Configuration
                    {
                        LaboratoryCode = _settings.LaboratoryCode,
                        CheckInIntervalSeconds = 300,
                        SystemStatusEnabled = true,
                        SystemStatusIntervalSeconds = 600
                    });

                _checkInStateMock.Setup(s => s.HasRecentSuccessfulCheckIn(It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()))
                    .Callback<DateTimeOffset, TimeSpan>((_, checkInInterval) => observedCheckInInterval = checkInInterval)
                    .Returns(true);

                // Act
                var result = await _sut.IsConnected(TestContext.Current.CancellationToken);

                // Assert
                Assert.True(result);
                Assert.Equal(TimeSpan.FromSeconds(300), observedCheckInInterval);
            }
        }
    }
}
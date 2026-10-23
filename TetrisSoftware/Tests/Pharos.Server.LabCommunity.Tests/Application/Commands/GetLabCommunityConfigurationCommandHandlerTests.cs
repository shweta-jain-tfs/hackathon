using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.GetConfiguration;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Commands
{
    public static class GetLabCommunityConfigurationCommandHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly Mock<ILabCommunityConfigurationService> _configurationServiceMock;
            private readonly Mock<DbSet<Configuration>> _configurationsMock;
            private readonly GetLabCommunityConfigurationCommandHandler _sut;
            private readonly LabCommunitySettings _settings;
            private readonly LabCommunityConfiguration _pulledConfiguration;
            private readonly Mock<ILabCommunitySchedulerService> _schedulerService;

            public TheHandleMethod()
            {
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                _configurationServiceMock = new Mock<ILabCommunityConfigurationService>();
                _configurationsMock = new Mock<DbSet<Configuration>>();
                var settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();

                _settings = new LabCommunitySettings
                {
                    LaboratoryCode = "123456"
                };

                settingsSnapshotMock.Setup(s => s.Value).Returns(_settings);
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Enrollment());
                _dbContextMock.Setup(db => db.Configurations)
                    .Returns(_configurationsMock.Object);
                _dbContextMock.Setup(db => db.GetConfigurationAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Configuration?)null);

                _pulledConfiguration = new LabCommunityConfiguration(
                    TimeSpan.FromSeconds(180),
                    true,
                    TimeSpan.FromSeconds(600),
                    true,
                    true);
                _configurationServiceMock.Setup(s => s.GetConfiguration(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_pulledConfiguration);
                _schedulerService = new Mock<ILabCommunitySchedulerService>();

                _sut = new GetLabCommunityConfigurationCommandHandler(
                    _dbContextMock.Object,
                    _configurationServiceMock.Object,
                    _schedulerService.Object,
                    settingsSnapshotMock.Object,
                    Mock.Of<ILogger<GetLabCommunityConfigurationCommandHandler>>());
            }

            private bool MatchesPulledConfiguration(Configuration? configuration)
            {
                return configuration is not null
                    && configuration.CheckInIntervalSeconds == (int)_pulledConfiguration.CheckInInterval.TotalSeconds
                    && configuration.SystemStatusEnabled == _pulledConfiguration.SystemStatusEnabled
                    && configuration.SystemStatusIntervalSeconds == (int)_pulledConfiguration.SystemStatusInterval.TotalSeconds
                    && configuration.ReportInstrumentErrorsEnabled == _pulledConfiguration.ReportInstrumentErrorsEnabled
                    && configuration.ReportCalibrationAndQualityResultsEnabled == _pulledConfiguration.ReportCalibrationAndQualityResultsEnabled;
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task PersistsPulledConfiguration_WhenNoConfigurationExistsYet()
            {
                // Arrange
                Configuration? persistedConfiguration = null;
                _configurationsMock.Setup(s => s.Add(It.IsAny<Configuration>()))
                    .Callback<Configuration>(configuration => persistedConfiguration = configuration);

                // Act
                await _sut.Handle(new GetLabCommunityConfigurationCommand(), TestContext.Current.CancellationToken);

                // Assert
                Assert.NotNull(persistedConfiguration);
                Assert.Equal(_settings.LaboratoryCode, persistedConfiguration.LaboratoryCode);
                Assert.Equal(_pulledConfiguration.CheckInInterval.TotalSeconds, persistedConfiguration.CheckInIntervalSeconds);
                Assert.True(persistedConfiguration.SystemStatusEnabled);
                Assert.Equal(_pulledConfiguration.SystemStatusInterval.TotalSeconds, persistedConfiguration.SystemStatusIntervalSeconds);
                Assert.True(persistedConfiguration.ReportInstrumentErrorsEnabled);
                Assert.True(persistedConfiguration.ReportCalibrationAndQualityResultsEnabled);
                _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task PersistsPulledConfiguration_WhenConfigurationAlreadyExists()
            {
                // Arrange
                var persistedConfiguration = new Configuration
                {
                    LaboratoryCode = _settings.LaboratoryCode,
                    CheckInIntervalSeconds = 120,
                    SystemStatusEnabled = false,
                    SystemStatusIntervalSeconds = 300,
                    ReportInstrumentErrorsEnabled = false
                };
                _dbContextMock.Setup(db => db.GetConfigurationAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(persistedConfiguration);

                // Act
                await _sut.Handle(new GetLabCommunityConfigurationCommand(), TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(_pulledConfiguration.CheckInInterval.TotalSeconds, persistedConfiguration.CheckInIntervalSeconds);
                Assert.True(persistedConfiguration.SystemStatusEnabled);
                Assert.Equal(_pulledConfiguration.SystemStatusInterval.TotalSeconds, persistedConfiguration.SystemStatusIntervalSeconds);
                Assert.True(persistedConfiguration.ReportInstrumentErrorsEnabled);
                Assert.True(persistedConfiguration.ReportCalibrationAndQualityResultsEnabled);
                _configurationsMock.Verify(s => s.Add(It.IsAny<Configuration>()), Times.Never);
                _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task ReconfiguresWithPulledConfigurationAndNoPreviousConfiguration_WhenNoConfigurationExistsYet()
            {
                // Act
                await _sut.Handle(new GetLabCommunityConfigurationCommand(), TestContext.Current.CancellationToken);

                // Assert - "new" carries the pulled values; "previous" is null because nothing was persisted before
                _schedulerService.Verify(s => s.ReconfigureAfterConfigurationPull(
                    It.Is<Configuration?>(newConfiguration => MatchesPulledConfiguration(newConfiguration)),
                    null,
                    It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task ReconfiguresWithPulledConfigurationAndPreviousConfiguration_WhenConfigurationAlreadyExists()
            {
                // Arrange - persisted values differ from the pulled ones so the pre-pull snapshot is distinguishable
                _dbContextMock.Setup(db => db.GetConfigurationAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Configuration
                    {
                        LaboratoryCode = _settings.LaboratoryCode,
                        CheckInIntervalSeconds = 120,
                        SystemStatusEnabled = false,
                        SystemStatusIntervalSeconds = 300,
                        ReportInstrumentErrorsEnabled = false
                    });

                // Act
                await _sut.Handle(new GetLabCommunityConfigurationCommand(), TestContext.Current.CancellationToken);

                // Assert - "new" carries the pulled values; "previous" is the pre-pull snapshot, not the updated record
                _schedulerService.Verify(s => s.ReconfigureAfterConfigurationPull(
                    It.Is<Configuration?>(newConfiguration => MatchesPulledConfiguration(newConfiguration)),
                    It.Is<Configuration?>(previousConfiguration =>
                        previousConfiguration != null
                        && previousConfiguration.CheckInIntervalSeconds == 120
                        && !previousConfiguration.SystemStatusEnabled
                        && previousConfiguration.SystemStatusIntervalSeconds == 300
                        && !previousConfiguration.ReportInstrumentErrorsEnabled),
                    It.IsAny<CancellationToken>()), Times.Once);
            }

            [Fact]
            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            public async Task SkipsConfigurationPull_WhenNoActiveEnrollmentExists()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);

                // Act
                await _sut.Handle(new GetLabCommunityConfigurationCommand(), TestContext.Current.CancellationToken);

                // Assert
                _configurationServiceMock.Verify(
                    s => s.GetConfiguration(It.IsAny<CancellationToken>()),
                    Times.Never);
                _schedulerService.Verify(
                    s => s.ReconfigureAfterConfigurationPull(
                        It.IsAny<Configuration?>(),
                        It.IsAny<Configuration?>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task SkipsPersistenceAndReschedule_WhenConfigurationPullFails()
            {
                // Arrange
                _configurationServiceMock.Setup(s => s.GetConfiguration(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((LabCommunityConfiguration?)null);

                // Act
                await _sut.Handle(new GetLabCommunityConfigurationCommand(), TestContext.Current.CancellationToken);

                // Assert
                _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
                _schedulerService.Verify(
                    s => s.ReconfigureAfterConfigurationPull(
                        It.IsAny<Configuration?>(),
                        It.IsAny<Configuration?>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }
    }
}
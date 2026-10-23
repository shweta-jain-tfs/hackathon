using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.LabCommunity.Application;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Services;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class LabCommunitySchedulerServiceTests
    {
        public class TheConfigureAllFromCurrentConfigurationMethod
        {
            private const string _laboratoryCode = "123456";
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly Mock<ILabCommunityJobScheduler> _schedulerMock;
            private readonly LabCommunitySchedulerService _sut;

            public TheConfigureAllFromCurrentConfigurationMethod()
            {
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                _schedulerMock = new Mock<ILabCommunityJobScheduler>();
                _schedulerMock.Setup(s => s.ShouldStop(It.IsAny<LabCommunityDefaultConfiguration>(), It.IsAny<Configuration?>())).Returns(false);
                _schedulerMock.Setup(s => s.ResolveInterval(It.IsAny<LabCommunityDefaultConfiguration>(), It.IsAny<Configuration?>()))
                    .Returns(TimeSpan.FromSeconds(42));

                _sut = new LabCommunitySchedulerService(
                    Options.Create(new LabCommunityDefaultConfiguration
                    {
                        CheckInIntervalSeconds = 120,
                        ReportInstrumentErrorsIntervalSeconds = 30
                    }),
                    _dbContextMock.Object,
                    new[] { _schedulerMock.Object });
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task ConfiguresAllSchedulersWithDefaultConfiguration_WhenNoPersistedConfigurationExists()
            {
                // Arrange
                LabCommunityDefaultConfiguration? defaultConfiguration = null;
                Configuration? configuration = null;
                _dbContextMock.Setup(db => db.GetConfigurationAsync(_laboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Configuration?)null);
                _schedulerMock.Setup(s => s.ResolveInterval(
                        It.IsAny<LabCommunityDefaultConfiguration>(),
                        It.IsAny<Configuration?>()))
                    .Callback<LabCommunityDefaultConfiguration, Configuration?>((d, c) =>
                    {
                        defaultConfiguration = d;
                        configuration = c;
                    })
                    .Returns(TimeSpan.FromSeconds(42));

                // Act
                await _sut.ConfigureAllFromCurrentConfiguration(_laboratoryCode, TestContext.Current.CancellationToken);

                // Assert
                Assert.NotNull(defaultConfiguration);
                Assert.Equal(TimeSpan.FromSeconds(120), defaultConfiguration.CheckInInterval);
                Assert.Equal(TimeSpan.FromSeconds(30), defaultConfiguration.InstrumentErrorExportInterval);
                Assert.Null(configuration);
                _schedulerMock.Verify(s => s.ConfigureScheduler(TimeSpan.FromSeconds(42), It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ProductRequirement", "PR_538")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Fact]
            public async Task ConfiguresAllSchedulersWithPersistedConfiguration_WhenAvailable()
            {
                // Arrange
                var persistedConfiguration = new Configuration
                {
                    LaboratoryCode = _laboratoryCode,
                    CheckInIntervalSeconds = 300,
                    SystemStatusEnabled = true,
                    SystemStatusIntervalSeconds = 600
                };
                Configuration? configuration = null;

                _dbContextMock.Setup(db => db.GetConfigurationAsync(_laboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(persistedConfiguration);
                _schedulerMock.Setup(s => s.ResolveInterval(
                        It.IsAny<LabCommunityDefaultConfiguration>(),
                        It.IsAny<Configuration?>()))
                    .Callback<LabCommunityDefaultConfiguration, Configuration?>((_, c) => configuration = c)
                    .Returns(TimeSpan.FromSeconds(42));

                // Act
                await _sut.ConfigureAllFromCurrentConfiguration(_laboratoryCode, TestContext.Current.CancellationToken);

                // Assert
                Assert.Same(persistedConfiguration, configuration);
                _schedulerMock.Verify(s => s.ConfigureScheduler(TimeSpan.FromSeconds(42), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        public class TheReconfigureAfterConfigurationPullMethod
        {
            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task ReconfiguresOnlySchedulersWhoseConfigurationChanged()
            {
                // Arrange
                var newConfiguration = new Configuration { CheckInIntervalSeconds = 180 };
                var previousConfiguration = new Configuration { CheckInIntervalSeconds = 120 };
                var reconfiguredSchedulerMock = new Mock<ILabCommunityJobScheduler>();
                var skippedSchedulerMock = new Mock<ILabCommunityJobScheduler>();

                reconfiguredSchedulerMock.Setup(s => s.ShouldReconfigureAfterConfigurationPull(It.IsAny<Configuration?>(), It.IsAny<Configuration?>())).Returns(true);
                skippedSchedulerMock.Setup(s => s.ShouldReconfigureAfterConfigurationPull(It.IsAny<Configuration?>(), It.IsAny<Configuration?>())).Returns(false);
                reconfiguredSchedulerMock.Setup(s => s.ShouldStop(It.IsAny<LabCommunityDefaultConfiguration>(), It.IsAny<Configuration?>())).Returns(false);
                skippedSchedulerMock.Setup(s => s.ShouldStop(It.IsAny<LabCommunityDefaultConfiguration>(), It.IsAny<Configuration?>())).Returns(false);
                reconfiguredSchedulerMock.Setup(s => s.ResolveInterval(It.IsAny<LabCommunityDefaultConfiguration>(), It.IsAny<Configuration?>()))
                    .Returns(TimeSpan.FromSeconds(180));

                var sut = new LabCommunitySchedulerService(
                    Options.Create(new LabCommunityDefaultConfiguration()),
                    Mock.Of<ILabCommunityDbContext>(),
                    new[] { reconfiguredSchedulerMock.Object, skippedSchedulerMock.Object });

                // Act
                await sut.ReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration, TestContext.Current.CancellationToken);

                // Assert
                reconfiguredSchedulerMock.Verify(s => s.ConfigureScheduler(
                    TimeSpan.FromSeconds(180),
                    It.IsAny<CancellationToken>(),
                    TimeSpan.FromSeconds(180)), Times.Once);
                skippedSchedulerMock.Verify(s => s.ConfigureScheduler(
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan>()), Times.Never);
                skippedSchedulerMock.Verify(s => s.ShouldStop(It.IsAny<LabCommunityDefaultConfiguration>(), It.IsAny<Configuration?>()), Times.Never);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task ProvidesNewAndPreviousConfigurationToSchedulers()
            {
                // Arrange
                var newConfiguration = new Configuration { CheckInIntervalSeconds = 180 };
                var previousConfiguration = new Configuration { CheckInIntervalSeconds = 120 };
                Configuration? reconfigurationNewConfiguration = null;
                Configuration? reconfigurationPreviousConfiguration = null;
                Configuration? resolvedConfiguration = null;
                var schedulerMock = new Mock<ILabCommunityJobScheduler>();

                schedulerMock.Setup(s => s.ShouldReconfigureAfterConfigurationPull(It.IsAny<Configuration?>(), It.IsAny<Configuration?>()))
                    .Callback<Configuration?, Configuration?>((n, p) =>
                    {
                        reconfigurationNewConfiguration = n;
                        reconfigurationPreviousConfiguration = p;
                    })
                    .Returns(true);
                schedulerMock.Setup(s => s.ShouldStop(It.IsAny<LabCommunityDefaultConfiguration>(), It.IsAny<Configuration?>())).Returns(false);
                schedulerMock.Setup(s => s.ResolveInterval(It.IsAny<LabCommunityDefaultConfiguration>(), It.IsAny<Configuration?>()))
                    .Callback<LabCommunityDefaultConfiguration, Configuration?>((_, c) => resolvedConfiguration = c)
                    .Returns(TimeSpan.FromSeconds(180));

                var sut = new LabCommunitySchedulerService(
                    Options.Create(new LabCommunityDefaultConfiguration()),
                    Mock.Of<ILabCommunityDbContext>(),
                    new[] { schedulerMock.Object });

                // Act
                await sut.ReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration, TestContext.Current.CancellationToken);

                // Assert
                Assert.Same(newConfiguration, reconfigurationNewConfiguration);
                Assert.Same(previousConfiguration, reconfigurationPreviousConfiguration);
                Assert.Same(newConfiguration, resolvedConfiguration);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ProductRequirement", "PR_538")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Fact]
            public async Task StopsReconfigurableScheduler_WhenSchedulerShouldStopAfterConfigurationPull()
            {
                // Arrange
                var newConfiguration = new Configuration { SystemStatusEnabled = false };
                var previousConfiguration = new Configuration { SystemStatusEnabled = true };
                var schedulerMock = new Mock<ILabCommunityJobScheduler>();

                schedulerMock.Setup(s => s.ShouldReconfigureAfterConfigurationPull(It.IsAny<Configuration?>(), It.IsAny<Configuration?>())).Returns(true);
                schedulerMock.Setup(s => s.ShouldStop(It.IsAny<LabCommunityDefaultConfiguration>(), It.IsAny<Configuration?>())).Returns(true);

                var sut = new LabCommunitySchedulerService(
                    Options.Create(new LabCommunityDefaultConfiguration()),
                    Mock.Of<ILabCommunityDbContext>(),
                    new[] { schedulerMock.Object });

                // Act
                await sut.ReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration, TestContext.Current.CancellationToken);

                // Assert
                schedulerMock.Verify(s => s.StopScheduler(It.IsAny<CancellationToken>()), Times.Once);
                schedulerMock.Verify(s => s.ConfigureScheduler(
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan>()), Times.Never);
            }
        }

        public class TheStopAllMethod
        {
            [Fact]
            public async Task StopsAllSchedulers()
            {
                // Arrange
                var firstSchedulerMock = new Mock<ILabCommunityJobScheduler>();
                var secondSchedulerMock = new Mock<ILabCommunityJobScheduler>();
                var sut = new LabCommunitySchedulerService(
                    Options.Create(new LabCommunityDefaultConfiguration()),
                    Mock.Of<ILabCommunityDbContext>(),
                    new[] { firstSchedulerMock.Object, secondSchedulerMock.Object });

                // Act
                await sut.StopAll(TestContext.Current.CancellationToken);

                // Assert
                firstSchedulerMock.Verify(s => s.StopScheduler(It.IsAny<CancellationToken>()), Times.Once);
                secondSchedulerMock.Verify(s => s.StopScheduler(It.IsAny<CancellationToken>()), Times.Once);
            }
        }
    }
}
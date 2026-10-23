using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pharos.Server.LabCommunity.Application;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Services;
using Pharos.Server.LabCommunity.Domain;
using Quartz;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services;

public static class LabCommunityJobSchedulerTests
{
    public class TheShouldReconfigureAfterConfigurationPullMethod
    {
        [Fact]
        public void ReturnsJobReconfigurationDecision()
        {
            // Arrange
            TestLabCommunityJob.SetShouldReconfigureAfterConfigurationPull((_, _) => true);
            var sut = CreateSut();
            var newConfiguration = new Configuration();
            var previousConfiguration = new Configuration();

            try
            {
                // Act
                var result = sut.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);

                // Assert
                Assert.True(result);
            }
            finally
            {
                TestLabCommunityJob.ResetShouldReconfigureAfterConfigurationPull();
            }
        }
    }

    public class TheResolveIntervalMethod
    {
        [Fact]
        public void ResolvesIntervalFromJob()
        {
            // Arrange
            var sut = CreateSut();
            var defaultConfiguration = new LabCommunityDefaultConfiguration();

            // Act
            var result = sut.ResolveInterval(defaultConfiguration, configuration: null);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(42), result);
        }

        [Fact]
        public void FallsBackToDefaultInterval_WhenResolvedIntervalIsZero()
        {
            // Arrange
            var capturedConfigurations = new List<Configuration?>();
            TestLabCommunityJob.SetResolveInterval((_, configuration) =>
            {
                capturedConfigurations.Add(configuration);
                return TimeSpan.Zero;
            });

            var sut = CreateSut();
            var defaultConfiguration = new LabCommunityDefaultConfiguration();
            var configuration = new Configuration();

            // Act
            sut.ResolveInterval(defaultConfiguration, configuration);

            // Assert - second call should use default configuration only
            Assert.Equal(2, capturedConfigurations.Count);
            Assert.Null(capturedConfigurations[1]);

            // Cleanup
            TestLabCommunityJob.ResetResolveInterval();
        }
    }

    public class TheConfigureSchedulerMethod
    {
        [Trait("ProductRequirement", "PR_533")]
        [Trait("ProductRequirement", "PR_538")]
        [Trait("ModuleRequirement", "MR_5825")]
        [Trait("ModuleRequirement", "MR_5989")]
        [Trait("ModuleRequirement", "MR_6001")]
        [Fact]
        public async Task ConfiguresQuartzJobFromLabCommunityJobMetadata()
        {
            // Arrange
            var schedulerMock = new Mock<IScheduler>();
            var schedulerFactoryMock = new Mock<ISchedulerFactory>();
            var interval = TimeSpan.FromSeconds(42);

            schedulerFactoryMock
                .Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedulerMock.Object);
            schedulerMock
                .Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTimeOffset.UtcNow);

            var sut = CreateSut(schedulerFactoryMock);

            // Act
            await sut.ConfigureScheduler(interval, TestContext.Current.CancellationToken);

            // Assert
            schedulerMock.Verify(s => s.DeleteJob(TestLabCommunityJob.Key, It.IsAny<CancellationToken>()), Times.Once);
            schedulerMock.Verify(s => s.ScheduleJob(
                It.Is<IJobDetail>(job =>
                    job.Key.Equals(TestLabCommunityJob.Key) &&
                    job.JobType == typeof(TestLabCommunityJob)),
                It.Is<ITrigger>(trigger => MatchesTestJobTrigger(trigger, interval)),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AppliesInitialDelay_WhenProvided()
        {
            // Arrange
            var schedulerMock = new Mock<IScheduler>();
            var schedulerFactoryMock = new Mock<ISchedulerFactory>();

            schedulerFactoryMock
                .Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedulerMock.Object);
            schedulerMock
                .Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTimeOffset.UtcNow);

            var sut = CreateSut(schedulerFactoryMock);

            // Act
            await sut.ConfigureScheduler(
                TimeSpan.FromSeconds(42),
                TestContext.Current.CancellationToken,
                TimeSpan.FromMinutes(5));

            // Assert
            schedulerMock.Verify(s => s.ScheduleJob(
                It.IsAny<IJobDetail>(),
                It.Is<ITrigger>(trigger => trigger.StartTimeUtc > DateTimeOffset.UtcNow),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task DoesNotScheduleJob_WhenIntervalIsZeroOrNegative(int seconds)
        {
            // Arrange
            var schedulerMock = new Mock<IScheduler>();
            var schedulerFactoryMock = new Mock<ISchedulerFactory>();

            schedulerFactoryMock
                .Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedulerMock.Object);

            var sut = CreateSut(schedulerFactoryMock);

            // Act
            await sut.ConfigureScheduler(TimeSpan.FromSeconds(seconds), TestContext.Current.CancellationToken);

            // Assert
            schedulerMock.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
            schedulerMock.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class TheStopSchedulerMethod
    {
        [Fact]
        public async Task DeletesQuartzJobFromLabCommunityJobMetadata()
        {
            // Arrange
            var schedulerMock = new Mock<IScheduler>();
            var schedulerFactoryMock = new Mock<ISchedulerFactory>();

            schedulerFactoryMock
                .Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedulerMock.Object);

            var sut = CreateSut(schedulerFactoryMock);

            // Act
            await sut.StopScheduler(TestContext.Current.CancellationToken);

            // Assert
            schedulerMock.Verify(s => s.DeleteJob(TestLabCommunityJob.Key, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    private static LabCommunityJobScheduler<TestLabCommunityJob> CreateSut(
        Mock<ISchedulerFactory>? schedulerFactoryMock = null)
    {
        schedulerFactoryMock ??= new Mock<ISchedulerFactory>();

        return new LabCommunityJobScheduler<TestLabCommunityJob>(
            schedulerFactoryMock.Object,
            NullLogger<LabCommunityJobScheduler<TestLabCommunityJob>>.Instance);
    }

    private static bool MatchesTestJobTrigger(ITrigger trigger, TimeSpan interval)
    {
        return trigger.Key.Equals(TestLabCommunityJob.Trigger) &&
               trigger is ISimpleTrigger { RepeatInterval: var repeatInterval } &&
               repeatInterval == interval;
    }

    private sealed class TestLabCommunityJob : ILabCommunityJob
    {
        public static JobKey Key => new("test-labcommunity-job");

        public static TriggerKey Trigger => new("test-labcommunity-job-trigger");

        private static Func<Configuration?, Configuration?, bool> _shouldReconfigureAfterConfigurationPull = (_, _) => false;
        private static Func<LabCommunityDefaultConfiguration, Configuration?, TimeSpan> _resolveInterval = (_, _) => TimeSpan.FromSeconds(42);
        public static void SetShouldReconfigureAfterConfigurationPull(Func<Configuration?, Configuration?, bool> func)
        {
            _shouldReconfigureAfterConfigurationPull = func;
        }

        public static void ResetShouldReconfigureAfterConfigurationPull()
        {
            _shouldReconfigureAfterConfigurationPull = (_, _) => false;
        }

        public static bool ShouldReconfigureAfterConfigurationPull(Configuration? newConfiguration, Configuration? previousConfiguration)
        {
            return _shouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);
        }

        public static void SetResolveInterval(Func<LabCommunityDefaultConfiguration, Configuration?, TimeSpan> func)
        {
            _resolveInterval = func;
        }

        public static void ResetResolveInterval()
        {
            _resolveInterval = (_, _) => TimeSpan.FromSeconds(42);
        }

        public static bool ShouldStop(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
        {
            return false;
        }

        public static TimeSpan ResolveInterval(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
        {
            return _resolveInterval(defaultConfiguration, configuration);
        }

        public Task Execute(IJobExecutionContext context)
        {
            return Task.CompletedTask;
        }
    }
}
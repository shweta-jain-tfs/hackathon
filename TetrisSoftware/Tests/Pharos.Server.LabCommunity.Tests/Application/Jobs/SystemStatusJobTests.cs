using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.LabCommunity.Application;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Jobs;
using Pharos.Server.LabCommunity.Domain;
using Quartz;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Jobs
{
    public static class SystemStatusJobTests
    {
        public class TheExecuteMethod
        {
            [Trait("ProductRequirement", "PR_538")]
            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public async Task SendsSystemStatusSliceCommands()
            {
                // Arrange
                var senderMock = new Mock<ISender>();
                var sut = new SystemStatusJob(
                    senderMock.Object,
                    Mock.Of<ILogger<SystemStatusJob>>());

                // Act
                await sut.Execute(Mock.Of<IJobExecutionContext>());

                // Assert
                senderMock.Verify(
                    s => s.Send(It.IsAny<SendPharosStatusToLabCommunityCommand>(), It.IsAny<CancellationToken>()),
                    Times.Once);
                senderMock.Verify(
                    s => s.Send(It.IsAny<SendInstrumentStatusToLabCommunityCommand>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            [Trait("ProductRequirement", "PR_538")]
            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public async Task SendsInstrumentStatusSliceCommand_WhenPharosStatusSliceCommandFails()
            {
                // Arrange
                var senderMock = new Mock<ISender>();
                senderMock.Setup(s => s.Send(
                        It.IsAny<SendPharosStatusToLabCommunityCommand>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("boom"));
                var sut = new SystemStatusJob(
                    senderMock.Object,
                    Mock.Of<ILogger<SystemStatusJob>>());

                // Act
                await sut.Execute(Mock.Of<IJobExecutionContext>());

                // Assert
                senderMock.Verify(
                    s => s.Send(It.IsAny<SendInstrumentStatusToLabCommunityCommand>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        public class TheResolveIntervalMethod
        {
            [Trait("ProductRequirement", "PR_538")]
            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public void UsesConfigurationSystemStatusInterval_WhenAvailable()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration { CheckInIntervalSeconds = 120 };
                var configuration = new Configuration { SystemStatusIntervalSeconds = 300, SystemStatusEnabled = true };

                // Act
                var result = SystemStatusJob.ResolveInterval(defaultConfiguration, configuration);

                // Assert
                Assert.Equal(TimeSpan.FromSeconds(300), result);
            }

            [Trait("ProductRequirement", "PR_538")]
            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public void UsesDefaultSystemStatusInterval_WhenConfigurationIsNotAvailable()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration { SystemStatusIntervalSeconds = 900 };

                // Act
                var result = SystemStatusJob.ResolveInterval(defaultConfiguration, configuration: null);

                // Assert
                Assert.Equal(TimeSpan.FromSeconds(900), result);
            }
        }

        public class TheShouldReconfigureAfterConfigurationPullMethod
        {
            [Trait("ProductRequirement", "PR_538")]
            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public void ReturnsTrue_WhenSystemStatusEnabledChanged()
            {
                // Arrange
                var previousConfiguration = new Configuration
                {
                    SystemStatusEnabled = false,
                    SystemStatusIntervalSeconds = 600
                };
                var newConfiguration = new Configuration
                {
                    SystemStatusEnabled = true,
                    SystemStatusIntervalSeconds = 600
                };

                // Act
                var result = SystemStatusJob.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);

                // Assert
                Assert.True(result);
            }

            [Trait("ProductRequirement", "PR_538")]
            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public void ReturnsTrue_WhenIntervalChanged()
            {
                // Arrange
                var previousConfiguration = new Configuration
                {
                    SystemStatusEnabled = true,
                    SystemStatusIntervalSeconds = 600
                };
                var newConfiguration = new Configuration
                {
                    SystemStatusEnabled = true,
                    SystemStatusIntervalSeconds = 300
                };

                // Act
                var result = SystemStatusJob.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);

                // Assert
                Assert.True(result);
            }

            [Trait("ProductRequirement", "PR_538")]
            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public void ReturnsFalse_WhenSystemStatusConfigurationIsUnchanged()
            {
                // Arrange
                var previousConfiguration = new Configuration
                {
                    SystemStatusEnabled = true,
                    SystemStatusIntervalSeconds = 600
                };
                var newConfiguration = new Configuration
                {
                    SystemStatusEnabled = true,
                    SystemStatusIntervalSeconds = 600
                };

                // Act
                var result = SystemStatusJob.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);

                // Assert
                Assert.False(result);
            }
        }

        public class TheShouldStopMethod
        {
            [Trait("ProductRequirement", "PR_538")]
            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public void ReturnsTrue_WhenSystemStatusIsDisabled()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration();
                var configuration = new Configuration { SystemStatusEnabled = false };

                // Act
                var result = SystemStatusJob.ShouldStop(defaultConfiguration, configuration);

                // Assert
                Assert.True(result);
            }

            [Trait("ProductRequirement", "PR_538")]
            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public void UsesDefaultSystemStatusEnabled_WhenConfigurationIsNotAvailable()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration { SystemStatusEnabled = true };

                // Act
                var result = SystemStatusJob.ShouldStop(defaultConfiguration, configuration: null);

                // Assert
                Assert.False(result);
            }
        }
    }
}
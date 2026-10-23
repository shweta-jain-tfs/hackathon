using MediatR;
using Moq;
using Pharos.Server.LabCommunity.Application;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Jobs;
using Pharos.Server.LabCommunity.Domain;
using Quartz;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Jobs
{
    public static class GetConfigurationJobTests
    {
        public class TheExecuteMethod
        {
            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task SendsGetConfigurationCommand()
            {
                // Arrange
                var senderMock = new Mock<ISender>();
                var sut = new GetConfigurationJob(senderMock.Object);

                // Act
                await sut.Execute(Mock.Of<IJobExecutionContext>());

                // Assert
                senderMock.Verify(
                    s => s.Send(It.IsAny<GetLabCommunityConfigurationCommand>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        public class TheShouldReconfigureAfterConfigurationPullMethod
        {
            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public void ReturnsTrue_WhenCheckInIntervalChanged()
            {
                // Arrange
                var previousConfiguration = new Configuration { CheckInIntervalSeconds = 120 };
                var newConfiguration = new Configuration { CheckInIntervalSeconds = 180 };

                // Act
                var result = GetConfigurationJob.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);

                // Assert
                Assert.True(result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public void ReturnsFalse_WhenCheckInIntervalIsUnchanged()
            {
                // Arrange
                var previousConfiguration = new Configuration { CheckInIntervalSeconds = 180 };
                var newConfiguration = new Configuration { CheckInIntervalSeconds = 180 };

                // Act
                var result = GetConfigurationJob.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);

                // Assert
                Assert.False(result);
            }
        }

        public class TheResolveIntervalMethod
        {
            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public void UsesConfiguration_WhenAvailable()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration { CheckInIntervalSeconds = 120 };
                var configuration = new Configuration { CheckInIntervalSeconds = 300 };

                // Act
                var result = GetConfigurationJob.ResolveInterval(defaultConfiguration, configuration);

                // Assert
                Assert.Equal(TimeSpan.FromSeconds(300), result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public void UsesDefaultConfiguration_WhenConfigurationIsNotAvailable()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration { CheckInIntervalSeconds = 120 };

                // Act
                var result = GetConfigurationJob.ResolveInterval(defaultConfiguration, configuration: null);

                // Assert
                Assert.Equal(TimeSpan.FromSeconds(120), result);
            }
        }
    }
}
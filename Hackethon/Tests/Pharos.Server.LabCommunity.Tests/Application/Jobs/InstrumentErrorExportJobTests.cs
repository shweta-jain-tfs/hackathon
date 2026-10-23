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
    public static class InstrumentErrorExportJobTests
    {
        public class TheExecuteMethod
        {
            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public async Task SendsExportItemsCommand()
            {
                // Arrange
                var senderMock = new Mock<ISender>();
                var sut = new InstrumentErrorExportJob(senderMock.Object);

                // Act
                await sut.Execute(Mock.Of<IJobExecutionContext>());

                // Assert
                senderMock.Verify(
                    s => s.Send(
                        It.Is<SendExportItemsToLabCommunityCommand>(c => c.ExportItemType == ExportItemType.InstrumentError),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        public class TheShouldReconfigureAfterConfigurationPullMethod
        {
            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public void ReturnsTrue_WhenReportInstrumentErrorsEnabledChanged()
            {
                // Arrange
                var previousConfiguration = new Configuration { ReportInstrumentErrorsEnabled = false };
                var newConfiguration = new Configuration { ReportInstrumentErrorsEnabled = true };

                // Act
                var result = InstrumentErrorExportJob.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);

                // Assert
                Assert.True(result);
            }

            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public void ReturnsFalse_WhenReportInstrumentErrorsEnabledIsUnchanged()
            {
                // Arrange
                var previousConfiguration = new Configuration { ReportInstrumentErrorsEnabled = true };
                var newConfiguration = new Configuration { ReportInstrumentErrorsEnabled = true };

                // Act
                var result = InstrumentErrorExportJob.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);

                // Assert
                Assert.False(result);
            }
        }

        public class TheShouldStopMethod
        {
            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public void ReturnsTrue_WhenConfigurationDisablesInstrumentErrors()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration();
                var configuration = new Configuration { ReportInstrumentErrorsEnabled = false };

                // Act
                var result = InstrumentErrorExportJob.ShouldStop(defaultConfiguration, configuration);

                // Assert
                Assert.True(result);
            }

            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public void UsesConfiguration_WhenAvailable()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration();
                var configuration = new Configuration { ReportInstrumentErrorsEnabled = true };

                // Act
                var result = InstrumentErrorExportJob.ShouldStop(defaultConfiguration, configuration);

                // Assert
                Assert.False(result);
            }

            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public void ReturnsTrue_WhenNoConfigurationIsAvailable()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration();

                // Act
                var result = InstrumentErrorExportJob.ShouldStop(defaultConfiguration, configuration: null);

                // Assert
                Assert.True(result);
            }
        }

        public class TheResolveIntervalMethod
        {
            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public void UsesDefaultReportInstrumentErrorsInterval()
            {
                // Arrange
                var defaultConfiguration = new LabCommunityDefaultConfiguration
                {
                    CheckInIntervalSeconds = 120,
                    ReportInstrumentErrorsIntervalSeconds = 30
                };

                // Act
                var result = InstrumentErrorExportJob.ResolveInterval(defaultConfiguration, configuration: null);

                // Assert
                Assert.Equal(TimeSpan.FromSeconds(30), result);
            }
        }
    }
}
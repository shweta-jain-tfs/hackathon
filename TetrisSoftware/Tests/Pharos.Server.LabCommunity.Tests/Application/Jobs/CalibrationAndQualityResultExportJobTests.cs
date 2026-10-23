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
    public static class CalibrationAndQualityResultExportJobTests
    {
        public class TheExecuteMethod
        {
            [Trait("ProductRequirement", "PR_2183")]
            [Trait("ProductRequirement", "PR_2184")]
            [Trait("ProductRequirement", "PR_2185")]
            [Trait("ModuleRequirement", "MR_6055")]
            [Trait("ModuleRequirement", "MR_6058")]
            [Trait("ModuleRequirement", "MR_6229")]
            [Fact]
            public async Task SendsExportItemsCommandForEachCalibrationAndQualityResultType()
            {
                var senderMock = new Mock<ISender>();
                var sut = new CalibrationAndQualityResultExportJob(senderMock.Object);

                await sut.Execute(Mock.Of<IJobExecutionContext>());

                foreach (var exportItemType in new[] { ExportItemType.CurveControlResult, ExportItemType.QualityControlResult, ExportItemType.BlankResult })
                {
                    senderMock.Verify(
                        sender => sender.Send(
                            It.Is<SendExportItemsToLabCommunityCommand>(command => command.ExportItemType == exportItemType),
                            It.IsAny<CancellationToken>()),
                        Times.Once);
                }
            }
        }

        public class TheShouldReconfigureAfterConfigurationPullMethod
        {
            [Theory]
            [InlineData(null, null, false)]
            [InlineData(true, null, true)]
            [InlineData(true, false, true)]
            [InlineData(true, true, false)]
            public void ReconfiguresOnlyWhenReportingSettingBecomesAvailableOrChanges(
                bool? newValue,
                bool? previousValue,
                bool expected)
            {
                var newConfiguration = CreateConfiguration(newValue);
                var previousConfiguration = CreateConfiguration(previousValue);

                var result = CalibrationAndQualityResultExportJob.ShouldReconfigureAfterConfigurationPull(
                    newConfiguration,
                    previousConfiguration);

                Assert.Equal(expected, result);
            }
        }

        public class TheShouldStopMethod
        {
            [Theory]
            [InlineData(null, true)]
            [InlineData(false, true)]
            [InlineData(true, false)]
            public void StopsUnlessReportingIsEnabled(bool? reportingEnabled, bool expected)
            {
                var configuration = CreateConfiguration(reportingEnabled);

                var result = CalibrationAndQualityResultExportJob.ShouldStop(
                    new LabCommunityDefaultConfiguration(),
                    configuration);

                Assert.Equal(expected, result);
            }
        }

        public class TheResolveIntervalMethod
        {
            [Fact]
            public void UsesDefaultCalibrationAndQualityResultExportInterval()
            {
                var defaultConfiguration = new LabCommunityDefaultConfiguration
                {
                    ReportCalibrationAndQualityResultsIntervalSeconds = 30
                };

                var result = CalibrationAndQualityResultExportJob.ResolveInterval(
                    defaultConfiguration,
                    configuration: null);

                Assert.Equal(TimeSpan.FromSeconds(30), result);
            }
        }

        private static Configuration? CreateConfiguration(bool? reportingEnabled)
        {
            return reportingEnabled is null
                ? null
                : new Configuration { ReportCalibrationAndQualityResultsEnabled = reportingEnabled.Value };
        }
    }
}

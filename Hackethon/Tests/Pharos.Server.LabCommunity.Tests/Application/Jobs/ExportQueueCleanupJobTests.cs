using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.LabCommunity.Application;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Jobs;
using Quartz;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Jobs
{
    public static class ExportQueueCleanupJobTests
    {
        public class TheExecuteMethod
        {
            [Fact]
            public async Task SendsCleanupCommandWithConfiguredRetention()
            {
                // Arrange
                var senderMock = new Mock<ISender>();
                var configuration = Options.Create(new LabCommunityDefaultConfiguration
                {
                    DeleteExportItemsOlderThanDays = 90
                });
                var sut = new ExportQueueCleanupJob(senderMock.Object, configuration);

                // Act
                await sut.Execute(Mock.Of<IJobExecutionContext>());

                // Assert
                senderMock.Verify(
                    sender => sender.Send(
                        It.Is<CleanupLabCommunityExportQueueCommand>(command => command.OlderThanDays == 90),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        public class TheResolveIntervalMethod
        {
            [Fact]
            public void UsesConfiguredCleanupInterval()
            {
                // Arrange
                var configuration = new LabCommunityDefaultConfiguration
                {
                    ExportQueueCleanupIntervalDays = 1
                };

                // Act
                var result = ExportQueueCleanupJob.ResolveInterval(configuration, configuration: null);

                // Assert
                Assert.Equal(TimeSpan.FromDays(1), result);
            }
        }
    }
}
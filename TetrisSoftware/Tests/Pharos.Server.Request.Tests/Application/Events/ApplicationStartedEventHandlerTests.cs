using Quartz;
using Pharos.Server.Application;
using Pharos.Server.Request.Application.Commands.RequestIssue;

namespace Pharos.Server.Request.Application.Events;

public static class ApplicationStartedEventHandlerTests
{
    public class TheHandleMethod
    {
        [Fact]
        public async Task ConfiguresSchedulerAndStartsNotReportedSync_OnApplicationStarted()
        {
            // Arrange
            var schedulerFactoryMock = new Mock<ISchedulerFactory>();
            var schedulerMock = new Mock<IScheduler>();
            schedulerFactoryMock
                .Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedulerMock.Object);

            var senderMock = new Mock<ISender>();
            senderMock
                .Setup(x => x.Send(It.IsAny<SyncNotReportedRequestIssuesCommand>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(MediatR.Unit.Value));

            var sut = new ApplicationStartedEventHandler(schedulerFactoryMock.Object, senderMock.Object);

            // Act
            await sut.Handle(new ApplicationStartedEvent(), TestContext.Current.CancellationToken);

            // Assert
            senderMock.Verify(
                x => x.Send(It.IsAny<SyncNotReportedRequestIssuesCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

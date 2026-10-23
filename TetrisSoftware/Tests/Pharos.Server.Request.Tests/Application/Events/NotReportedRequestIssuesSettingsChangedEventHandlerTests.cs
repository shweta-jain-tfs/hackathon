using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Request.Application.Commands.RequestIssue;

namespace Pharos.Server.Request.Application.Events;

public class NotReportedRequestIssuesSettingsChangedEventHandlerTests
{
    private readonly Mock<ISender> _senderMock = new();

    [Fact]
    public async Task Handle_ShouldSyncNotReportedIssues_WhenLisSettingsChange()
    {
        // Arrange
        var sut = new NotReportedRequestIssuesSettingsChangedEventHandler(_senderMock.Object);
        var notification = new SettingsChangedEvent<LisSettings>(new LisSettings(), new LisSettings());

        // Act
        await sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        VerifyIssuesSynchronized();
    }

    [Fact]
    public async Task Handle_ShouldSyncNotReportedIssues_WhenMethodsChange()
    {
        // Arrange
        var sut = new NotReportedRequestIssuesSettingsChangedEventHandler(_senderMock.Object);

        // Act
        await sut.Handle(new MethodsUpdatedEvent(), TestContext.Current.CancellationToken);

        // Assert
        VerifyIssuesSynchronized();
    }

    private void VerifyIssuesSynchronized()
    {
        _senderMock.Verify(
            x => x.Send(It.IsAny<SyncNotReportedRequestIssuesCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

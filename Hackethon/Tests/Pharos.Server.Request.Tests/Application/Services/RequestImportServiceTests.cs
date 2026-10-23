using Pharos.Server.Application.SystemErrors;

namespace Pharos.Server.Request.Application.Services;

public class RequestImportServiceTests
{
    [Fact]
    public async Task ImportRequestsFile_WhenHandlerThrows_SendsSystemErrorAndRethrows()
    {
        // Arrange
        var requestImportHandler = new Mock<IRequestImportHandler>();
        var sender = new Mock<ISender>();
        var fileName = "requests.csv";
        var fileContent = new byte[] { 1, 2, 3 };

        requestImportHandler
            .Setup(x => x.Handle(fileContent, fileName))
            .ThrowsAsync(new InvalidOperationException("Import failed"));

        var sut = new RequestImportService(requestImportHandler.Object, sender.Object);

        // Act
        var action = () => sut.ImportRequestsFile(fileContent, fileName);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Import failed");

        sender.Verify(x => x.Send(
                It.Is<CreatePharosErrorCommand>(cmd =>
                    cmd.ErrorCode == PharosErrorCodes.RequestImportFailed
                    && cmd.Classification == ErrorClassification.Error
                    && cmd.InstrumentNumber == null
                    && cmd.Parameters != null
                    && cmd.Parameters.Count == 2
                    && cmd.Parameters[0] == fileName
                    && cmd.Parameters[1].Contains("Import failed")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportRequestsFile_WhenHandlerSucceeds_DoesNotSendSystemError()
    {
        // Arrange
        var requestImportHandler = new Mock<IRequestImportHandler>();
        var sender = new Mock<ISender>();
        var fileName = "requests.csv";
        var fileContent = new byte[] { 1, 2, 3 };

        requestImportHandler
            .Setup(x => x.Handle(fileContent, fileName))
            .Returns(Task.CompletedTask);

        var sut = new RequestImportService(requestImportHandler.Object, sender.Object);

        // Act
        await sut.ImportRequestsFile(fileContent, fileName);

        // Assert
        sender.Verify(x => x.Send(It.IsAny<CreatePharosErrorCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

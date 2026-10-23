using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pharos.Server.Application.AuditTrail;

namespace Pharos.Server.Request.Web.Controllers;

public class TubeDefinitionsControllerTests
{
    [Fact]
    public async Task RequestImport_When_FileIsNull_ShouldReturnBadRequestValidationProblem()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestsServiceMock = new Mock<IRequestsService>();

        var controller = new TubeDefinitionsController(senderMock.Object, requestsServiceMock.Object);

        // Act
        var result = await controller.RequestImport(null, TestContext.Current.CancellationToken);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("file");
        senderMock.Verify(x => x.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestImport_When_ImportSucceeds_ShouldReturnNoContentAndCreateAuditEvent()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestsServiceMock = new Mock<IRequestsService>();

        senderMock
            .Setup(x => x.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new TubeDefinitionsController(senderMock.Object, requestsServiceMock.Object);
        var formFile = CreateFormFile("SampleId,MethodName,TestName\r\nS1,M1,T1", "requests.csv");

        // Act
        var result = await controller.RequestImport(formFile, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        requestsServiceMock.Verify(x => x.ImportRequestsFile(It.IsAny<byte[]>(), "requests.csv"), Times.Once);
        senderMock.Verify(x => x.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestImport_When_ImportThrowsArgumentException_ShouldReturnBadRequest()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestsServiceMock = new Mock<IRequestsService>();

        requestsServiceMock
            .Setup(x => x.ImportRequestsFile(It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Request import file missing mandatory column 'TestName'.", "mandatoryColumnName"));

        var controller = new TubeDefinitionsController(senderMock.Object, requestsServiceMock.Object);
        var formFile = CreateFormFile("SampleId,MethodName\r\nS1,M1", "requests.csv");

        // Act
        var result = await controller.RequestImport(formFile, TestContext.Current.CancellationToken);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("mandatoryColumnName");
        problem.Errors["mandatoryColumnName"].Single().Should().Contain("missing mandatory column");

        senderMock.Verify(x => x.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestImport_When_ImportThrowsUnknownException_ShouldRethrow()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestsServiceMock = new Mock<IRequestsService>();

        requestsServiceMock
            .Setup(x => x.ImportRequestsFile(It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected import failure"));

        var controller = new TubeDefinitionsController(senderMock.Object, requestsServiceMock.Object);
        var formFile = CreateFormFile("SampleId,MethodName,TestName\r\nS1,M1,T1", "requests.csv");

        // Act
        var act = async () => await controller.RequestImport(formFile, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        senderMock.Verify(x => x.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static FormFile CreateFormFile(string content, string fileName)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }
}

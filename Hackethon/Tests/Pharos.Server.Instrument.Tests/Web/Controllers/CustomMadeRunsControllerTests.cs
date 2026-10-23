using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Pharos.Server.Application;
using Pharos.Server.Instrument.Application.Commands.SendCmrTestOrder;

namespace Pharos.Server.Instrument.Web.Controllers;

public class CustomMadeRunsControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<ICurrentInstrumentService> _instrumentServiceMock;
    private readonly CustomMadeRunsController _controller;

    private const string _serialNumber = "SN001";

    public CustomMadeRunsControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _senderMock
            .Setup(s => s.Send(It.IsAny<SendCmrTestOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CmrTestOrderResultDto { Status = CmrTestOrderStatus.Ok });

        _instrumentServiceMock = new Mock<ICurrentInstrumentService>();
        _instrumentServiceMock
            .Setup(s => s.InstrumentNumber)
            .Returns(_serialNumber);

        _controller = new CustomMadeRunsController(_senderMock.Object, _instrumentServiceMock.Object);
    }

    [Fact]
    public async Task UploadFile_WhenFileIsNotCsv_ShouldReturnBadRequest()
    {
        // Arrange
        var fileMock = CreateFormFileMock("testfile.txt");

        // Act and assert
        await Assert.ThrowsAsync<ValidationException>(() => _controller.UploadFile(fileMock.Object));
    }

    [Theory]
    [InlineData(".cmr")]
    [InlineData(".xml")]
    [InlineData(".json")]
    [InlineData(".xlsx")]
    public async Task UploadFile_WhenFileHasNonCsvExtension_ShouldReturnBadRequest(string extension)
    {
        // Arrange
        var fileMock = CreateFormFileMock($"testfile{extension}");

        // Act and assert
        await Assert.ThrowsAsync<ValidationException>(() => _controller.UploadFile(fileMock.Object));
    }

    [Theory]
    [Trait("ModuleRequirement", "MR_5837")]
    [Trait("ProductRequirement", "PR_784")]
    [InlineData(".csv")]
    [InlineData(".CSV")]
    [InlineData(".Csv")]
    public async Task UploadFile_WhenFileHasCsvExtension_ShouldSendCommandAndReturnOk(string extension)
    {
        // Arrange
        var fileMock = CreateFormFileMock($"testfile{extension}");

        // Act
        var result = await _controller.UploadFile(fileMock.Object);

        // Assert
        result.Should().BeOfType<CmrTestOrderResultDto>();
        _senderMock.Verify(
            s => s.Send(It.Is<SendCmrTestOrderCommand>(c => c.InstrumentNumber == _serialNumber), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadFile_WhenResultIsSuccess_ShouldReturnOkWithOkStatus()
    {
        // Arrange
        var fileMock = CreateFormFileMock("test.csv");
        _senderMock
            .Setup(s => s.Send(It.IsAny<SendCmrTestOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CmrTestOrderResultDto { Status = CmrTestOrderStatus.Ok });

        // Act
        var result = await _controller.UploadFile(fileMock.Object);

        // Assert
        result.Should().BeOfType<CmrTestOrderResultDto>()
            .Which.Status.Should().Be(CmrTestOrderStatus.Ok);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5840")]
    [Trait("ProductRequirement", "PR_784")]
    public async Task UploadFile_WhenResultIsFailure_ShouldReturnOkWithFailedStatus()
    {
        // Arrange
        var fileMock = CreateFormFileMock("test.csv");
        _senderMock
            .Setup(s => s.Send(It.IsAny<SendCmrTestOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CmrTestOrderResultDto { Status = CmrTestOrderStatus.ParseError });

        // Act
        var result = await _controller.UploadFile(fileMock.Object);

        // Assert
        result.Should().BeOfType<CmrTestOrderResultDto>()
            .Which.Status.Should().Be(CmrTestOrderStatus.ParseError);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5837")]
    [Trait("ProductRequirement", "PR_784")]
    public async Task UploadFile_ShouldSendFileDataInCommand()
    {
        // Arrange
        var fileData = new byte[] { 10, 20, 30 };
        var fileMock = CreateFormFileMock("test.csv", fileData);

        SendCmrTestOrderCommand? capturedCommand = null;
        _senderMock
            .Setup(s => s.Send(It.IsAny<SendCmrTestOrderCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<CmrTestOrderResultDto>, CancellationToken>((req, _) => capturedCommand = req as SendCmrTestOrderCommand)
            .ReturnsAsync(new CmrTestOrderResultDto { Status = CmrTestOrderStatus.Ok });

        // Act
        await _controller.UploadFile(fileMock.Object);

        // Assert
        capturedCommand.Should().NotBeNull();
        capturedCommand!.InstrumentNumber.Should().Be(_serialNumber);
        capturedCommand.Data.Should().BeEquivalentTo(fileData);
    }

    private static Mock<IFormFile> CreateFormFileMock(string fileName, byte[]? content = null)
    {
        content ??= [1, 2, 3];
        var stream = new MemoryStream(content);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((targetStream, ct) =>
            {
                stream.Position = 0;
                return stream.CopyToAsync(targetStream, ct);
            });

        return fileMock;
    }
}

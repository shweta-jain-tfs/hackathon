using System.Globalization;
using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Queries;

namespace Pharos.Server.Reagents.Web.Controllers;

public class ReagentDrawerControllerTests
{
    [Trait("ProductRequirement", "PR_1358")]
    [Trait("ModuleRequirement", "MR_4502")]
    [Trait("ProductRequirement", "PR_312")]
    [Fact]
    public async Task GetReagentDrawerDetails_ReturnsExpectedDto()
    {
        var instrumentNumber = "SN123";
        var expectedDto = MockReagentDrawerData();
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        currentInstrumentServiceMock.Setup(x => x.InstrumentNumber)
            .Returns(instrumentNumber);

        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.Is<GetReagentDrawerDetailsQuery>(q => q.InstrumentNumber == instrumentNumber), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var controller = new ReagentDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act
        var result = await controller.GetReagentDrawerDetails(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedDto);
        senderMock.Verify(x => x.Send(It.IsAny<GetReagentDrawerDetailsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReagentDrawerDetails_ThrowsNotFoundException_WhenInstrumentNumberIsNull()
    {
        // Arrange
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        var senderMock = new Mock<ISender>();
        var controller = new ReagentDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => controller.GetReagentDrawerDetails(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("B123", ArticleType.CalibratorCartridge, ArticleQuantityUnit.Tests, ArticleIndicatorShape.Round, "Test1", "LotA", 0, "2025-12-31T00:00:00+00:00")]
    [InlineData("B456", ArticleType.ConjugateCartridge, ArticleQuantityUnit.Milliliters, ArticleIndicatorShape.Rectangular, "Test2", "LotB", 5, null)]
    [InlineData("B789", ArticleType.CurveControlCartridge, ArticleQuantityUnit.Sets, ArticleIndicatorShape.Slanted, null, null, 0, "2024-06-01T00:00:00+00:00")]
    public async Task GetReagentContentDetail_ReturnsExpectedDto(
        string barcode,
        ArticleType type,
        ArticleQuantityUnit unit,
        ArticleIndicatorShape shape,
        string? test,
        string? lotNumber,
        int quantity,
        string? expiryDateStr)
    {
        // Arrange
        var instrumentNumber = "SN123";
        var expectedDto = new ReagentContentDetailDto
        {
            Type = type,
            UnitOfMeasure = unit,
            Shape = shape,
            Test = test,
            LotNumber = lotNumber,
            Quantity = quantity,
            ExpiryDate = expiryDateStr != null ? DateTimeOffset.Parse(expiryDateStr, CultureInfo.InvariantCulture) : null,
            Colors = new List<string?> { "Red", "Green" },
            Texts = new List<string?> { "Text1", "Text2" }
        };
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        currentInstrumentServiceMock.Setup(x => x.InstrumentNumber)
            .Returns(instrumentNumber);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.Is<GetReagentContentDetailQuery>(q => q.Barcode == barcode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);
        var controller = new ReagentDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act
        var result = await controller.GetReagentContentDetail(barcode, null, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto, options => options
            .RespectingRuntimeTypes()
            .WithStrictOrdering());
        result.Type.Should().Be(type);
        result.UnitOfMeasure.Should().Be(unit);
        result.Shape.Should().Be(shape);
        result.Test.Should().Be(test);
        result.LotNumber.Should().Be(lotNumber);
        result.Quantity.Should().Be(quantity);
        if (expiryDateStr != null)
        {
            result.ExpiryDate.Should().Be(DateTimeOffset.Parse(expiryDateStr, CultureInfo.InvariantCulture));
        }
        else
        {
            result.ExpiryDate.Should().BeNull();
        }

        result.Colors.Should().BeEquivalentTo(expectedDto.Colors);
        result.Texts.Should().BeEquivalentTo(expectedDto.Texts);
        senderMock.Verify(x => x.Send(It.IsAny<GetReagentContentDetailQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReagentContentDetail_PropagatesExceptionFromSender()
    {
        // Arrange
        var barcode = "B123";
        var instrumentNumber = "SN123";
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        currentInstrumentServiceMock.Setup(x => x.InstrumentNumber)
            .Returns(instrumentNumber);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.IsAny<GetReagentContentDetailQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException());
        var controller = new ReagentDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => controller.GetReagentContentDetail(barcode, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetReagentContentDetail_AllowsEmptyBarcode()
    {
        // Arrange
        var barcode = string.Empty;
        var instrumentNumber = "SN123";
        var expectedDto = new ReagentContentDetailDto();
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        currentInstrumentServiceMock.Setup(x => x.InstrumentNumber)
            .Returns(instrumentNumber);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.Is<GetReagentContentDetailQuery>(q => q.Barcode == barcode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);
        var controller = new ReagentDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act
        var result = await controller.GetReagentContentDetail(barcode, null, TestContext.Current.CancellationToken);
        result.Should().Be(expectedDto);
    }

    private static ReagentDrawerDto MockReagentDrawerData()
    {
        return new ReagentDrawerDto
        {
            DilutionPlateStacks = new List<DilutionPlateStackDto> {
                new() {
                    NumberOfPlates = 3, Position = 1, Status = ReagentPositionStatus.DilutionPlateNotDetected
                } },
            ReagentLoaders = new List<ReagentLoaderDto> {
                new() {
                    Compartments = new List<ReagentCompartmentDto> {
                        new() { Position = 1, IsEmpty = true,
                            CellContent = new List<CompartmentCellContentDto>{ new() { ReagentBarcode = "ZYZ1", Position =1, Status = ReagentPositionStatus.AspirationError} },
                            Type = CompartmentReagentType.Cartridge }
                    },
                    Location = "Location1",
                    Position = 1,
                    Status = ReagentPositionStatus.CartridgeStorageFull
                } },
        };
    }
}

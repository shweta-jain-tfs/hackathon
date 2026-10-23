using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetInternalDilutionPlatesQueryHandlerTests
{
    private const string _testInstrument1 = "Inst1";
    private const string _testInstrument2 = "Inst2";
    private const string _testSerialNumber1 = "SN001";
    private const string _testSerialNumber2 = "SN002";

    [Fact]
    public async Task Handle_ReturnsEmptyResult_WhenNoInstruments()
    {
        // Arrange
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(new List<IInstrument>());

        var reagentStorage = new Mock<IInstrumentReagentStorage>();

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().HaveCount(1);
        result.Items[0].Items.Should().BeEmpty();
        result.Items[0].PlatesCount.Should().Be(0);
        result.Items[0].FreeWellsCount.Should().Be(0);
    }

    [Trait("ProductRequirement", "PR_1662")]
    [Trait("ModuleRequirement", "MR_4940")]
    [Fact]
    public async Task Handle_ReturnsInternalDilutionPlates_WhenPlatesExist()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var dilutionPlates = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(1, 5),
            CreateDilutionPlateInStation(2, 10)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber1))
            .Returns(dilutionPlates);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items[0].Items.Should().HaveCount(2);
        result.Items[0].PlatesCount.Should().Be(2);
        result.Items[0].FreeWellsCount.Should().Be((Constants.MaxDilutionPlateWells - 5) + (Constants.MaxDilutionPlateWells - 10));
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4929")]
    [Theory]
    [InlineData(1, InternalDilutionPlatePosition.Left)]
    [InlineData(2, InternalDilutionPlatePosition.Right)]
    public async Task Handle_MapsPositionCorrectly(int position, InternalDilutionPlatePosition expectedPosition)
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var dilutionPlates = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(position, 5)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber1))
            .Returns(dilutionPlates);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(1);
        result.Items[0].Items[0].Position.Should().Be(expectedPosition);
    }

    [Trait("ProductRequirement", "PR_1662")]
    [Trait("ModuleRequirement", "MR_4931")]
    [Trait("ProductRequirement", "PR_1662")]
    [Trait("ModuleRequirement", "MR_4940")]
    [Fact]
    public async Task Handle_CalculatesFreeWellsCorrectly()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var usedCells = 15;
        var dilutionPlates = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(1, usedCells)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber1))
            .Returns(dilutionPlates);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var expectedFreeWells = Constants.MaxDilutionPlateWells - usedCells;
        result.Items[0].Items[0].FreeWells.Should().Be(expectedFreeWells);
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4932")]
    [Trait("ProductRequirement", "PR_1662")]
    [Trait("ModuleRequirement", "MR_4940")]
    [Fact]
    public async Task Handle_AggregatesDataFromMultipleInstruments()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1),
            CreateInstrument(_testInstrument2, _testSerialNumber2)
        };

        var plates1 = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(1, 5)
        };

        var plates2 = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(2, 8)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber1))
            .Returns(plates1);
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber2))
            .Returns(plates2);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items[0].Items.Should().HaveCount(2);
        result.Items[0].Items.Should().Contain(p => p.Instrument == _testInstrument1);
        result.Items[0].Items.Should().Contain(p => p.Instrument == _testInstrument2);
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4936")]
    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4932")]
    [Fact]
    public async Task Handle_FiltersInstruments_WhenSelectedInstrumentsProvided()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1),
            CreateInstrument(_testInstrument2, _testSerialNumber2)
        };

        var plates1 = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(1, 5)
        };

        var plates2 = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(2, 8)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber1))
            .Returns(plates1);
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber2))
            .Returns(plates2);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedInstruments = [_testInstrument1]
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items[0].Items.Should().HaveCount(1);
        result.Items[0].Items[0].Instrument.Should().Be(_testInstrument1);
        reagentStorage.Verify(x => x.GetDilutionPlatesStation(_testSerialNumber1), Times.Once);
        reagentStorage.Verify(x => x.GetDilutionPlatesStation(_testSerialNumber2), Times.Never);
    }

    [Fact]
    public async Task Handle_UsesAllInstruments_WhenSelectedInstrumentsIsNull()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1),
            CreateInstrument(_testInstrument2, _testSerialNumber2)
        };

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(It.IsAny<string>()))
            .Returns([CreateDilutionPlateInStation(1, 1)]);

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedInstruments = null
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(2);
        reagentStorage.Verify(x => x.GetDilutionPlatesStation(_testSerialNumber1), Times.Once);
        reagentStorage.Verify(x => x.GetDilutionPlatesStation(_testSerialNumber2), Times.Once);
    }

    [Fact]
    public async Task Handle_UsesAllInstruments_WhenSelectedInstrumentsIsEmpty()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1),
            CreateInstrument(_testInstrument2, _testSerialNumber2)
        };

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(It.IsAny<string>()))
            .Returns([CreateDilutionPlateInStation(1, 1)]);

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedInstruments = []
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(2);
        reagentStorage.Verify(x => x.GetDilutionPlatesStation(_testSerialNumber1), Times.Once);
        reagentStorage.Verify(x => x.GetDilutionPlatesStation(_testSerialNumber2), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4938")]
    [Fact]
    public async Task Handle_SortsDefaultByInstrumentThenPosition()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument("Zebra", "SN003"),
            CreateInstrument("Alpha", "SN001")
        };

        var plates1 = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(2, 5),
            CreateDilutionPlateInStation(1, 3)
        };

        var plates2 = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(1, 7)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation("SN003"))
            .Returns(plates1);
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation("SN001"))
            .Returns(plates2);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(3);
        result.Items[0].Items[0].Instrument.Should().Be("Alpha");
        result.Items[0].Items[0].Position.Should().Be(InternalDilutionPlatePosition.Left);
        result.Items[0].Items[1].Instrument.Should().Be("Zebra");
        result.Items[0].Items[1].Position.Should().Be(InternalDilutionPlatePosition.Left);
        result.Items[0].Items[2].Instrument.Should().Be("Zebra");
        result.Items[0].Items[2].Position.Should().Be(InternalDilutionPlatePosition.Right);
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4938")]
    [Theory]
    [InlineData(nameof(InternalDilutionPlateDto.Position), true)]
    [InlineData(nameof(InternalDilutionPlateDto.Position), false)]
    [InlineData(nameof(InternalDilutionPlateDto.FreeWells), true)]
    [InlineData(nameof(InternalDilutionPlateDto.FreeWells), false)]
    [InlineData(nameof(InternalDilutionPlateDto.Instrument), true)]
    [InlineData(nameof(InternalDilutionPlateDto.Instrument), false)]
    public async Task Handle_SortsBySpecifiedProperty(string sortProperty, bool ascending)
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument("Inst2", "SN002"),
            CreateInstrument("Inst1", "SN001")
        };

        var plates1 = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(2, 5),
            CreateDilutionPlateInStation(1, 3)
        };

        var plates2 = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(1, 15)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation("SN001"))
            .Returns(plates1);
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation("SN002"))
            .Returns(plates2);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10,
            SortByProperty = sortProperty,
            SortByAscending = ascending
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(3);

        if (sortProperty == nameof(InternalDilutionPlateDto.Position))
        {
            var positions = result.Items[0].Items.Select(x => x.Position).ToList();
            if (ascending)
            {
                positions.Should().BeInAscendingOrder();
            }
            else
            {
                positions.Should().BeInDescendingOrder();
            }
        }
        else if (sortProperty == nameof(InternalDilutionPlateDto.FreeWells))
        {
            var freeWells = result.Items[0].Items.Select(x => x.FreeWells).ToList();
            if (ascending)
            {
                freeWells.Should().BeInAscendingOrder();
            }
            else
            {
                freeWells.Should().BeInDescendingOrder();
            }
        }
        else if (sortProperty == nameof(InternalDilutionPlateDto.Instrument))
        {
            var instrumentNames = result.Items[0].Items.Select(x => x.Instrument).ToList();
            if (ascending)
            {
                instrumentNames.Should().BeInAscendingOrder();
            }
            else
            {
                instrumentNames.Should().BeInDescendingOrder();
            }
        }
    }

    [Fact]
    public async Task Handle_AppliesPagination_Correctly()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var plates = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(1, 1),
            CreateDilutionPlateInStation(2, 2)
        };

        // Add more instruments to have enough data for pagination
        var allPlates = new List<DilutionPlateInStation>();
        for (var i = 1; i <= 5; i++)
        {
            allPlates.Add(CreateDilutionPlateInStation(i % 2 == 0 ? 2 : 1, i));
        }

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber1))
            .Returns(allPlates);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 1,
            Count = 2
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(5);
        result.StartIndex.Should().Be(1);
        result.Items[0].Items.Should().HaveCount(2);
    }

    [Trait("ProductRequirement", "PR_1662")]
    [Trait("ModuleRequirement", "MR_4940")]
    [Fact]
    public async Task Handle_CalculatesPlatesCountAndFreeWellsCount_FromPagedResults()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var plates = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(1, 10),
            CreateDilutionPlateInStation(2, 20)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber1))
            .Returns(plates);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].PlatesCount.Should().Be(2);
        var expectedFreeWells = (Constants.MaxDilutionPlateWells - 10) + (Constants.MaxDilutionPlateWells - 20);
        result.Items[0].FreeWellsCount.Should().Be(expectedFreeWells);
    }

    [Fact]
    public async Task Handle_GeneratesUniqueIds_ForEachPlateDto()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var plates = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(1, 5),
            CreateDilutionPlateInStation(2, 5)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber1))
            .Returns(plates);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(2);
        var ids = result.Items[0].Items.Select(x => x.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_ThrowsValidationException_WhenPositionIsInvalid()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var plates = new List<DilutionPlateInStation>
        {
            CreateDilutionPlateInStation(3, 5) // Invalid position
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesStation(_testSerialNumber1))
            .Returns(plates);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetInternalDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        Func<Task> act = async () => await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Position*unknown*");
    }

    private static GetInternalDilutionPlatesQueryHandler CreateHandler(
        IInstrumentManager instrumentService,
        IInstrumentReagentStorage reagentStorage)
    {
        return new GetInternalDilutionPlatesQueryHandler(instrumentService, reagentStorage);
    }

    private static TestInstrument CreateInstrument(string displayName, string serialNumber)
    {
        return new TestInstrument(serialNumber, InstrumentType.Medium, displayName);
    }

    private static DilutionPlateInStation CreateDilutionPlateInStation(int position, int cellCount)
    {
        var dilutionPlate = new DilutionPlateInStation
        {
            Position = position
        };

        for (var i = 0; i < cellCount; i++)
        {
            dilutionPlate.Cells.Add(new DilutionCell());
        }

        return dilutionPlate;
    }
}

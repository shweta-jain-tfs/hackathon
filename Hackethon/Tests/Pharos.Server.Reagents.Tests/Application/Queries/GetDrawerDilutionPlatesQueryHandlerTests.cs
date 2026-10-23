using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetDrawerDilutionPlatesQueryHandlerTests
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
        var query = new GetDrawerDilutionPlatesQuery
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
        result.Items[0].WellsCount.Should().Be(0);
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4924")]
    [Trait("ProductRequirement", "PR_1662")]
    [Trait("ModuleRequirement", "MR_4939")]
    [Fact]
    public async Task Handle_ReturnsOnlyStacksWithPlates_WhenNoErrors()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var dilutionPlateStacks = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 5),
            CreateDilutionPlateStack(2, 0),
            CreateDilutionPlateStack(3, 3)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber1))
            .Returns(dilutionPlateStacks);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(1);
        result.Items[0].Items.Should().HaveCount(2);
        result.Items[0].Items.Should().OnlyContain(x => x.DilutionPlates > 0);
        result.Items[0].PlatesCount.Should().Be(8);
        result.Items[0].WellsCount.Should().Be(8 * Constants.MaxDilutionPlateWells);
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4927")]
    [Fact]
    public async Task Handle_IncludesStacksWithErrors_EvenWithZeroPlates()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var dilutionPlateStacks = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 0, EnumDilutionPlatePositionError.CouldNotDetectDilutionPlates),
            CreateDilutionPlateStack(2, 0, EnumDilutionPlatePositionError.CouldNotPickupDilutionPlates),
            CreateDilutionPlateStack(3, 0)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber1))
            .Returns(dilutionPlateStacks);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
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
        result.Items[0].Items.Should().OnlyContain(x =>
            x.Status == ReagentPositionStatus.DilutionPlateNotDetected ||
            x.Status == ReagentPositionStatus.DilutionPlateNotPickedUp);
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4925")]
    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4927")]
    [Theory]
    [InlineData(EnumDilutionPlatePositionError.CouldNotDetectDilutionPlates, ReagentPositionStatus.DilutionPlateNotDetected)]
    [InlineData(EnumDilutionPlatePositionError.CouldNotPickupDilutionPlates, ReagentPositionStatus.DilutionPlateNotPickedUp)]
    public async Task Handle_MapsErrorToStatus_Correctly(
        EnumDilutionPlatePositionError error,
        ReagentPositionStatus expectedStatus)
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var dilutionPlateStacks = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 5, error)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber1))
            .Returns(dilutionPlateStacks);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(1);
        result.Items[0].Items[0].Status.Should().Be(expectedStatus);
        result.Items[0].Items[0].DrawerPosition.Should().Be(1);
        result.Items[0].Items[0].DilutionPlates.Should().Be(5);
        result.Items[0].Items[0].Instrument.Should().Be(_testInstrument1);
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4928")]
    [Trait("ProductRequirement", "PR_1662")]
    [Trait("ModuleRequirement", "MR_4939")]
    [Fact]
    public async Task Handle_AggregatesDataFromMultipleInstruments()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1),
            CreateInstrument(_testInstrument2, _testSerialNumber2)
        };

        var stacks1 = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 3),
            CreateDilutionPlateStack(2, 2)
        };

        var stacks2 = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 4)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber1))
            .Returns(stacks1);
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber2))
            .Returns(stacks2);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items[0].Items.Should().HaveCount(3);
        result.Items[0].Items.Where(x => x.Instrument == _testInstrument1).Should().HaveCount(2);
        result.Items[0].Items.Where(x => x.Instrument == _testInstrument2).Should().HaveCount(1);
        result.Items[0].PlatesCount.Should().Be(9);
        result.Items[0].WellsCount.Should().Be(9 * Constants.MaxDilutionPlateWells);
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4936")]
    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4928")]
    [Fact]
    public async Task Handle_FiltersInstruments_WhenSelectedInstrumentsProvided()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1),
            CreateInstrument(_testInstrument2, _testSerialNumber2)
        };

        var stacks1 = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 3)
        };

        var stacks2 = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 4)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber1))
            .Returns(stacks1);
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber2))
            .Returns(stacks2);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
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
        result.Items[0].PlatesCount.Should().Be(3);
        reagentStorage.Verify(x => x.GetDilutionPlatesDrawer(_testSerialNumber1), Times.Once);
        reagentStorage.Verify(x => x.GetDilutionPlatesDrawer(_testSerialNumber2), Times.Never);
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
            .Setup(x => x.GetDilutionPlatesDrawer(It.IsAny<string>()))
            .Returns([CreateDilutionPlateStack(1, 1)]);

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedInstruments = null
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(2);
        reagentStorage.Verify(x => x.GetDilutionPlatesDrawer(_testSerialNumber1), Times.Once);
        reagentStorage.Verify(x => x.GetDilutionPlatesDrawer(_testSerialNumber2), Times.Once);
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
            .Setup(x => x.GetDilutionPlatesDrawer(It.IsAny<string>()))
            .Returns([CreateDilutionPlateStack(1, 1)]);

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedInstruments = []
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(2);
        reagentStorage.Verify(x => x.GetDilutionPlatesDrawer(_testSerialNumber1), Times.Once);
        reagentStorage.Verify(x => x.GetDilutionPlatesDrawer(_testSerialNumber2), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4937")]
    [Fact]
    public async Task Handle_SortsDefaultByInstrumentThenPosition()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument("Zebra", "SN003"),
            CreateInstrument("Alpha", "SN001")
        };

        var stacks1 = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(3, 1),
            CreateDilutionPlateStack(1, 2)
        };

        var stacks2 = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(2, 3)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer("SN003"))
            .Returns(stacks1);
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer("SN001"))
            .Returns(stacks2);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(3);
        result.Items[0].Items[0].Instrument.Should().Be("Alpha");
        result.Items[0].Items[0].DrawerPosition.Should().Be(2);
        result.Items[0].Items[1].Instrument.Should().Be("Zebra");
        result.Items[0].Items[1].DrawerPosition.Should().Be(1);
        result.Items[0].Items[2].Instrument.Should().Be("Zebra");
        result.Items[0].Items[2].DrawerPosition.Should().Be(3);
    }

    [Trait("ProductRequirement", "PR_2125")]
    [Trait("ModuleRequirement", "MR_4937")]
    [Theory]
    [InlineData(nameof(DrawerDilutionPlateDto.DrawerPosition), true)]
    [InlineData(nameof(DrawerDilutionPlateDto.DrawerPosition), false)]
    [InlineData(nameof(DrawerDilutionPlateDto.DilutionPlates), true)]
    [InlineData(nameof(DrawerDilutionPlateDto.DilutionPlates), false)]
    [InlineData(nameof(DrawerDilutionPlateDto.Status), true)]
    [InlineData(nameof(DrawerDilutionPlateDto.Status), false)]
    [InlineData(nameof(DrawerDilutionPlateDto.Instrument), true)]
    [InlineData(nameof(DrawerDilutionPlateDto.Instrument), false)]
    public async Task Handle_SortsBySpecifiedProperty(string sortProperty, bool ascending)
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument("Inst2", "SN002"),
            CreateInstrument("Inst1", "SN001")
        };

        var stacks1 = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(2, 5),
            CreateDilutionPlateStack(1, 3, EnumDilutionPlatePositionError.CouldNotDetectDilutionPlates)
        };

        var stacks2 = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 7)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer("SN001"))
            .Returns(stacks1);
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer("SN002"))
            .Returns(stacks2);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
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

        if (sortProperty == nameof(DrawerDilutionPlateDto.DrawerPosition))
        {
            var positions = result.Items[0].Items.Select(x => x.DrawerPosition).ToList();
            if (ascending)
            {
                positions.Should().BeInAscendingOrder();
            }
            else
            {
                positions.Should().BeInDescendingOrder();
            }
        }
        else if (sortProperty == nameof(DrawerDilutionPlateDto.DilutionPlates))
        {
            var plates = result.Items[0].Items.Select(x => x.DilutionPlates).ToList();
            if (ascending)
            {
                plates.Should().BeInAscendingOrder();
            }
            else
            {
                plates.Should().BeInDescendingOrder();
            }
        }
        else if (sortProperty == nameof(DrawerDilutionPlateDto.Status))
        {
            var statuses = result.Items[0].Items.Select(x => x.Status).ToList();
            if (ascending)
            {
                statuses.Should().BeInAscendingOrder();
            }
            else
            {
                statuses.Should().BeInDescendingOrder();
            }
        }
        else if (sortProperty == nameof(DrawerDilutionPlateDto.Instrument))
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

        var stacks = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 1),
            CreateDilutionPlateStack(2, 2),
            CreateDilutionPlateStack(3, 3),
            CreateDilutionPlateStack(4, 4),
            CreateDilutionPlateStack(5, 5)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber1))
            .Returns(stacks);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
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
        result.Items[0].Items[0].DrawerPosition.Should().Be(2);
        result.Items[0].Items[1].DrawerPosition.Should().Be(3);
    }

    [Trait("ProductRequirement", "PR_1662")]
    [Trait("ModuleRequirement", "MR_4939")]
    [Fact]
    public async Task Handle_CalculatesPlatesCountAndWellsCount_FromPagedResults()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var stacks = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 10),
            CreateDilutionPlateStack(2, 20),
            CreateDilutionPlateStack(3, 30)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber1))
            .Returns(stacks);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 2
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items[0].Items.Should().HaveCount(2);
        result.Items[0].PlatesCount.Should().Be(30);
        result.Items[0].WellsCount.Should().Be(30 * Constants.MaxDilutionPlateWells);
    }

    [Fact]
    public async Task Handle_GeneratesUniqueIds_ForEachStackDto()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var stacks = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 5),
            CreateDilutionPlateStack(2, 5),
            CreateDilutionPlateStack(3, 5, EnumDilutionPlatePositionError.CouldNotDetectDilutionPlates)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber1))
            .Returns(stacks);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(3);
        var ids = result.Items[0].Items.Select(x => x.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_GeneratesResultId_BasedOnContent()
    {
        // Arrange
        var instruments = new List<IInstrument>
        {
            CreateInstrument(_testInstrument1, _testSerialNumber1)
        };

        var stacks = new List<DilutionPlateStack>
        {
            CreateDilutionPlateStack(1, 3)
        };

        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService
            .Setup(x => x.GetInstruments())
            .Returns(instruments);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetDilutionPlatesDrawer(_testSerialNumber1))
            .Returns(stacks);

        var handler = CreateHandler(instrumentService.Object, reagentStorage.Object);
        var query = new GetDrawerDilutionPlatesQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Id.Should().NotBe(0);
        result.Items[0].Id.Should().Be(HashCode.Combine(1, 1));
    }

    private static GetDrawerDilutionPlatesQueryHandler CreateHandler(
        IInstrumentManager instrumentService,
        IInstrumentReagentStorage reagentStorage)
    {
        return new GetDrawerDilutionPlatesQueryHandler(instrumentService, reagentStorage);
    }

    private static TestInstrument CreateInstrument(string displayName, string serialNumber)
    {
        return new TestInstrument(serialNumber, InstrumentType.Medium, displayName);
    }

    private static DilutionPlateStack CreateDilutionPlateStack(
        int position,
        int numberOfPlates,
        EnumDilutionPlatePositionError error = EnumDilutionPlatePositionError.UnspecifiedDilutionPlatePositionError)
    {
        return new DilutionPlateStack
        {
            Position = position,
            NumberOfPlates = numberOfPlates,
            Error = error
        };
    }
}

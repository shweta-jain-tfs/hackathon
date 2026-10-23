using System.Drawing;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.LoadList;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Localization;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;
using static Pharos.Server.Application.Reagents.BarcodeFormat;
using LotInformation = Pharos.Server.Application.Reagents.LotInformation;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetCarrierQueryHandlerTests
{
    private static GetCarrierQueryHandler CreateHandler(
        IInstrumentManager instrumentManager,
        IReagentBarcodeService reagentBarcodeService,
        IInstrumentReagentStorage instrumentReagentStorage,
        ISender? sender = null)
    {
        return new GetCarrierQueryHandler(
            instrumentManager,
            reagentBarcodeService,
            instrumentReagentStorage,
            sender ?? CreateSender());
    }

    private static ISender CreateSender(IReadOnlyList<CarrierRefillLevelConfigurationDto>? configurations = null)
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<GetCarrierRefillLevelConfigurationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configurations ?? []);
        return sender.Object;
    }

    private static CarrierInformation CreateCarrier(string barcode, string udi, EnumCarrierError error, int position, int remaining = 1, int reservedStarted = 0, int reservedScheduled = 0, bool unloadPending = false)
    {
        return new CarrierInformation
        {
            ReagentBarcode = barcode,
            ReagentUdiBarcode = udi,
            Error = error,
            Content = new ReagentContent
            {
                RemainingQuantity = remaining,
                ReservedStartedQuantity = reservedStarted,
                ReservedScheduledQuantity = reservedScheduled,
                ExpiryDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(5))
            },
            Position = new CarrierPosition { PositionInternalStorage = position },
            UnloadPending = unloadPending
        };
    }

    private static ReagentInfo CreateReagentInfo(
        string identity = "TestA",
        string lot = "Lot1",
        string articleNumber = "AAA")
    {
        return new ReagentInfo
        {
            Article = new Article
            {
                Shape = ArticleIndicatorShape.Round,
                Color1 = Color.Red,
                Identity = identity,
                ArticleNumber = articleNumber,
                ArticleType = ArticleType.ImmunoCapCarrier
            },
            Lot = new LotInformation { LotNumber = lot, ExpiryDate = DateTimeOffset.UtcNow.AddDays(10) }
        };
    }

    [Trait("ProductRequirement", "PR_2124")]
    [Trait("ModuleRequirement", "MR_4866")]
    [Theory]
    [InlineData(EnumCarrierError.CarrierGripError, ReagentPositionStatus.TransferNotPossible)]
    [InlineData(EnumCarrierError.CarrierUnreadableBarcodeError, ReagentPositionStatus.UnreadableBarcode)]
    [InlineData(EnumCarrierError.CarrierCapDispenseError, ReagentPositionStatus.DispenseError)]
    [InlineData(EnumCarrierError.CarrierExpiredError, ReagentPositionStatus.Expired)]
    [InlineData(EnumCarrierError.CarrierNoError, ReagentPositionStatus.Available)]
    public async Task Handle_MapsErrorToStatus_Theory(EnumCarrierError error, ReagentPositionStatus expected)
    {
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());
        var carrier = CreateCarrier("BC", "UDI", error, 1);
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(new List<CarrierInformation> { carrier });
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Status.Should().Be(expected);
    }

    [Trait("ProductRequirement", "PR_2124")]
    [Trait("ModuleRequirement", "MR_4866")]
    [Theory]
    [InlineData(true, ReagentPositionStatus.Unloading)]
    [InlineData(false, ReagentPositionStatus.Available)]
    public async Task Handle_CheckRemainingStatus_Theory(bool unloadPending, ReagentPositionStatus expected)
    {
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());
        var carrier = CreateCarrier("BC", "UDI", EnumCarrierError.CarrierNoError, 1, unloadPending: unloadPending);
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(new List<CarrierInformation> { carrier });
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Status.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyResult_WhenNoInstruments()
    {
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument>());
        var barcodeService = new Mock<IReagentBarcodeService>();
        var storage = new Mock<IInstrumentReagentStorage>();
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items.Count.Should().Be(1);
        result.TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    public async Task Handle_MapsAndFiltersCarriersByMissingRefillLevel(bool isRefillLevelMissing, int expectedCount)
    {
        // Arrange
        var instrumentOne = new TestInstrument("SN1", 0, "Instrument one");
        var instrumentTwo = new TestInstrument("SN2", 0, "Instrument two");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns([instrumentOne, instrumentTwo]);

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), "BC1", It.IsAny<string?>()))
            .ReturnsAsync(CreateReagentInfo("t1", articleNumber: "A01"));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), "BC2", It.IsAny<string?>()))
            .ReturnsAsync(CreateReagentInfo("t1", articleNumber: "A02"));

        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrumentOne.SerialNumber))
            .Returns([CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1)]);
        storage.Setup(x => x.GetCarriers(instrumentTwo.SerialNumber))
            .Returns([CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, 1)]);

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<GetCarrierRefillLevelConfigurationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new CarrierRefillLevelConfigurationDto("SN1", ArticleType.EliaWellCarrier, "t1", 0),
                new CarrierRefillLevelConfigurationDto("SN1", ArticleType.ImmunoCapCarrier, "t1", 1),
                new CarrierRefillLevelConfigurationDto("SN2", ArticleType.ImmunoCapCarrier, "t1", 0)
            ]);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object, sender.Object);

        // Act
        var result = await handler.Handle(new GetCarriersQuery
        {
            StartIndex = 0,
            Count = 10,
            IsRefillLevelMissing = isRefillLevelMissing
        }, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(expectedCount);
        result.Items[0].Items.Should().OnlyContain(item => item.Test == "t1");
        if (isRefillLevelMissing)
        {
            result.Items[0].Items.Should().OnlyContain(item => !item.IsRefillLevelDefined);
        }
        sender.Verify(x => x.Send(
            It.Is<GetCarrierRefillLevelConfigurationsQuery>(query =>
                query.InstrumentNumbers.Count == 2 &&
                query.InstrumentNumbers.Contains("SN1") &&
                query.InstrumentNumbers.Contains("SN2")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4878")]
    [Theory]
    [InlineData("TestA", "Inst1", 1, "TestA")]
    [InlineData("TestB", "Inst2", 2, "TestB")]
    public async Task Handle_AppliesFilteringAndSorting_Theory(string identity, string instrumentName, int position, string expectedTest)
    {
        var instrument = new TestInstrument("SN2", 0, instrumentName);
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo(identity));
        var carriers = new List<CarrierInformation>
        {
            CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1),
            CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, position)
        };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(carriers);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery
        {
            StartIndex = 0,
            Count = 1,
            SearchText = expectedTest,
            SelectedInstruments = new List<string> { instrumentName },
            SortByProperty = nameof(CarrierListItemDto.Position),
            SortByAscending = false
        };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items.Should().HaveCount(1);
        var dto = result.Items[0];
        dto.Items.Should().HaveCount(1);
        dto.Items[0].Test.Should().Be(expectedTest);
        dto.Items[0].Position.Should().Be(position);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    public async Task Handle_Pagination_Theory(int startIndex, int expectedCount)
    {
        var instrument = new TestInstrument("SN2", 0, "Inst2");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo("TestB"));
        var carriers = new List<CarrierInformation>
        {
            CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1),
            CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, 2)
        };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(carriers);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = startIndex, Count = 1 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Count.Should().Be(expectedCount);
    }

    [Trait("ProductRequirement", "PR_2124")]
    [Trait("ModuleRequirement", "MR_4866")]
    [Theory]
    [InlineData(EnumCarrierError.CarrierUnreadableOcrError, ReagentPositionStatus.UnreadableOcr)]
    [InlineData(EnumCarrierError.CarrierUnknownCarrierError, ReagentPositionStatus.UnknownCarrier)]
    [InlineData(EnumCarrierError.CarrierStorageFullError, ReagentPositionStatus.InternalStorageFull)]
    public async Task Handle_MapsAdditionalErrorsToStatus_Theory(EnumCarrierError error, ReagentPositionStatus expected)
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());
        var carrier = CreateCarrier("BC", "UDI", error, 1);
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(new List<CarrierInformation> { carrier });
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10 };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items[0].Status.Should().Be(expected);
    }

    [Trait("ProductRequirement", "PR_2124")]
    [Trait("ModuleRequirement", "MR_4866")]
    [Theory]
    [InlineData(1, 0, ReagentPositionStatus.Reserved)] // ReservedStarted > 0
    [InlineData(0, 1, ReagentPositionStatus.Reserved)] // ReservedScheduled > 0
    [InlineData(0, 0, ReagentPositionStatus.Empty, 0)] // Empty when remaining quantity is zero
    [InlineData(0, 0, ReagentPositionStatus.Available)] // Neither reserved
    public async Task Handle_ChecksReservedStatus_Theory(int reservedStarted, int reservedScheduled, ReagentPositionStatus expected, int remaining = 1)
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());
        var carrier = CreateCarrier("BC", "UDI", EnumCarrierError.CarrierNoError, 1, remaining: remaining, reservedStarted: reservedStarted, reservedScheduled: reservedScheduled);
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(new List<CarrierInformation> { carrier });
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10 };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items[0].Status.Should().Be(expected);
    }

    [Trait("ProductRequirement", "PR_881")]
    [Trait("ModuleRequirement", "MR_4861")]
    [Fact]
    public async Task Handle_FiltersCarriersInDrawer_OnlyIncludesInternalStorage()
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());

        var carrierInStorage = CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1); // PositionInternalStorage = 1
        var carrierInDrawer = CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, 0); // PositionInternalStorage = 0 (should be excluded)

        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(new List<CarrierInformation> { carrierInStorage, carrierInDrawer });
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10 };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(1);
        result.Items[0].Items[0].Barcode.Should().Be("BC1");
    }

    [Trait("ProductRequirement", "PR_878")]
    [Trait("ModuleRequirement", "MR_4862")]
    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4880")]
    [Fact]
    public async Task Handle_FiltersBySearchText_MatchesTestName()
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(ArticleType.ImmunoCapCarrier, "BC1", It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo("TestA", "Lot1"));
        barcodeService.Setup(x => x.GetReagentInfo(ArticleType.ImmunoCapCarrier, "BC2", It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo("TestB", "Lot2"));

        var carriers = new List<CarrierInformation>
        {
            CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1),
            CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, 2)
        };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(carriers);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10, SearchText = "TestA" };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(1);
        result.Items[0].Items[0].Test.Should().Be("TestA");
    }

    [Trait("ProductRequirement", "PR_2124")]
    [Trait("ModuleRequirement", "MR_4865")]
    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4880")]
    [Fact]
    public async Task Handle_FiltersBySearchText_MatchesLotNumber()
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(ArticleType.ImmunoCapCarrier, "BC1", It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo("TestA", "LOT123"));
        barcodeService.Setup(x => x.GetReagentInfo(ArticleType.ImmunoCapCarrier, "BC2", It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo("TestB", "LOT456"));

        var carriers = new List<CarrierInformation>
        {
            CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1),
            CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, 2)
        };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(carriers);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10, SearchText = "LOT123" };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(1);
        result.Items[0].Items[0].Lot.Should().Be("LOT123");
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4879")]
    [Fact]
    public async Task Handle_FiltersBySelectedStatus_Reserved()
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());

        var carriers = new List<CarrierInformation>
        {
            CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1, reservedStarted: 1), // Reserved
            CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, 2, reservedStarted: 0) // Available
        };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(carriers);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedStatuses = new List<string> { ReagentPositionStatus.Reserved.ToString() }
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(1);
        result.Items[0].Items[0].Status.Should().Be(ReagentPositionStatus.Reserved);
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4879")]
    [Fact]
    public async Task Handle_FiltersBySelectedStatus_Unloading()
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());

        var carriers = new List<CarrierInformation>
        {
            CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1, unloadPending: true), // Unloading
            CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, 2, unloadPending: false) // Available
        };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(carriers);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedStatuses = new List<string> { ReagentPositionStatus.Unloading.ToString() }
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(1);
        result.Items[0].Items[0].Status.Should().Be(ReagentPositionStatus.Unloading);
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4879")]
    [Fact]
    public async Task Handle_FiltersBySelectedStatus_ErrorFilter()
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());

        var carriers = new List<CarrierInformation>
        {
            CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierGripError, 1), // TransferNotPossible (error)
            CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierExpiredError, 2), // Expired (error)
            CreateCarrier("BC3", "UDI3", EnumCarrierError.CarrierNoError, 3) // Available (not error)
        };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(carriers);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedStatuses = new List<string> { Translations.Inventory_ErrorFilter }
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(2);
        result.Items[0].Items.Should().OnlyContain(x =>
            x.Status == ReagentPositionStatus.TransferNotPossible ||
            x.Status == ReagentPositionStatus.Expired);
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4879")]
    [Fact]
    public async Task Handle_FiltersBySelectedStatus_Empty()
    {
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());

        var carriers = new List<CarrierInformation>
        {
            CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1, remaining: 0),
            CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, 2, remaining: 1)
        };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(carriers);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedStatuses = new List<string> { ReagentPositionStatus.Empty.ToString() }
        };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items[0].Items.Should().HaveCount(1);
        result.Items[0].Items[0].Status.Should().Be(ReagentPositionStatus.Empty);
    }

    [Fact]
    public async Task Handle_UnloadingTakesPrecedenceOverEmpty()
    {
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());
        var carrier = CreateCarrier("BC", "UDI", EnumCarrierError.CarrierNoError, 1, remaining: 0, unloadPending: true);
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(new List<CarrierInformation> { carrier });
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);

        var result = await handler.Handle(new GetCarriersQuery { StartIndex = 0, Count = 10 }, TestContext.Current.CancellationToken);

        result.Items[0].Items[0].Status.Should().Be(ReagentPositionStatus.Unloading);
    }

    [Trait("ProductRequirement", "PR_2124")]
    [Trait("ModuleRequirement", "MR_4881")]
    [Theory]
    [InlineData(nameof(CarrierListItemDto.Test), true)]
    [InlineData(nameof(CarrierListItemDto.Instrument), false)]
    [InlineData(nameof(CarrierListItemDto.Type), true)]
    [InlineData(nameof(CarrierListItemDto.Quantity), false)]
    [InlineData(nameof(CarrierListItemDto.Lot), true)]
    [InlineData(nameof(CarrierListItemDto.Status), false)]
    [InlineData(nameof(CarrierListItemDto.ExpiryDate), true)]
    public async Task Handle_SortsByProperty_Theory(string sortProperty, bool ascending)
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(ArticleType.ImmunoCapCarrier, "BC1", It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo("TestA", "Lot1"));
        barcodeService.Setup(x => x.GetReagentInfo(ArticleType.ImmunoCapCarrier, "BC2", It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo("TestB", "Lot2"));

        var carriers = new List<CarrierInformation>
        {
            CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 2),
            CreateCarrier("BC2", "UDI2", EnumCarrierError.CarrierNoError, 1)
        };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(carriers);
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery
        {
            StartIndex = 0,
            Count = 10,
            SortByProperty = sortProperty,
            SortByAscending = ascending
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().HaveCount(2);
    }

    [Trait("ProductRequirement", "PR_2124")]
    [Trait("ModuleRequirement", "MR_4882")]
    [Fact]
    public async Task Handle_CalculatesFreePositions_WithSelectedInstruments()
    {
        // Arrange
        var inst1 = new TestInstrument("SN1", 0, "Inst1");
        var inst2 = new TestInstrument("SN2", 0, "Inst2");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { inst1, inst2 });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());

        var carriers = new List<CarrierInformation> { CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1) };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers("SN1")).Returns(carriers);
        storage.Setup(x => x.GetCarriers("SN2")).Returns(new List<CarrierInformation>());

        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery
        {
            StartIndex = 0,
            Count = 10,
            SelectedInstruments = new List<string> { "Inst1" }
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].TotalPositions.Should().Be(Constants.MaxCarrierStoragePositions);
        result.Items[0].FreePositions.Should().Be(Constants.MaxCarrierStoragePositions - 1);
    }

    [Trait("ProductRequirement", "PR_2124")]
    [Trait("ModuleRequirement", "MR_4882")]
    [Fact]
    public async Task Handle_CalculatesFreePositions_WithoutSelectedInstruments()
    {
        // Arrange
        var inst1 = new TestInstrument("SN1", 0, "Inst1");
        var inst2 = new TestInstrument("SN2", 0, "Inst2");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { inst1, inst2 });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo());

        var carriers = new List<CarrierInformation> { CreateCarrier("BC1", "UDI1", EnumCarrierError.CarrierNoError, 1) };
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers("SN1")).Returns(carriers);
        storage.Setup(x => x.GetCarriers("SN2")).Returns(new List<CarrierInformation>());

        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10 };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].TotalPositions.Should().Be(2 * Constants.MaxCarrierStoragePositions);
        result.Items[0].FreePositions.Should().Be((2 * Constants.MaxCarrierStoragePositions) - 1);
    }

    [Trait("ProductRequirement", "PR_2124")]
    [Trait("ModuleRequirement", "MR_4867")]
    [Fact]
    public async Task Handle_UsesCarrierExpiryDate_WhenEarlierThanLotExpiryDate()
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));

        var lotExpiryDate = DateTimeOffset.UtcNow.AddDays(10);
        var carrierExpiryDate = DateTime.UtcNow.AddDays(5); // Earlier
        var reagentInfo = new ReagentInfo
        {
            Article = new Article { Shape = ArticleIndicatorShape.Round, Color1 = Color.Red, Identity = "Test", ArticleType = ArticleType.ImmunoCapCarrier },
            Lot = new LotInformation { LotNumber = "Lot1", ExpiryDate = lotExpiryDate }
        };
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(reagentInfo);

        var carrier = new CarrierInformation
        {
            ReagentBarcode = "BC",
            ReagentUdiBarcode = "UDI",
            Error = EnumCarrierError.CarrierNoError,
            Content = new ReagentContent
            {
                RemainingQuantity = 1,
                ExpiryDate = Timestamp.FromDateTime(carrierExpiryDate)
            },
            Position = new CarrierPosition { PositionInternalStorage = 1 },
            UnloadPending = false
        };

        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(new List<CarrierInformation> { carrier });
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10 };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items[0].ExpiryDate.Should().Be(carrierExpiryDate);
    }

    [Fact]
    public async Task Handle_SkipsCarrier_WhenReagentInfoIsNull()
    {
        // Arrange
        var instrument = new TestInstrument("SN1", 0, "Inst1");
        var instrumentService = new Mock<IInstrumentManager>();
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument });
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.ImmunoCapCarrier, Primary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync((ReagentInfo?)null);

        var carrier = CreateCarrier("BC", "UDI", EnumCarrierError.CarrierNoError, 1);
        var storage = new Mock<IInstrumentReagentStorage>();
        storage.Setup(x => x.GetCarriers(instrument.SerialNumber)).Returns(new List<CarrierInformation> { carrier });
        var handler = CreateHandler(instrumentService.Object, barcodeService.Object, storage.Object);
        var query = new GetCarriersQuery { StartIndex = 0, Count = 10 };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items[0].Items.Should().BeEmpty();
    }
}


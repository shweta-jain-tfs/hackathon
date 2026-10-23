using System.Drawing;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Localization;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Tetris.Protocol.Pharos.Storage;
using static Pharos.Server.Application.Reagents.BarcodeFormat;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetCartridgesQueryHandlerTests
{
    private static Cartridge CreateCartridge(string barcode, EnumCartridgeError error, int position, EnumCartridgeArea area, List<CartridgeCell>? cells = null, bool unloadPending = false)
    {
        return new Cartridge
        {
            ReagentBarcode = barcode,
            Error = error,
            Position = new CartridgePosition { PositionInternalStorage = position, Area = area },
            Cells = { cells ?? new List<CartridgeCell>() },
            UnloadPending = unloadPending
        };
    }

    private static CartridgeCell CreateCell(int remaining, int reservedStarted = 0, int reservedScheduled = 0, Timestamp? open = null, Timestamp? expiry = null)
    {
        return new CartridgeCell
        {
            Content = new Tetris.Protocol.Common.ReagentContent
            {
                RemainingQuantity = remaining,
                ReservedStartedQuantity = reservedStarted,
                ReservedScheduledQuantity = reservedScheduled,
                OpenTimestamp = open ?? Timestamp.FromDateTime(DateTime.UtcNow),
                ExpiryDate = expiry ?? Timestamp.FromDateTime(DateTime.UtcNow.AddDays(10))
            }
        };
    }

    private static ReagentInfo CreateReagentInfo(ArticleType type, string lot, DateTime expiry, string name = "TestName", List<string?>? texts = null, List<Color?>? colors = null)
    {
        var article = new Article
        {
            ArticleType = type,
            FriendlyName = name,
            Text1 = texts != null && texts.Count > 0 ? texts[0] : "text1",
            Text2 = texts != null && texts.Count > 1 ? texts[1] : "text2",
            Text3 = texts != null && texts.Count > 2 ? texts[2] : "text3",
            Text4 = texts != null && texts.Count > 3 ? texts[3] : "text4",
            Color1 = colors != null && colors.Count > 0 ? colors[0] : null,
            Color2 = colors != null && colors.Count > 1 ? colors[1] : null,
            Color3 = colors != null && colors.Count > 2 ? colors[2] : null,
            Color4 = colors != null && colors.Count > 3 ? colors[3] : null,
            Shape = type switch
            {
                ArticleType.ImmunoCapCarrier => ArticleIndicatorShape.Round,
                ArticleType.ConjugateCartridge => ArticleIndicatorShape.Rectangular,
                ArticleType.QualityControlCartridge => ArticleIndicatorShape.Rectangular,
                ArticleType.CalibratorCartridge => ArticleIndicatorShape.Slanted,
                ArticleType.CurveControlCartridge => ArticleIndicatorShape.Slanted,
                _ => ArticleIndicatorShape.Round
            }
        };

        return new ReagentInfo
        {
            Article = article,
            Lot = new LotInformation
            {
                LotNumber = lot,
                ExpiryDate = expiry
            }
        };
    }

    private static TestInstrument CreateInstrument(string displayName, string serial)
    {
        return new TestInstrument(displayName, InstrumentType.Medium, serial);
    }

    private static Mock<IInstrumentManager> SetupInstrumentService(params IInstrument[] instruments)
    {
        var mock = new Mock<IInstrumentManager>();
        mock.Setup(x => x.GetInstruments()).Returns(instruments.ToList());
        return mock;
    }

    private static Mock<IInstrumentReagentStorage> SetupStorage(string serial, List<Cartridge> cartridges)
    {
        var mock = new Mock<IInstrumentReagentStorage>();
        mock.Setup(x => x.GetCartridges(serial)).Returns(cartridges);
        return mock;
    }

    private static Mock<IReagentBarcodeService> SetupBarcodeService(Dictionary<string, ReagentInfo?> barcodeMap)
    {
        var mock = new Mock<IReagentBarcodeService>();
        mock.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((string barcode) =>
        {
            if (barcodeMap.TryGetValue(barcode, out var reagentInfo) && reagentInfo != null)
            {
                return (reagentInfo.Article.ArticleType, Primary);
            }
            return (ArticleType.None, null);
        });
        foreach (var kvp in barcodeMap)
        {
            mock.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), kvp.Key, It.IsAny<string?>())).ReturnsAsync(kvp.Value);
        }
        mock.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((ArticleType _, string? primaryBarcode, string? secondaryBarcode) =>
            {
                var barcode = primaryBarcode ?? secondaryBarcode;
                if (barcode != null && barcodeMap.TryGetValue(barcode, out var reagentInfo))
                {
                    return reagentInfo;
                }
                return null;
            });
        return mock;
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4902")]
    [Theory]
    [InlineData("Intr3", 0)]
    [InlineData("Inst2", 1)]
    public async Task Handle_FiltersBySelectedInstrument_Theory(string selectedInstrument, int expectedCount)
    {
        var instr1 = CreateInstrument("Inst1", "Inst1");
        var instr2 = CreateInstrument("Inst2", "Inst2");
        var instrumentService = SetupInstrumentService(instr1, instr2);
        var reagentStorage = SetupStorage("Inst1", new List<Cartridge>());
        reagentStorage.Setup(x => x.GetCartridges("Inst2")).Returns(new List<Cartridge> { CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea) });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SelectedInstruments = selectedInstrument == null ? null : new List<string> { selectedInstrument }, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Count.Should().Be(expectedCount);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4889")]
    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4903")]
    [Theory]
    [InlineData(ReagentPositionStatus.Reserved, 1)]
    [InlineData(ReagentPositionStatus.Available, 0)]
    [InlineData(ReagentPositionStatus.Empty, 1)]
    public async Task Handle_FiltersByStatus_Theory(ReagentPositionStatus status, int expectedCount)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cells = status == ReagentPositionStatus.Empty
            ? new List<CartridgeCell> { CreateCell(0) }
            : new List<CartridgeCell> { CreateCell(0, 1) };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.RearStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.QualityControlCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SelectedStatuses = new List<string> { status.ToString() }, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Count.Should().Be(expectedCount);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4905")]
    [Theory]
    [InlineData("B1", "B1")]
    [InlineData("B2", "B2")]
    public async Task Handle_SortsByProperty_Theory(string name, string expected)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2 });
        var barcodeService = SetupBarcodeService(new()
        {
            [name] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5), name),
            [name] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L2", DateTime.UtcNow.AddDays(5), name)
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SortByProperty = nameof(CartridgeListItemDto.Name), SortByAscending = true, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.First().Name.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 5, 5)]
    [InlineData(5, 5, 5)]
    public async Task Handle_PaginatesResults_Theory(int startIndex, int count, int expected)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var carts = Enumerable.Range(0, 20).Select(i => CreateCartridge($"B{i}", EnumCartridgeError.CartridgeNoError, i, EnumCartridgeArea.FrontStorageArea)).ToList();
        var reagentStorage = SetupStorage("Inst", carts);
        var barcodeService = SetupBarcodeService(carts.ToDictionary(c => c.ReagentBarcode, c => (ReagentInfo?)CreateReagentInfo(ArticleType.ConjugateCartridge, "L", DateTime.UtcNow.AddDays(5))));
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = startIndex, Count = count };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Count.Should().Be(expected);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4885")]
    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4904")]
    [Theory]
    [InlineData("Text1", "text1/text2/text3/text4")]
    public async Task Handle_FiltersBySearchText_Theory(string searchText, string expectedMethod)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5), "B1", new List<string?> { "text1", "text2", "text3", "text4" })
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SearchText = searchText, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Method.Should().Contain(expectedMethod);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4889")]
    [Theory]
    [InlineData(EnumCartridgeError.UnreadableCartridgeBarcodeError, ReagentPositionStatus.UnreadableBarcode)]
    [InlineData(EnumCartridgeError.UnknownCartridgeError, ReagentPositionStatus.UnknownCartridge)]
    [InlineData(EnumCartridgeError.CartridgeStorageFullError, ReagentPositionStatus.CartridgeStorageFull)]
    [InlineData(EnumCartridgeError.CartridgeGripError, ReagentPositionStatus.TransferNotPossible)]
    [InlineData(EnumCartridgeError.CartridgeAspirationError, ReagentPositionStatus.AspirationError)]
    [InlineData(EnumCartridgeError.CartridgeExpiredError, ReagentPositionStatus.Expired)]
    public async Task Handle_MapsErrorToStatus_Theory(EnumCartridgeError error, ReagentPositionStatus expectedStatus)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", error, 1, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Status.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(EnumCartridgeError.UnspecifiedCartridgeError)]
    public async Task Handle_ThrowsOnUnspecifiedCartridgeError_Theory(EnumCartridgeError error)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", error, 1, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(query, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    public async Task Handle_ReturnsEmpty_WhenNoInstruments_Theory(string? instrumentName)
    {
        var instrumentService = SetupInstrumentService();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SelectedInstruments = instrumentName == null ? null : new List<string> { instrumentName }, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4893")]
    [Theory]
    [InlineData(EnumCartridgeArea.FrontStorageArea, CartridgeStorageArea.FrontStorage)]
    [InlineData(EnumCartridgeArea.RearStorageArea, CartridgeStorageArea.BackStorage)]
    [InlineData(EnumCartridgeArea.ReagentDrawerArea, CartridgeStorageArea.ReagentDrawer)]
    public async Task Handle_MapsStorageAreaCorrectly_Theory(EnumCartridgeArea area, CartridgeStorageArea expectedArea)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, area);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        if (area == EnumCartridgeArea.ReagentDrawerArea)
        {
            result.Items[0].Items.Should().BeEmpty();
        }
        else
        {
            result.Items[0].Items[0].StorageArea.Should().Be(expectedArea);
        }
    }

    [Theory]
    [InlineData(ArticleType.ImmunoCapCarrier, ArticleIndicatorShape.Round)]
    [InlineData(ArticleType.ConjugateCartridge, ArticleIndicatorShape.Rectangular)]
    [InlineData(ArticleType.QualityControlCartridge, ArticleIndicatorShape.Rectangular)]
    [InlineData(ArticleType.CalibratorCartridge, ArticleIndicatorShape.Slanted)]
    [InlineData(ArticleType.CurveControlCartridge, ArticleIndicatorShape.Slanted)]
    public async Task Handle_MapsArticleTypeToShape_Theory(ArticleType type, ArticleIndicatorShape expectedShape)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(type, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Shape.Should().Be(expectedShape);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4906")]
    [Fact]
    public async Task Handle_CalculatesFreePositionsFront_Correctly()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L2", DateTime.UtcNow.AddDays(5))
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].FreePositionsFront.Should().Be(result.Items[0].TotalPositionsFront - 2);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4906")]
    [Fact]
    public async Task Handle_CalculatesFreePositionsRear_Correctly()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.RearStorageArea);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.RearStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.QualityControlCartridge, "L1", DateTime.UtcNow.AddDays(5)),
            ["B2"] = CreateReagentInfo(ArticleType.QualityControlCartridge, "L2", DateTime.UtcNow.AddDays(5))
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].FreePositionsRear.Should().Be(result.Items[0].TotalPositionsRear - 2);
    }

    [Fact]
    public async Task Handle_UnloadingStatus_WhenUnloadPendingIsTrue()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, null, true);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Status.Should().Be(ReagentPositionStatus.Unloading);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4889")]
    [Fact]
    public async Task Handle_ReservedStatus_WhenReservedScheduledQuantityGreaterThanZero()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, new List<CartridgeCell> { CreateCell(100, 0, 10) });
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Status.Should().Be(ReagentPositionStatus.Reserved);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4887")]
    [Fact]
    public async Task Handle_CalculatesQuantity_SumsAllCells()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cells = new List<CartridgeCell> { CreateCell(50), CreateCell(30), CreateCell(20) };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Quantity.Value.Should().Be(100);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4890")]
    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4891")]
    [Fact]
    public async Task Handle_ExpiryDate_UsesCellExpiryWhenEarlier()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var earlierDate = DateTime.UtcNow.AddDays(3);
        var laterDate = DateTime.UtcNow.AddDays(10);
        var cells = new List<CartridgeCell> { CreateCell(50, expiry: Timestamp.FromDateTime(earlierDate), open: Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc))) };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", laterDate) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].ExpiryDate.Should().BeCloseTo(earlierDate, TimeSpan.FromSeconds(1));
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4890")]
    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4891")]
    [Fact]
    public async Task Handle_ExpiryDate_UsesReagentInfoExpiryWhenEarlier()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var earlierDate = DateTime.UtcNow.AddDays(3);
        var laterDate = DateTime.UtcNow.AddDays(10);
        var cells = new List<CartridgeCell> { CreateCell(50, expiry: Timestamp.FromDateTime(laterDate)) };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", earlierDate) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].ExpiryDate.Should().BeCloseTo(earlierDate, TimeSpan.FromSeconds(1));
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4891")]
    [Theory]
    [InlineData(ArticleType.CalibratorCartridge)]
    [InlineData(ArticleType.CurveControlCartridge)]
    public async Task Handle_ExpiryDateOpenCell_NoPiercedCellForCalibratorAndCurveControl_Theory(ArticleType type)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cells = new List<CartridgeCell> { CreateCell(50, open: Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1))) };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(type, "L1", DateTime.UtcNow.AddDays(10)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].ExpiryDateOpenCell.HasPiercedCell.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4891")]
    [Fact]
    public async Task Handle_ExpiryDateOpenCell_HasPiercedCell_WhenCellIsOpen()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var openDate = DateTime.UtcNow.AddDays(-1);
        var expiryDate = DateTime.UtcNow.AddDays(5);
        var cells = new List<CartridgeCell> { CreateCell(50, open: Timestamp.FromDateTime(openDate), expiry: Timestamp.FromDateTime(expiryDate)) };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(10)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].ExpiryDateOpenCell.HasPiercedCell.Should().BeTrue();
        result.Items[0].Items[0].ExpiryDateOpenCell.ExpiryDate.Should().BeCloseTo(expiryDate, TimeSpan.FromSeconds(1));
        result.Items[0].Items[0].ExpiryDateOpenCell.Quantity!.Value.Should().Be(50);
    }

    [Fact]
    public async Task Handle_ExpiryDateOpenCell_NoPiercedCell_WhenAllCellsClosed()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cells = new List<CartridgeCell> { CreateCell(50, open: Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc))) };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(10)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].ExpiryDateOpenCell.HasPiercedCell.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExpiryDateOpenCell_SumsQuantityOfPiercedCells()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var openDate = DateTime.UtcNow.AddDays(-1);
        var cells = new List<CartridgeCell>
        {
            CreateCell(30, open: Timestamp.FromDateTime(openDate)),
            CreateCell(20, open: Timestamp.FromDateTime(openDate)),
            CreateCell(50, open: Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)))
        };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(10)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].ExpiryDateOpenCell.Quantity!.Value.Should().Be(50);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4893")]
    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4905")]
    [Theory]
    [InlineData(true, CartridgeStorageArea.FrontStorage, 1)]
    [InlineData(false, CartridgeStorageArea.FrontStorage, 2)]
    public async Task Handle_SortsByPosition_Theory(bool ascending, CartridgeStorageArea expectedFirstArea, int expectedFirstPosition)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.FrontStorageArea);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L2", DateTime.UtcNow.AddDays(5))
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SortByProperty = nameof(CartridgeListItemDto.Position), SortByAscending = ascending, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].StorageArea.Should().Be(expectedFirstArea);
        result.Items[0].Items[0].Position.Should().Be(expectedFirstPosition);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4887")]
    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4905")]
    [Theory]
    [InlineData(true, 20)]
    [InlineData(false, 100)]
    public async Task Handle_SortsByQuantity_Theory(bool ascending, int expectedFirstQuantity)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, new List<CartridgeCell> { CreateCell(100) });
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.FrontStorageArea, new List<CartridgeCell> { CreateCell(20) });
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L2", DateTime.UtcNow.AddDays(5))
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SortByProperty = nameof(CartridgeListItemDto.Quantity), SortByAscending = ascending, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Quantity.Value.Should().Be(expectedFirstQuantity);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4888")]
    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4904")]
    [Fact]
    public async Task Handle_SearchByLot_FindsExactMatch()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "LOT123", DateTime.UtcNow.AddDays(5)),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "LOT456", DateTime.UtcNow.AddDays(5))
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SearchText = "LOT123", StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Count.Should().Be(1);
        result.Items[0].Items[0].Lot.Should().Be("LOT123");
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4886")]
    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4904")]
    [Fact]
    public async Task Handle_SearchByName_FindsPartialMatch()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5), "TestReagent1"),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L2", DateTime.UtcNow.AddDays(5), "OtherReagent")
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SearchText = "TestReagent", StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Count.Should().Be(1);
        result.Items[0].Items[0].Name.Should().Be("TestReagent1");
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4889")]
    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4903")]
    [Fact]
    public async Task Handle_ErrorStatusFilter_IncludesAllErrorStatuses()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeAspirationError, 1, EnumCartridgeArea.FrontStorageArea);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.UnreadableCartridgeBarcodeError, 2, EnumCartridgeArea.FrontStorageArea);
        var cart3 = CreateCartridge("B3", EnumCartridgeError.CartridgeNoError, 3, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2, cart3 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L2", DateTime.UtcNow.AddDays(5)),
            ["B3"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L3", DateTime.UtcNow.AddDays(5))
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SelectedStatuses = new List<string> { Translations.Inventory_ErrorFilter }, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Count.Should().Be(2);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4889")]
    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4903")]
    [Fact]
    public async Task Handle_MultipleStatusFilters_CombinesResults()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, null, true);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.FrontStorageArea, new List<CartridgeCell> { CreateCell(100, 10) });
        var cart3 = CreateCartridge("B3", EnumCartridgeError.CartridgeNoError, 3, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2, cart3 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L2", DateTime.UtcNow.AddDays(5)),
            ["B3"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L3", DateTime.UtcNow.AddDays(5))
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery
        {
            SelectedStatuses = new List<string> { ReagentPositionStatus.Unloading.ToString(), ReagentPositionStatus.Reserved.ToString() },
            StartIndex = 0,
            Count = 10
        };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Count.Should().Be(2);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4889")]
    [Fact]
    public async Task Handle_MapsEmptyStatus_WhenAllCellsAreEmpty()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cells = new List<CartridgeCell>
        {
            CreateCell(0, open: Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc))),
            CreateCell(0, open: Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)))
        };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);

        var result = await handler.Handle(new GetCartridgesQuery { StartIndex = 0, Count = 10 }, TestContext.Current.CancellationToken);

        result.Items[0].Items[0].Status.Should().Be(ReagentPositionStatus.Empty);
    }

    [Fact]
    public async Task Handle_ReservedTakesPrecedenceOverEmpty()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cells = new List<CartridgeCell> { CreateCell(0, reservedStarted: 1) };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);

        var result = await handler.Handle(new GetCartridgesQuery { StartIndex = 0, Count = 10 }, TestContext.Current.CancellationToken);

        result.Items[0].Items[0].Status.Should().Be(ReagentPositionStatus.Reserved);
    }

    [Fact]
    public async Task Handle_UnloadingTakesPrecedenceOverEmpty()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cells = new List<CartridgeCell> { CreateCell(0) };
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea, cells, unloadPending: true);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)) });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);

        var result = await handler.Handle(new GetCartridgesQuery { StartIndex = 0, Count = 10 }, TestContext.Current.CancellationToken);

        result.Items[0].Items[0].Status.Should().Be(ReagentPositionStatus.Unloading);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4885")]
    [Fact]
    public async Task Handle_GetMethod_CombinesNonEmptyTexts()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5), texts: new List<string?> { "Text1", null, "Text3", "" })
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Method.Should().Be("Text1/Text3");
    }

    [Fact]
    public async Task Handle_Colors_MapsCorrectly()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5), colors: new List<Color?> { Color.Red, null, Color.Blue })
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        var colors = result.Items[0].Items[0].Colors;
        colors.Should().HaveCount(4);
        colors[0].Should().Be(ColorTranslator.ToHtml(Color.Red));
        colors[1].Should().BeNull();
        colors[2].Should().Be(ColorTranslator.ToHtml(Color.Blue));
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4886")]
    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4905")]
    [Fact]
    public async Task Handle_DefaultSorting_AppliesMultipleProperties()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.FrontStorageArea);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.RearStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5), "NameB", new List<string?> { "MethodA" }),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L2", DateTime.UtcNow.AddDays(5), "NameA", new List<string?> { "MethodA" })
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Name.Should().Be("NameA");
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4888")]
    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4905")]
    [Theory]
    [InlineData(nameof(CartridgeListItemDto.Lot), true, "LOT001")]
    [InlineData(nameof(CartridgeListItemDto.Lot), false, "LOT999")]
    public async Task Handle_SortsByLot_Theory(string sortProperty, bool ascending, string expectedFirstLot)
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart1 = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var cart2 = CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 2, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart1, cart2 });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "LOT999", DateTime.UtcNow.AddDays(5)),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "LOT001", DateTime.UtcNow.AddDays(5))
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SortByProperty = sortProperty, SortByAscending = ascending, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items[0].Lot.Should().Be(expectedFirstLot);
    }

    [Trait("ProductRequirement", "PR_881")]
    [Trait("ModuleRequirement", "MR_4884")]
    [Trait("ProductRequirement", "PR_877")]
    [Trait("ModuleRequirement", "MR_4892")]
    [Fact]
    public async Task Handle_MultipleInstruments_AggregatesCartridges()
    {
        var instr1 = CreateInstrument("Inst1", "Inst1");
        var instr2 = CreateInstrument("Inst2", "Inst2");
        var instrumentService = SetupInstrumentService(instr1, instr2);
        var reagentStorage = SetupStorage("Inst1", new List<Cartridge> { CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea) });
        reagentStorage.Setup(x => x.GetCartridges("Inst2")).Returns(new List<Cartridge> { CreateCartridge("B2", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea) });
        var barcodeService = SetupBarcodeService(new()
        {
            ["B1"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L1", DateTime.UtcNow.AddDays(5)),
            ["B2"] = CreateReagentInfo(ArticleType.ConjugateCartridge, "L2", DateTime.UtcNow.AddDays(5))
        });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Count.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NullReagentInfo_SkipsCartridge()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var cart = CreateCartridge("B1", EnumCartridgeError.CartridgeNoError, 1, EnumCartridgeArea.FrontStorageArea);
        var reagentStorage = SetupStorage("Inst", new List<Cartridge> { cart });
        var barcodeService = SetupBarcodeService(new() { ["B1"] = null });
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.Items[0].Items.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4907")]
    [Fact]
    public async Task Handle_TotalCount_ReflectsFilteredResults()
    {
        var instr = CreateInstrument("Inst", "Inst");
        var instrumentService = SetupInstrumentService(instr);
        var carts = Enumerable.Range(0, 20).Select(i => CreateCartridge($"B{i}", EnumCartridgeError.CartridgeNoError, i, EnumCartridgeArea.FrontStorageArea)).ToList();
        var reagentStorage = SetupStorage("Inst", carts);
        var barcodeService = SetupBarcodeService(carts.ToDictionary(c => c.ReagentBarcode, c => (ReagentInfo?)CreateReagentInfo(ArticleType.ConjugateCartridge, "L", DateTime.UtcNow.AddDays(5), $"Name{c.Position.PositionInternalStorage}")));
        var handler = new GetCartridgesQueryHandler(instrumentService.Object, barcodeService.Object, reagentStorage.Object);
        var query = new GetCartridgesQuery { SearchText = "Name1", StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        result.TotalCount.Should().BeGreaterThan(0);
        result.TotalCount.Should().BeLessThan(20);
    }
}

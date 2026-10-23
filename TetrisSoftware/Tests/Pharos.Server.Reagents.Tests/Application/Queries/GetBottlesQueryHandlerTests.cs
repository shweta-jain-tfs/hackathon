using Google.Protobuf.WellKnownTypes;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;
using static Pharos.Server.Application.Reagents.BarcodeFormat;
using LotInformation = Pharos.Server.Application.Reagents.LotInformation;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetBottlesQueryHandlerTests
{
    private static BottledReagent CreateBottle(string udi, EnumReagentType type, int position, int remainingQuantity, EnumBottledReagentError error = EnumBottledReagentError.UnspecifiedBottledReagentError, bool inUse = false, int reservedStarted = 0, int reservedScheduled = 0, DateTime? expiryDate = null)
    {
        return new BottledReagent
        {
            ReagentUdiBarcode = udi,
            ReagentType = type,
            Position = position,
            Content = new ReagentContent
            {
                RemainingQuantity = remainingQuantity,
                ReservedStartedQuantity = reservedStarted,
                ReservedScheduledQuantity = reservedScheduled,
                ExpiryDate = Timestamp.FromDateTime((expiryDate ?? new DateTime(2025, 1, 1)).ToUniversalTime())
            },
            Error = error,
            InUse = inUse
        };
    }

    private static ReagentInfo CreateReagentInfo(string articleNumber, ArticleType articleType, string friendlyName, string lotNumber = "LOT1", DateTime? expiryDate = null)
    {
        return new ReagentInfo
        {
            Article = new Article
            {
                ArticleNumber = articleNumber,
                ArticleType = articleType,
                FriendlyName = friendlyName
            },
            Lot = new LotInformation
            {
                LotNumber = lotNumber,
                ExpiryDate = expiryDate ?? new DateTime(2025, 1, 2)
            }
        };
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4909")]
    [Theory]
    [InlineData("|01073330660041291722033110WA101|991234", EnumReagentType.WashAdditive, 1, 10, "WA1", ArticleType.WashAdditiveBottle, "B", "LOT1")]
    [InlineData("|01073330660041291722033110WC102|991234", EnumReagentType.WashConcentrate, 2, 5, "WC1", ArticleType.WashConcentrateBottle, "A", "LOT2")]
    public async Task Handle_SortByProperty_ReturnsSorted_Parameterized(string udi, EnumReagentType type, int position, int quantity, string articleNumber, ArticleType articleType, string friendlyName, string lotNumber)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var reagentBarcodeService = new Mock<IReagentBarcodeService>();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var instrumentInfo = new TestInstrument("Inst1", InstrumentType.Medium, "Inst1");
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrumentInfo });
        var bottles = new List<BottledReagent>
        {
            CreateBottle(udi, type, position, quantity)
        };
        reagentStorage.Setup(x => x.GetBottles("Inst1")).Returns(bottles);
        reagentBarcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((string barcode) =>
        {
            if (barcode.Contains("WA1"))
            {
                return (ArticleType.WashAdditiveBottle, Secondary);
            }

            if (barcode.Contains("WC1"))
            {
                return (ArticleType.WashConcentrateBottle, Secondary);
            }

            return (ArticleType.None, null);
        });
        reagentBarcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(
            CreateReagentInfo(articleNumber, articleType, friendlyName, lotNumber));
        var handler = new GetBottlesQueryHandler(
            instrumentService.Object,
            reagentBarcodeService.Object,
            reagentStorage.Object);
        var query = new GetBottlesQuery { SortByProperty = nameof(BottleListItemDto.Name), StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        Assert.Single(result.Items[0].Items);
        Assert.Equal(friendlyName, result.Items[0].Items[0].Name);
        Assert.Equal(lotNumber, result.Items[0].Items[0].Lot);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4912")]
    [Theory]
    [InlineData(EnumBottledReagentError.VolumeReadError, ReagentPositionStatus.UndetectedVolume)]
    [InlineData(EnumBottledReagentError.CannotPumpLiquid, ReagentPositionStatus.DisconnectedBottle)]
    [InlineData(EnumBottledReagentError.WrongVolume, ReagentPositionStatus.UnexpectedVolume)]
    [InlineData(EnumBottledReagentError.BottleUnknown, ReagentPositionStatus.UnknownBottle)]
    [InlineData(EnumBottledReagentError.BottleExpired, ReagentPositionStatus.Expired)]
    [InlineData(EnumBottledReagentError.UnspecifiedBottledReagentError, ReagentPositionStatus.Available)]
    public async Task Handle_MapsErrorToStatus_Theory(EnumBottledReagentError error, ReagentPositionStatus expectedStatus)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var reagentBarcodeService = new Mock<IReagentBarcodeService>();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var instrumentInfo = new TestInstrument("Inst1", InstrumentType.Medium, "Inst1");
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrumentInfo });
        var bottle = CreateBottle("|01073330660041291722033110AB103|991234", EnumReagentType.StopSolution, 1, 10, error);
        reagentStorage.Setup(x => x.GetBottles("Inst1")).Returns(new List<BottledReagent> { bottle });
        reagentBarcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.StopSolutionBottle, Secondary));
        reagentBarcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(
            CreateReagentInfo("ES3", ArticleType.StopSolutionBottle, "StopSolution"));
        var handler = new GetBottlesQueryHandler(
            instrumentService.Object,
            reagentBarcodeService.Object,
            reagentStorage.Object);
        var query = new GetBottlesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        var dto = result.Items.SelectMany(x => x.Items).FirstOrDefault();
        Assert.NotNull(dto);
        Assert.Equal(expectedStatus, dto.Status);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4912")]
    [Theory]
    [InlineData(0, 0, ReagentPositionStatus.Empty)]
    [InlineData(10, 0, ReagentPositionStatus.Available)]
    [InlineData(10, 0, ReagentPositionStatus.InUse, true)]
    [InlineData(10, 5, ReagentPositionStatus.Reserved)]
    public async Task Handle_CheckRemainingStatus_Theory(int remainingQuantity, int reservedScheduled, ReagentPositionStatus expectedStatus, bool inUse = false)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var reagentBarcodeService = new Mock<IReagentBarcodeService>();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var instrumentInfo = new TestInstrument("Inst1", InstrumentType.Medium, "Inst1");
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrumentInfo });
        var bottle = CreateBottle("|01073330660041291722033110AB103|991234", EnumReagentType.StopSolution, 1, remainingQuantity, EnumBottledReagentError.UnspecifiedBottledReagentError, inUse, 0, reservedScheduled);
        reagentStorage.Setup(x => x.GetBottles("Inst1")).Returns(new List<BottledReagent> { bottle });
        reagentBarcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.StopSolutionBottle, Secondary));
        reagentBarcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(
            CreateReagentInfo("ES3", ArticleType.StopSolutionBottle, "StopSolution"));
        var handler = new GetBottlesQueryHandler(
            instrumentService.Object,
            reagentBarcodeService.Object,
            reagentStorage.Object);
        var query = new GetBottlesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        var dto = result.Items.SelectMany(x => x.Items).FirstOrDefault();
        Assert.NotNull(dto);
        Assert.Equal(expectedStatus, dto.Status);
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4920")]
    [Theory]
    [InlineData("LOT1", "WashAdditive", true)]
    [InlineData("LOT2", "WashConcentrate", false)]
    public async Task Handle_FilterBySearchText_Theory(string searchText, string expectedName, bool shouldExist)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var reagentBarcodeService = new Mock<IReagentBarcodeService>();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var instrumentInfo = new TestInstrument("Inst1", InstrumentType.Medium, "Inst1");
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrumentInfo });
        var bottles = new List<BottledReagent>
        {
            CreateBottle("|01073330660041291722033110WA101|991234", EnumReagentType.WashAdditive, 1, 10),
            CreateBottle("|01073330660041291722033110WC102|995678", EnumReagentType.WashConcentrate, 2, 10)
        };
        reagentStorage.Setup(x => x.GetBottles("Inst1")).Returns(bottles);
        reagentBarcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((string barcode) =>
        {
            if (barcode.Contains("WA1"))
            {
                return (ArticleType.WashAdditiveBottle, Secondary);
            }

            if (barcode.Contains("WC1"))
            {
                return (ArticleType.WashConcentrateBottle, Secondary);
            }

            return (ArticleType.None, null);
        });
        reagentBarcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync((ArticleType _, string? primaryBarcode, string? secondaryBarcode) =>
        {
            var udi = secondaryBarcode ?? primaryBarcode;
            if (udi?.EndsWith("1234") == true)  // WA1 bottle serial number
            {
                return CreateReagentInfo("WA1", ArticleType.WashAdditiveBottle, "WashAdditive", "LOT1");
            }
            if (udi?.EndsWith("5678") == true)  // WC1 bottle serial number
            {
                return CreateReagentInfo("WC1", ArticleType.WashConcentrateBottle, "WashConcentrate", "LOT3");
            }

            return null;
        });
        var handler = new GetBottlesQueryHandler(
            instrumentService.Object,
            reagentBarcodeService.Object,
            reagentStorage.Object);
        var query = new GetBottlesQuery { SearchText = searchText, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        var items = result.Items.SelectMany(x => x.Items).ToList();
        if (shouldExist)
        {
            Assert.Single(items);
            Assert.Equal(expectedName, items[0].Name);
        }
        else
        {
            Assert.Empty(items);
        }
    }

    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4918")]
    [Theory]
    [InlineData("Unknown", 0)]
    [InlineData("Inst1", 1)]
    public async Task Handle_FilterBySelectedInstruments_Theory(string instrumentName, int expectedCount)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var reagentBarcodeService = new Mock<IReagentBarcodeService>();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var instrumentInfo = new TestInstrument("Inst1", InstrumentType.Medium, "Inst1");
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrumentInfo });
        var bottles = new List<BottledReagent>
        {
            CreateBottle("|01073330660041291722033110WA101|991234", EnumReagentType.WashAdditive, 1, 10)
        };
        reagentStorage.Setup(x => x.GetBottles("Inst1")).Returns(bottles);
        reagentBarcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.WashAdditiveBottle, Secondary));
        reagentBarcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo("WA1", ArticleType.WashAdditiveBottle, "WashAdditive", "LOT1"));
        var handler = new GetBottlesQueryHandler(
            instrumentService.Object,
            reagentBarcodeService.Object,
            reagentStorage.Object);
        var query = new GetBottlesQuery { SelectedInstruments = new List<string> { instrumentName }, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        Assert.Equal(expectedCount, result.Items[0].Items.Count);
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4912")]
    [Trait("ProductRequirement", "PR_2121")]
    [Trait("ModuleRequirement", "MR_4919")]
    [Theory]
    [InlineData("Reserved", ReagentPositionStatus.Reserved, true)]
    [InlineData("Empty", ReagentPositionStatus.Empty, false)]
    public async Task Handle_FilterByStatus_Theory(string status, ReagentPositionStatus bottleStatus, bool shouldExist)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var reagentBarcodeService = new Mock<IReagentBarcodeService>();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var instrumentInfo = new TestInstrument("Inst1", InstrumentType.Medium, "Inst1");
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrumentInfo });
        var bottle = CreateBottle("|01073330660041291722033110WA101|991234", EnumReagentType.WashAdditive, 1, 10, EnumBottledReagentError.UnspecifiedBottledReagentError, false, bottleStatus == ReagentPositionStatus.Reserved ? 5 : 0);
        reagentStorage.Setup(x => x.GetBottles("Inst1")).Returns(new List<BottledReagent> { bottle });
        reagentBarcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((ArticleType.WashAdditiveBottle, Secondary));
        reagentBarcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(CreateReagentInfo("WA1", ArticleType.WashAdditiveBottle, "WashAdditive", "LOT1"));
        var handler = new GetBottlesQueryHandler(
            instrumentService.Object,
            reagentBarcodeService.Object,
            reagentStorage.Object);
        var query = new GetBottlesQuery { SelectedStatuses = new List<string> { status }, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        var items = result.Items.SelectMany(x => x.Items).ToList();
        if (shouldExist)
        {
            Assert.Single(items);
            Assert.Equal(ReagentPositionStatus.Reserved, items[0].Status);
        }
        else
        {
            Assert.Empty(items);
        }
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4921")]
    [Theory]
    [InlineData("", true)]
    [InlineData("Position", true)]
    [InlineData("Name", true)]
    [InlineData("UnknownProperty", true)]
    public async Task Handle_Sorting_Theory(string sortProperty, bool shouldExist)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var reagentBarcodeService = new Mock<IReagentBarcodeService>();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var instrumentInfo = new TestInstrument("Inst1", InstrumentType.Medium, "Inst1");
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrumentInfo });
        var bottles = new List<BottledReagent>
        {
            CreateBottle("|01073330660041291722033110WA101|991234", EnumReagentType.WashAdditive, 2, 10),
            CreateBottle("|01073330660041291722033110WC102|991234", EnumReagentType.WashConcentrate, 1, 10)
        };
        reagentStorage.Setup(x => x.GetBottles("Inst1")).Returns(bottles);
        reagentBarcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((string barcode) =>
        {
            if (barcode.Contains("WA1"))
            {
                return (ArticleType.WashAdditiveBottle, Secondary);
            }

            if (barcode.Contains("WC1"))
            {
                return (ArticleType.WashConcentrateBottle, Secondary);
            }

            return (ArticleType.None, null);
        });
        reagentBarcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync((ArticleType _, string? primaryBarcode, string? secondaryBarcode) =>
        {
            var udi = secondaryBarcode ?? primaryBarcode;
            if (udi?.Contains("WA1") == true)
            {
                return CreateReagentInfo("WA1", ArticleType.WashAdditiveBottle, "B", "LOT1");
            }

            return CreateReagentInfo("WC1", ArticleType.WashConcentrateBottle, "A", "LOT2");
        });
        var handler = new GetBottlesQueryHandler(
            instrumentService.Object,
            reagentBarcodeService.Object,
            reagentStorage.Object);
        var query = new GetBottlesQuery { SortByProperty = sortProperty, StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        var items = result.Items.SelectMany(x => x.Items).ToList();
        if (shouldExist)
        {
            Assert.Equal(2, items.Count);
        }
        else
        {
            Assert.Empty(items);
        }
    }

    [Trait("ProductRequirement", "PR_60")]
    [Trait("ModuleRequirement", "MR_4923")]
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    public async Task Handle_Pagination_Theory(int startIndex, int expectedCount)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var reagentBarcodeService = new Mock<IReagentBarcodeService>();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var instrumentInfo = new TestInstrument("Inst1", InstrumentType.Medium, "Inst1");
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrumentInfo });
        var bottles = new List<BottledReagent>
        {
            CreateBottle("|01073330660041291722033110WA101|991234", EnumReagentType.WashAdditive, 1, 10),
            CreateBottle("|01073330660041291722033110WC102|991234", EnumReagentType.WashConcentrate, 2, 10)
        };
        reagentStorage.Setup(x => x.GetBottles("Inst1")).Returns(bottles);
        reagentBarcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((string barcode) =>
        {
            if (barcode.Contains("WA1"))
            {
                return (ArticleType.WashAdditiveBottle, Secondary);
            }
            if (barcode.Contains("WC1"))
            {
                return (ArticleType.WashConcentrateBottle, Secondary);
            }
            return (ArticleType.None, null);
        });
        reagentBarcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync((ArticleType _, string? primaryBarcode, string? secondaryBarcode) =>
        {
            var udi = secondaryBarcode ?? primaryBarcode;
            if (udi?.Contains("WA1") == true)
            {
                return CreateReagentInfo("WA1", ArticleType.WashAdditiveBottle, "B", "LOT1");
            }

            return CreateReagentInfo("WC1", ArticleType.WashConcentrateBottle, "A", "LOT2");
        });
        var handler = new GetBottlesQueryHandler(
            instrumentService.Object,
            reagentBarcodeService.Object,
            reagentStorage.Object);
        var query = new GetBottlesQuery { StartIndex = startIndex, Count = 1 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        Assert.Equal(expectedCount, result.Items[0].Items.Count);
    }

    [Theory]
    [InlineData("INVALID_UDI")]
    [InlineData("(01)12345678901234(17)250731(10)AB1234(99)ES3MJAYZA")]
    public async Task Handle_InvalidUdiOrNullReagentInfo_Theory(string udi)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var reagentBarcodeService = new Mock<IReagentBarcodeService>();
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var instrumentInfo = new TestInstrument("Inst1", InstrumentType.Medium, "Inst1");
        instrumentService.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrumentInfo });
        var bottle = CreateBottle(udi, EnumReagentType.StopSolution, 1, 10);
        reagentStorage.Setup(x => x.GetBottles("Inst1")).Returns(new List<BottledReagent> { bottle });
        reagentBarcodeService.Setup(x => x.DetectArticleType(It.IsAny<string>())).ReturnsAsync((string barcode) =>
        {
            return barcode == "INVALID_UDI" ? (ArticleType.None, null) : (ArticleType.StopSolutionBottle, Secondary);
        });
        reagentBarcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync((ArticleType _, string? primaryBarcode, string? secondaryBarcode) =>
        {
            var barcode = secondaryBarcode ?? primaryBarcode;
            // Return null for both invalid UDI and the specific long UDI to test "InvalidUdiOrNullReagentInfo"
            return barcode is "INVALID_UDI" or "(01)12345678901234(17)250731(10)AB1234(99)ES3MJAYZA" ? null : CreateReagentInfo("ES3", ArticleType.StopSolutionBottle, "StopSolution");
        });
        var handler = new GetBottlesQueryHandler(
            instrumentService.Object,
            reagentBarcodeService.Object,
            reagentStorage.Object);
        var query = new GetBottlesQuery { StartIndex = 0, Count = 10 };
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);
        Assert.Empty(result.Items[0].Items);
    }
}

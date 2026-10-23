using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Application.Services;

namespace Pharos.Server.LoadList.Tests.Application.Services;

public class LoadListServiceTests
{
    private readonly LoadListService _sut = new();

    [Fact]
    public void GetReagentLoadersCount_NoData_ReturnsZero()
    {
        // Act
        var result = _sut.GetReagentLoadersCount();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetReagentLoadersCount_OnlyCarriers_CalculatesCorrectly()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        _sut.AddItemToCarrierDict(instrumentNumber, "c1", CreateCarrier(missingQuantity: 300));
        _sut.AddItemToCarrierDict(instrumentNumber, "c2", CreateCarrier(missingQuantity: 200));

        // Act - 300 + 200 doses = 50 carriers, => 50 spaces, needs 1 loader (50/72 < 1)
        var result = _sut.GetReagentLoadersCount();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetReagentLoadersCount_OnlyCartridges_CalculatesCorrectly()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        _sut.AddItemToCartridgeDict(instrumentNumber, "ct1", CreateCartridge(quantityToLoad: 50)); // (50/10) * 6 = 30 spaces
        _sut.AddItemToCartridgeDict(instrumentNumber, "ct2", CreateCartridge(quantityToLoad: 80)); // (80/10) * 6 = 48 spaces

        // Act - Total 78 spaces, needs 2 loaders (78/72 = 1.08)
        var result = _sut.GetReagentLoadersCount();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void GetReagentLoadersCount_CarriersAndCartridges_CalculatesCorrectly()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        _sut.AddItemToCarrierDict(instrumentNumber, "c1", CreateCarrier(missingQuantity: 360)); // 36 spaces
        _sut.AddItemToCartridgeDict(instrumentNumber, "ct1", CreateCartridge(quantityToLoad: 60)); // (60/10) * 6 = 36 spaces

        // Act - Total 72 spaces, needs exactly 1 loader
        var result = _sut.GetReagentLoadersCount();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetReagentLoadersCount_ExactlyOneLoaderCapacity_ReturnsOne()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        _sut.AddItemToCarrierDict(instrumentNumber, "c1", CreateCarrier(missingQuantity: 720)); // Exactly 72 spaces

        // Act
        var result = _sut.GetReagentLoadersCount();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetReagentLoadersCount_OneSpaceOverCapacity_ReturnsTwoLoaders()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        _sut.AddItemToCarrierDict(instrumentNumber, "c1", CreateCarrier(missingQuantity: 730)); // 73 spaces

        // Act - 73/72 = 1.01, needs 2 loaders
        var result = _sut.GetReagentLoadersCount();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void GetReagentLoadersCount_MultipleInstruments_SumsAllLoaders()
    {
        // Arrange
        const string instrument1 = "INST001";
        const string instrument2 = "INST002";

        _sut.AddItemToCarrierDict(instrument1, "c1", CreateCarrier(missingQuantity: 500)); // 50 spaces = 1 loader
        _sut.AddItemToCarrierDict(instrument2, "c2", CreateCarrier(missingQuantity: 1000)); // 100 spaces = 2 loaders

        // Act
        var result = _sut.GetReagentLoadersCount();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void GetReagentLoadersCount_FilteredByInstrumentNumber_ReturnsCorrectCount()
    {
        // Arrange
        const string instrument1 = "INST001";
        const string instrument2 = "INST002";

        _sut.AddItemToCarrierDict(instrument1, "c1", CreateCarrier(missingQuantity: 500)); // 1 loader
        _sut.AddItemToCarrierDict(instrument2, "c2", CreateCarrier(missingQuantity: 1000)); // 2 loaders

        // Act - Only count for INST001
        var result = _sut.GetReagentLoadersCount([instrument1]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetReagentLoadersCount_FilteredByMultipleInstruments_ReturnsCorrectCount()
    {
        // Arrange
        const string instrument1 = "INST001";
        const string instrument2 = "INST002";
        const string instrument3 = "INST003";

        _sut.AddItemToCarrierDict(instrument1, "c1", CreateCarrier(missingQuantity: 500)); // 1 loader
        _sut.AddItemToCarrierDict(instrument2, "c2", CreateCarrier(missingQuantity: 1000)); // 2 loaders
        _sut.AddItemToCarrierDict(instrument3, "c3", CreateCarrier(missingQuantity: 300)); // 1 loader

        // Act - Only count for INST001 and INST003
        var result = _sut.GetReagentLoadersCount([instrument1, instrument3]);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void GetReagentLoadersCount_FilteredByStatus_OnlyCountsMatchingItems()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        _sut.AddItemToCarrierDict(instrumentNumber, "c1", CreateCarrier(missingQuantity: 360));
        _sut.AddItemToCarrierDict(instrumentNumber, "c2", CreateCarrier(missingQuantity: 360, minutes: 20));

        // Act - Only count Alarm status (36 spaces = 1 loader)
        var result = _sut.GetReagentLoadersCount(statuses: [LoadListStatus.Alarm]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetReagentLoadersCount_FilteredByMultipleStatuses_CountsMatchingItems()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        _sut.AddItemToCarrierDict(instrumentNumber, "c1", CreateCarrier(missingQuantity: 300));
        _sut.AddItemToCarrierDict(instrumentNumber, "c2", CreateCarrier(missingQuantity: 300, 20));
        _sut.AddItemToCarrierDict(instrumentNumber, "c3", CreateCarrier(missingQuantity: 300, 80));

        // Act - Count Alarm and Warning (60 spaces = 1 loader)
        var result = _sut.GetReagentLoadersCount(statuses: [LoadListStatus.Alarm, LoadListStatus.Warning]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetReagentLoadersCount_FilteredByInstrumentAndStatus_CountsCorrectly()
    {
        // Arrange
        const string instrument1 = "INST001";
        const string instrument2 = "INST002";

        _sut.AddItemToCarrierDict(instrument1, "c1", CreateCarrier(missingQuantity: 400));
        _sut.AddItemToCarrierDict(instrument1, "c2", CreateCarrier(missingQuantity: 400, 20));
        _sut.AddItemToCarrierDict(instrument2, "c3", CreateCarrier(missingQuantity: 400));

        // Act - Only count INST001 with Alarm status (40 spaces = 1 loader)
        var result = _sut.GetReagentLoadersCount([instrument1], [LoadListStatus.Alarm]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetReagentLoadersCount_EmptyInstrumentFilter_ReturnsAll()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        _sut.AddItemToCarrierDict(instrumentNumber, "c1", CreateCarrier(missingQuantity: 500));

        // Act - Empty instrument list should return 1
        var result = _sut.GetReagentLoadersCount([]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetReagentLoadersCount_EmptyStatusFilter_CountsAllStatuses()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        _sut.AddItemToCarrierDict(instrumentNumber, "c1", CreateCarrier(missingQuantity: 360));
        _sut.AddItemToCarrierDict(instrumentNumber, "c2", CreateCarrier(missingQuantity: 360, 20));

        // Act - Empty status list should count all (72 spaces = 1 loader)
        var result = _sut.GetReagentLoadersCount(statuses: []);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetReagentLoadersCount_NonExistentInstrument_ReturnsZero()
    {
        // Arrange
        const string existingInstrument = "INST001";
        const string nonExistentInstrument = "INST999";

        _sut.AddItemToCarrierDict(existingInstrument, "c1", CreateCarrier(missingQuantity: 500));

        // Act
        var result = _sut.GetReagentLoadersCount([nonExistentInstrument]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetReagentLoadersCount_ComplexScenario_CalculatesCorrectly()
    {
        // Arrange
        const string instrument1 = "INST001";
        const string instrument2 = "INST002";

        // INST001: 20 carriers + 3 cartridges (6 each) = 20 + 18 = 38 spaces = 1 loader
        _sut.AddItemToCarrierDict(instrument1, "c1", CreateCarrier(missingQuantity: 100));
        _sut.AddItemToCarrierDict(instrument1, "c2", CreateCarrier(missingQuantity: 100, 20));
        _sut.AddItemToCartridgeDict(instrument1, "ct1", CreateCartridge(quantityToLoad: 30));

        // INST002: 50 carriers + 10 cartridges (6 each) = 50 + 60 = 110 spaces = 2 loaders
        _sut.AddItemToCarrierDict(instrument2, "c3", CreateCarrier(missingQuantity: 500));
        _sut.AddItemToCartridgeDict(instrument2, "ct2", CreateCartridge(quantityToLoad: 100));

        // Act - Total = 3 loaders
        var result = _sut.GetReagentLoadersCount();

        // Assert
        result.Should().Be(3);
    }


    //Create both articles to hold 10 doses. 
    private static LoadListCarrierDto CreateCarrier(
        int missingQuantity = 1, int minutes = 0)
    {
        var item = new LoadListCarrierDto
        {
            QuantityInArticle = 10,
            ArticleIdentity = "C123",
            ArticleType = ArticleType.None,
            InstrumentName = "Instrument",
            Shape = ArticleIndicatorShape.None,
            Colors = []
        };
        item.SetPlanned(DateTimeOffset.UtcNow.AddMinutes(minutes), missingQuantity, null);
        return item;
    }

    private static LoadListCartridgeDto CreateCartridge(
        int quantityToLoad = 1,
        int minutes = 0)
    {
        var item = new LoadListCartridgeDto
        {
            QuantityInArticle = 10,
            Texts = ["Text1"],
            ArticleIdentity = "CT123",
            ArticleType = ArticleType.None,
            InstrumentName = "Instrument",
            Shape = ArticleIndicatorShape.None,
            Colors = []
        };
        item.SetPlanned(DateTimeOffset.UtcNow.AddMinutes(minutes), quantityToLoad, null);

        return item;
    }
}
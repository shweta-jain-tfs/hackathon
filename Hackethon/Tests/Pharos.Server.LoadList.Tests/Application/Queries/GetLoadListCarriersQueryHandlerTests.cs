using System.Collections.Concurrent;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Application.Interfaces;
using Pharos.Server.LoadList.Application.Queries;

namespace Pharos.Server.LoadList.Tests.Application.Queries;

public class GetLoadListCarriersQueryHandlerTests
{
    private readonly Mock<ILoadListService> _loadListService = new();
    private readonly Mock<IInstrumentManager> _instrumentManager = new();
    private readonly Mock<IInstrumentReagentStorageUtilization> _storageUtilization = new();

    [Fact]
    public async Task Handle_WhenTotalQuantityIsWithinCapacity_DoesNotChangeQuantities()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        var carriers = new ConcurrentDictionary<string, Dictionary<string, LoadListCarrierDto>>
        {
            [instrumentNumber] = new Dictionary<string, LoadListCarrierDto>
            {
                ["a"] = CreateCarrier("a", 300),
                ["b"] = CreateCarrier("b", 200)
            }
        };

        SetupDependencies(carriers, instrumentNumber);
        var sut = new GetLoadListCarriersQueryHandler(_loadListService.Object, _instrumentManager.Object, _storageUtilization.Object);

        // Act
        var result = await sut.Handle(new GetLoadListCarriersQuery(), CancellationToken.None);

        // Assert
        result.Sum(x => x.QuantityToLoad).Should().Be(500);
        result.Select(x => x.QuantityToLoad).Should().BeEquivalentTo([300, 200]);
    }

    [Fact]
    public async Task Handle_WhenTotalQuantityExceedsCapacity_ProportionallyCapsTo630()
    {
        // Arrange
        const string instrumentNumber = "INST001";
        var carriers = new ConcurrentDictionary<string, Dictionary<string, LoadListCarrierDto>>
        {
            [instrumentNumber] = new Dictionary<string, LoadListCarrierDto>
            {
                ["a"] = CreateCarrier("a", 400),
                ["b"] = CreateCarrier("b", 300)
            }
        };

        SetupDependencies(carriers, instrumentNumber);
        var sut = new GetLoadListCarriersQueryHandler(_loadListService.Object, _instrumentManager.Object, _storageUtilization.Object);

        // Act
        var result = await sut.Handle(new GetLoadListCarriersQuery(), CancellationToken.None);

        // Assert
        result.Sum(x => x.QuantityToLoad).Should().Be(630);
        result.Single(x => x.ArticleIdentity == "a").QuantityToLoad.Should().Be(360);
        result.Single(x => x.ArticleIdentity == "b").QuantityToLoad.Should().Be(270);
    }

    [Fact]
    public async Task Handle_WhenMultipleInstruments_CapsEachInstrumentIndependently()
    {
        // Arrange
        const string instrument1 = "INST001";
        const string instrument2 = "INST002";
        var carriers = new ConcurrentDictionary<string, Dictionary<string, LoadListCarrierDto>>
        {
            [instrument1] = new Dictionary<string, LoadListCarrierDto>
            {
                ["a"] = CreateCarrier("a", 500),
                ["b"] = CreateCarrier("b", 200)
            },
            [instrument2] = new Dictionary<string, LoadListCarrierDto>
            {
                ["c"] = CreateCarrier("c", 320),
                ["d"] = CreateCarrier("d", 180)
            }
        };

        SetupDependencies(carriers, instrument1, instrument2);
        var sut = new GetLoadListCarriersQueryHandler(_loadListService.Object, _instrumentManager.Object, _storageUtilization.Object);

        // Act
        var result = await sut.Handle(new GetLoadListCarriersQuery(), CancellationToken.None);

        // Assert
        var instrument1Items = result.Where(x => x.InstrumentName == instrument1).ToList();
        var instrument2Items = result.Where(x => x.InstrumentName == instrument2).ToList();

        instrument1Items.Sum(x => x.QuantityToLoad).Should().Be(630);
        instrument2Items.Sum(x => x.QuantityToLoad).Should().Be(500);
    }

    private void SetupDependencies(
        ConcurrentDictionary<string, Dictionary<string, LoadListCarrierDto>> carriers,
        params string[] instrumentNumbers)
    {
        _loadListService.Setup(x => x.GetLoadListCarriers()).Returns(carriers);

        foreach (var instrumentNumber in instrumentNumbers)
        {
            _storageUtilization
                .Setup(x => x.GetAvailablePositions(instrumentNumber, ReagentStorageType.Carrier))
                .Returns(630);

            var instrument = new Mock<IInstrument>();
            instrument.SetupGet(x => x.DisplayName).Returns(instrumentNumber);
            _instrumentManager.Setup(x => x.FindInstrument(instrumentNumber)).Returns(instrument.Object);
        }
    }

    private static LoadListCarrierDto CreateCarrier(string articleIdentity, int quantityToLoad)
    {
        var item = new LoadListCarrierDto
        {
            QuantityInArticle = 1,
            ArticleIdentity = articleIdentity,
            ArticleType = ArticleType.None,
            InstrumentName = string.Empty,
            Shape = ArticleIndicatorShape.None,
            Colors = []
        };

        item.SetPlanned(DateTimeOffset.UtcNow.AddMinutes(10), quantityToLoad, null);
        return item;
    }
}

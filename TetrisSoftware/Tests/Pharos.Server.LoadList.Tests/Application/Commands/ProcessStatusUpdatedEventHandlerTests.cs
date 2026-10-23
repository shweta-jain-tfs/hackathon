using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Pharos.Server.Application.LoadList;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Application.Interfaces;
using Pharos.Server.LoadList.Application.Services;
using Pharos.Server.LoadList.Domain;
using Tetris.Protocol.Pharos.Consumables;

namespace Pharos.Server.LoadList.Tests.Application.Commands;

public class ProcessStatusUpdatedEventHandlerTests
{
    private readonly MissingConsumablesUpdatedCommandHandler _sut;
    private static readonly Mock<IMediator> _mediatorMock = new();
    private static readonly Mock<IArticleUsageService> _articleUsageServiceMock = new();
    private static readonly Mock<ILoadListKeyService> _keyServiceMock = new();
    private readonly LoadListService _loadListService = new();

    private readonly Timestamp _time1 = Timestamp.FromDateTimeOffset(DateTimeOffset.Parse("2025-09-17 11:30:00", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
    private const string _instrumentNumber = "SEED_0001";

    public ProcessStatusUpdatedEventHandlerTests()
    {
        SetupArticleUsageServiceMock();
        _sut = new MissingConsumablesUpdatedCommandHandler(_loadListService, _mediatorMock.Object, _articleUsageServiceMock.Object, _keyServiceMock.Object);
    }

    private static void SetupArticleUsageServiceMock()
    {
        _articleUsageServiceMock
            .Setup(x => x.GetInformation(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ArticleType>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string instrumentNumber, string articleName, ArticleType articleType, string? methodName, CancellationToken ct) =>
                new ArticleUsage
                {
                    InstrumentNumber = instrumentNumber,
                    ArticleIdentity = articleName,
                    LotNumber = "LOT123",
                    FullName = $"{articleName} Full Name",
                    FriendlyName = articleName,
                    Texts = [],
                    Quantity = 100,
                    Shape = ArticleIndicatorShape.None,
                    Colors = []
                });
    }

    [Fact]
    public async Task Handle_ShouldProcessEvent()
    {
        // Arrange
        var msg = CreateProcessStatusMessage();
        var command = new MissingConsumablesUpdatedCommand(_instrumentNumber, msg);

        _keyServiceMock
            .Setup(x => x.GetKey(It.IsAny<MissingConsumable>()))
            .Returns((MissingConsumable item) => item.ReagentName);

        // Act

        //Send command to handler
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        //get bottle list from service 
        var bottleDict = _loadListService.GetLoadListBottles();

        // Assert
        bottleDict.Should().NotBeNull();
        bottleDict.Should().HaveCount(1);

        //extract bottle
        var bottleList = bottleDict[_instrumentNumber];
        bottleList.Should().HaveCount(1);

        var firstBottle = bottleList.Values.First();
        firstBottle.Planned.MissingQuantity.Should().Be(550);
        firstBottle.QuantityInArticle.Should().Be(100);

        firstBottle.Planned.StartTime.Should().Be(_time1.ToDateTimeOffset());
    }

    private MissingConsumablesInfo CreateProcessStatusMessage()
    {
        var processStatusMsg = new MissingConsumablesInfo
        {
            MissingConsumables =
            {
                new MissingConsumable
                {
                    Type = EnumMissingConsumableType.MissingWashConcentrate,
                    ReagentName = "wash",
                    MissingQuantity = 500,
                    TimeCapDispenseStop = _time1
                },
                new MissingConsumable
                {
                    Type = EnumMissingConsumableType.MissingWashConcentrate,
                    ReagentName = "wash",
                    MissingQuantity = 50,
                    TimeCapDispenseStop = _time1
                }
            }
        };

        return processStatusMsg;
    }
}
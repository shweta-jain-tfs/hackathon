using Google.Protobuf.WellKnownTypes;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetUnloadableQualityControlCartridgesQueryHandlerTests
{
    private readonly Mock<IInstrumentManager> _instruments = new();
    private readonly Mock<IInstrumentReagentStorage> _storage = new();
    private readonly Mock<IReagentBarcodeService> _barcodes = new();
    private readonly DateTime _expiry = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public GetUnloadableQualityControlCartridgesQueryHandlerTests()
    {
        var instrument = new Mock<IInstrument>();
        instrument.SetupGet(x => x.SerialNumber).Returns("123");
        instrument.SetupGet(x => x.DisplayName).Returns("Instrument 1");
        _instruments.Setup(x => x.GetInstruments()).Returns(new List<IInstrument> { instrument.Object });
        _barcodes.Setup(x => x.DetectArticleType(It.IsAny<string>()))
            .ReturnsAsync((ArticleType.QualityControlCartridge, BarcodeFormat.Primary));
        _barcodes.Setup(x => x.GetReagentInfo(ArticleType.QualityControlCartridge, It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new ReagentInfo
            {
                Article = new Article { ArticleType = ArticleType.QualityControlCartridge, Identity = "QC1" },
                Lot = new LotInformation { LotNumber = "LOT1", ExpiryDate = _expiry }
            });
    }
    
    [Theory]
    [InlineData(EnumCartridgeArea.FrontStorageArea, CartridgeStorageArea.FrontStorage)]
    [InlineData(EnumCartridgeArea.RearStorageArea, CartridgeStorageArea.BackStorage)]
    public async Task Handle_MapsMatchingUnloadableCartridge(EnumCartridgeArea area, CartridgeStorageArea expectedArea)
    {
        _storage.Setup(x => x.GetCartridges("123")).Returns(new[] { CreateCartridge(area) });

        var result = await CreateHandler().Handle(new GetUnloadableQualityControlCartridgesQuery("QC1"), TestContext.Current.CancellationToken);

        var cartridge = result.Should().ContainSingle().Subject;
        cartridge.Doses.Should().Be(10);
        cartridge.Lot.Should().Be("LOT1");
        cartridge.Status.Should().Be(ReagentPositionStatus.Available);
        cartridge.InstrumentName.Should().Be("Instrument 1");
        cartridge.Position.Should().Be(3);
        cartridge.Area.Should().Be(expectedArea);
        cartridge.ExpiryDate.Should().Be(new DateTimeOffset(_expiry));
        cartridge.ExpiryDateOpenCell.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExcludesOtherQualityControlNames()
    {
        _storage.Setup(x => x.GetCartridges("123")).Returns(new[] { CreateCartridge() });

        var result = await CreateHandler().Handle(new GetUnloadableQualityControlCartridgesQuery("Other QC"), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExcludesNonQualityControlArticles()
    {
        _storage.Setup(x => x.GetCartridges("123")).Returns(new[] { CreateCartridge() });
        _barcodes.Setup(x => x.DetectArticleType(It.IsAny<string>()))
            .ReturnsAsync((ArticleType.ConjugateCartridge, BarcodeFormat.Primary));

        var result = await CreateHandler().Handle(new GetUnloadableQualityControlCartridgesQuery("QC1"), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    private GetUnloadableQualityControlCartridgesQueryHandler CreateHandler() => new(_instruments.Object, _barcodes.Object, _storage.Object);

    private Cartridge CreateCartridge(EnumCartridgeArea area = EnumCartridgeArea.FrontStorageArea) => new()
    {
        ReagentBarcode = "barcode",
        Error = EnumCartridgeError.CartridgeNoError,
        Position = new CartridgePosition { Area = area, PositionInternalStorage = 3 },
        Cells =
        {
            new CartridgeCell
            {
                Content = new Tetris.Protocol.Common.ReagentContent
                {
                    RemainingQuantity = 10,
                    OpenTimestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)),
                    ExpiryDate = Timestamp.FromDateTime(_expiry)
                }
            }
        }
    };
}

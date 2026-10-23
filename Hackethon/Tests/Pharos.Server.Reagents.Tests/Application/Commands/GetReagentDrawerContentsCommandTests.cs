using Google.Protobuf.WellKnownTypes;
using MediatR;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Reagents.Application.Commands;

public class GetReagentDrawerContentsCommandTests
{
    private static readonly DateTimeOffset _lotExpiryDate = new(2022, 3, 5, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _openTimestamp = new(2022, 1, 4, 3, 4, 1, TimeSpan.Zero);
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GetReagentDrawerContentsCommandHandler _sut;

    public GetReagentDrawerContentsCommandTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new GetReagentDrawerContentsCommandHandler(_mediatorMock.Object);
    }

    [Fact]
    public async Task OneCartridge()
    {
        var barcode = "ABCE25T9355R0";
        var cartridges = new Dictionary<string, ReagentContentDto>();
        var dto = new ReagentContentDto
        {
            ReagentBarcode = barcode,
            CarrierUdi = string.Empty,
            ArticleType = ArticleType.ConjugateCartridge,
            ArticleNumber = "ABC",
            LotNumber = "ABCT9",
            LotExpiryDate = _lotExpiryDate,
            SerialNumber = string.Empty,
            ContentCells = new List<ReagentContentCellDto>
            {
                new()
                {
                    Position = 1,
                    RemainingQuantity = 20,
                    UseRemainingQuantity = true,
                    OpenTimestamp = _openTimestamp
                }
            }
        };
        cartridges[barcode] = dto;

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCartridgeContentsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartridges);

        var instrumentMessage = new ReagentDrawerContentsRequest();
        var position1 = new ReagentDrawerPosition
        {
            DrawerPosition = 1,
            CompartmentPosition = 2,
            SubCompartmentPosition = 1

        };
        instrumentMessage.Cartridges.Add(new DetectedCartridge
        {
            Position = position1,
            ReagentBarcode = barcode,
            CannotReadBarcode = false
        });

        var position2 = new ReagentDrawerPosition
        {
            DrawerPosition = 2,
            CompartmentPosition = 3,
            SubCompartmentPosition = 1
        };
        instrumentMessage.Cartridges.Add(new DetectedCartridge
        {
            Position = position2,
            CannotReadBarcode = true
        });

        var result = await _sut.Handle(new GetReagentDrawerContentsCommand("1", instrumentMessage), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Cartridges.Should().HaveCount(2);
        var reagent = result.Cartridges[0];
        reagent.UnknownCartridge.Should().BeFalse();
        reagent.Position.Should().BeSameAs(position1);
        var content = reagent.Content;

        content.ReagentBarcode.Should().Be(dto.ReagentBarcode);
        content.ReagentUdiBarcode.Should().BeEmpty();
        content.ArticleNumber.Should().Be(dto.ArticleNumber);
        content.LotNumber.Should().Be(dto.LotNumber);
        content.LotExpiryDate.Should().Be(Timestamp.FromDateTimeOffset(dto.LotExpiryDate.ToUniversalTime()));
        content.ContentCells.Should().HaveCount(1);
        content.ContentCells[0].Position.Should().Be(1);
        content.ContentCells[0].RemainingQuantity.Should().Be(20);
        content.ContentCells[0].OpenTimestamp.Should().Be(Timestamp.FromDateTimeOffset(_openTimestamp.ToUniversalTime()));

        var reagent2 = result.Cartridges[1];
        reagent2.UnknownCartridge.Should().BeTrue();
    }

    [Fact]
    public async Task CarrierWithReadableOcr_PopulatesDetectedCarrierInfoCorrectly()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetCarrierContentsByOcrQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ReagentContentDto>());
        var sut = new GetReagentDrawerContentsCommandHandler(mediatorMock.Object);

        var instrumentMessage = new ReagentDrawerContentsRequest();
        var position = new ReagentDrawerPosition
        {
            DrawerPosition = 1,
            CompartmentPosition = 2,
            SubCompartmentPosition = 1
        };
        instrumentMessage.Carriers.Add(new DetectedCarrier
        {
            Position = position,
            CarrierOcr = "f1",
            CannotReadOcr = false
        });

        var result = await sut.Handle(new GetReagentDrawerContentsCommand("1", instrumentMessage), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Carriers.Should().HaveCount(1);
        var carrier = result.Carriers[0];
        carrier.UnknownCarrier.Should().BeTrue();
        carrier.TestName.Should().Be(string.Empty);
        carrier.CarrierType.Should().Be(EnumReagentType.UnspecifiedReagentType);
        carrier.Position.Should().BeSameAs(position);
    }
}

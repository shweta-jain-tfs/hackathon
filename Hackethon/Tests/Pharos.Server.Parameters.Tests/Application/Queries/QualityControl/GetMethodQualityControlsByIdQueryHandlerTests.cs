using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.QualityControl;

public class GetMethodQualityControlsByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_TubeQualityControl_ReturnsTubesWithoutQueryingCartridges()
    {
        var context = new Mock<IParametersDbContext>();
        context.Setup(x => x.QualityControlTests).ReturnsDbSet(new List<BaseQualityControlTest>
        {
            new PhadiaQualityControlTest
            {
                Id = 1,
                SystemType = QualityControlSystemType.Phadia,
                QualityControl = new PhadiaQualityControl { Name = "QC1", IsQcInTube = true },
                Test = new MethodTest { TestName = "Test", Method = new Method { MethodName = "Method" } },
                Lots = new List<MethodQualityControlLot>()
            }
        });
        var detected = DateTimeOffset.UtcNow;
        var tubes = new List<UnloadableQualityControlTubeDto>
        {
            new()
            {
                Id = "TUBE1",
                Lot = "TUBELOT",
                DetectedDateTime = detected,
                RemainingVolume = 100,
                InstrumentNumber = "123",
                InstrumentName = "Instrument 1",
                Area = SampleArea.InternalStorage,
                TubePosition = 12,
                TubePositionDisplayValue = "B2",
                TrayId = "TRAY1",
                DoesTrayExist = true,
                IsInCurrentInstrument = true
            }
        };
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(new GetUnloadableQualityControlTubesQuery("QC1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tubes);
        var handler = new GetMethodQualityControlsByIdQueryHandler(context.Object, sender.Object);

        var result = await handler.Handle(new GetMethodQualityControlsByIdQuery(1), TestContext.Current.CancellationToken);

        result.Tubes.Should().BeSameAs(tubes);
        result.Cartridges.Should().BeEmpty();
        var tube = result.Tubes.Should().ContainSingle().Subject;
        tube.Id.Should().Be("TUBE1");
        tube.Lot.Should().Be("TUBELOT");
        tube.DetectedDateTime.Should().Be(detected);
        tube.RemainingVolume.Should().Be(100);
        tube.TubePositionDisplayValue.Should().Be("B2");
        tube.DoesTrayExist.Should().BeTrue();
        tube.IsInCurrentInstrument.Should().BeTrue();
        sender.Verify(x => x.Send(new GetUnloadableQualityControlTubesQuery("QC1"), TestContext.Current.CancellationToken), Times.Once);
        sender.Verify(x => x.Send(It.IsAny<GetUnloadableQualityControlCartridgesQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CartridgeQualityControl_ReturnsCartridgesWithoutQueryingTubes()
    {
        var context = new Mock<IParametersDbContext>();
        context.Setup(x => x.QualityControlTests).ReturnsDbSet(new List<BaseQualityControlTest>
        {
            new PhadiaQualityControlTest
            {
                Id = 1,
                SystemType = QualityControlSystemType.Phadia,
                QualityControl = new PhadiaQualityControl { Name = "QC1", IsQcInTube = false },
                Test = new MethodTest { TestName = "Test", Method = new Method { MethodName = "Method" } },
                Lots = new List<MethodQualityControlLot>()
            }
        });
        var expiryDate = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var cartridges = new List<UnloadableQualityControlCartridgeDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Doses = 10,
                Lot = "CARTRIDGELOT",
                Barcode = "CARTRIDGEBARCODE",
                Status = ReagentPositionStatus.Available,
                ExpiryDate = expiryDate,
                InstrumentName = "Instrument 1",
                InstrumentNumber = "123",
                Position = 3,
                Area = CartridgeStorageArea.BackStorage
            }
        };
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(new GetUnloadableQualityControlCartridgesQuery("QC1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartridges);
        var handler = new GetMethodQualityControlsByIdQueryHandler(context.Object, sender.Object);

        var result = await handler.Handle(new GetMethodQualityControlsByIdQuery(1), TestContext.Current.CancellationToken);

        result.Cartridges.Should().BeSameAs(cartridges);
        result.Tubes.Should().BeEmpty();
        var cartridge = result.Cartridges.Should().ContainSingle().Subject;
        cartridge.Doses.Should().Be(10);
        cartridge.Lot.Should().Be("CARTRIDGELOT");
        cartridge.Barcode.Should().Be("CARTRIDGEBARCODE");
        cartridge.ExpiryDate.Should().Be(expiryDate);
        cartridge.ExpiryDateOpenCell.Should().BeNull();
        cartridge.InstrumentName.Should().Be("Instrument 1");
        cartridge.InstrumentNumber.Should().Be("123");
        cartridge.Position.Should().Be(3);
        cartridge.Area.Should().Be(CartridgeStorageArea.BackStorage);
        sender.Verify(x => x.Send(new GetUnloadableQualityControlCartridgesQuery("QC1"), TestContext.Current.CancellationToken), Times.Once);
        sender.Verify(x => x.Send(It.IsAny<GetUnloadableQualityControlTubesQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

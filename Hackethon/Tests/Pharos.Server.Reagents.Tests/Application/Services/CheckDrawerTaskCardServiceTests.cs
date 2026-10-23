using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Services;

[Trait("ProductRequirement", "PR_737")]
[Trait("ModuleRequirement", "PR_737")]
public class CheckDrawerTaskCardServiceTests
{
    private const string _instrumentNumber = "I1";
    private readonly Mock<IInstrumentReagentStorage> _reagentStorageMock = new();
    private readonly Mock<IInstrumentManager> _instrumentServiceMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IInstrument> _instrumentMock = new();

    private CheckDrawerTaskCardService CreateService()
    {
        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_instrumentNumber))
            .Returns(_instrumentMock.Object);

        return new CheckDrawerTaskCardService(
            _reagentStorageMock.Object,
            _instrumentServiceMock.Object,
            _mediatorMock.Object);
    }

    [Fact]
    public async Task UpdateCheckLiquidDrawerTaskCard_WhenBottleMissingAndDrawerOpen_DoesNotChangeTaskCard()
    {
        // Arrange
        _reagentStorageMock.Setup(x => x.GetLiquidWaste(_instrumentNumber)).Returns(Array.Empty<WasteContainer>());
        _reagentStorageMock.Setup(x => x.GetRinseContainers(_instrumentNumber)).Returns(Array.Empty<RinseContainer>());
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.RinseWaste]).Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.Open, false));
        var service = CreateService();

        // Act
        await service.UpdateCheckLiquidDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        // Assert
        VerifyRemoveTaskCardNotSent(TaskCardType.CheckLiquidDrawer);
        VerifyTaskCardNotSent(TaskCardType.CheckLiquidDrawer);
    }

    [Fact]
    public async Task UpdateCheckLiquidDrawerTaskCard_WhenNoBottleMissingAndDrawerOpen_RemovesTaskCard()
    {
        // Arrange
        _reagentStorageMock.Setup(x => x.GetLiquidWaste(_instrumentNumber)).Returns([
            new WasteContainer { Position = 1 },
            new WasteContainer { Position = 2 }
        ]);
        _reagentStorageMock.Setup(x => x.GetRinseContainers(_instrumentNumber)).Returns([
            new RinseContainer { Position = 1 },
            new RinseContainer { Position = 2 }
        ]);
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.RinseWaste]).Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.Open, false));
        var service = CreateService();

        // Act
        await service.UpdateCheckLiquidDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        // Assert
        VerifyRemoveTaskCard(TaskCardType.CheckLiquidDrawer);
        VerifyTaskCardNotSent(TaskCardType.CheckLiquidDrawer);
    }

    [Fact]
    public async Task UpdateCheckLiquidDrawerTaskCard_WhenBottleMissingAndDrawerClosedUnlocked_RaisesTaskCard()
    {
        _reagentStorageMock.Setup(x => x.GetLiquidWaste(_instrumentNumber)).Returns(Array.Empty<WasteContainer>());
        _reagentStorageMock.Setup(x => x.GetRinseContainers(_instrumentNumber)).Returns(Array.Empty<RinseContainer>());
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.RinseWaste]).Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedUnlocked, false));
        var service = CreateService();

        await service.UpdateCheckLiquidDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        VerifyTaskCard(TaskCardType.CheckLiquidDrawer, TaskCardState.Error);
    }

    [Fact]
    public async Task UpdateCheckLiquidDrawerTaskCard_WhenBottleMissingAndDrawerClosedLocked_DoesNotChangeTaskCard()
    {
        // Arrange
        _reagentStorageMock.Setup(x => x.GetLiquidWaste(_instrumentNumber)).Returns(Array.Empty<WasteContainer>());
        _reagentStorageMock.Setup(x => x.GetRinseContainers(_instrumentNumber)).Returns(Array.Empty<RinseContainer>());
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.RinseWaste]).Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));
        var service = CreateService();

        // Act
        await service.UpdateCheckLiquidDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        // Assert
        VerifyRemoveTaskCardNotSent(TaskCardType.CheckLiquidDrawer);
        VerifyTaskCardNotSent(TaskCardType.CheckLiquidDrawer);
    }

    [Fact]
    public async Task UpdateCheckLiquidDrawerTaskCard_WhenBottleHasErrorAndDrawerOpen_RaisesTaskCard()
    {
        _reagentStorageMock.Setup(x => x.GetLiquidWaste(_instrumentNumber)).Returns([
            CreateWasteContainer(1, EnumWasteContainerError.WasteContainerVolumeReadError)
        ]);
        _reagentStorageMock.Setup(x => x.GetRinseContainers(_instrumentNumber)).Returns(Array.Empty<RinseContainer>());
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.RinseWaste]).Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.Open, false));
        var service = CreateService();

        await service.UpdateCheckLiquidDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        VerifyTaskCard(TaskCardType.CheckLiquidDrawer, TaskCardState.Error);
    }

    [Fact]
    public async Task UpdateCheckLiquidDrawerTaskCard_WhenInstrumentIsResting_RaisesWarning()
    {
        _reagentStorageMock.Setup(x => x.GetLiquidWaste(_instrumentNumber)).Returns(Array.Empty<WasteContainer>());
        _reagentStorageMock.Setup(x => x.GetRinseContainers(_instrumentNumber)).Returns(Array.Empty<RinseContainer>());
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.RinseWaste]).Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedUnlocked, false));
        var service = CreateService();
        _instrumentMock.Setup(x => x.State).Returns(InstrumentState.Standby);

        await service.UpdateCheckLiquidDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        VerifyTaskCard(TaskCardType.CheckLiquidDrawer, TaskCardState.Warning);
    }

    [Fact]
    public async Task UpdateCheckWasteDrawerTaskCard_WhenBinMissingAndDrawerOpen_DoesNotChangeTaskCard()
    {
        // Arrange
        _reagentStorageMock.Setup(x => x.GetSolidWaste(_instrumentNumber)).Returns(Array.Empty<WasteContainer>());
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.SolidWaste]).Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.Open, false));
        var service = CreateService();

        // Act
        await service.UpdateCheckWasteDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        // Assert
        VerifyRemoveTaskCardNotSent(TaskCardType.CheckWasteDrawer);
        VerifyTaskCardNotSent(TaskCardType.CheckWasteDrawer);
    }

    [Fact]
    public async Task UpdateCheckWasteDrawerTaskCard_WhenNoBinMissingAndDrawerOpen_RemovesTaskCard()
    {
        // Arrange
        _reagentStorageMock.Setup(x => x.GetSolidWaste(_instrumentNumber)).Returns([
            new WasteContainer { Position = 1, Type = EnumWasteContainerType.WasteContainerTypeBioHazard },
            new WasteContainer { Position = 2, Type = EnumWasteContainerType.WasteContainerTypeRecyclable }
        ]);
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.SolidWaste]).Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.Open, false));
        var service = CreateService();

        // Act
        await service.UpdateCheckWasteDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        // Assert
        VerifyRemoveTaskCard(TaskCardType.CheckWasteDrawer);
        VerifyTaskCardNotSent(TaskCardType.CheckWasteDrawer);
    }

    [Fact]
    public async Task UpdateCheckWasteDrawerTaskCard_WhenBinMissingAndDrawerClosed_RaisesTaskCard()
    {
        _reagentStorageMock.Setup(x => x.GetSolidWaste(_instrumentNumber)).Returns(Array.Empty<WasteContainer>());
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.SolidWaste]).Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));
        var service = CreateService();

        await service.UpdateCheckWasteDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        VerifyTaskCard(TaskCardType.CheckWasteDrawer, TaskCardState.Error);
    }

    [Fact]
    public async Task UpdateCheckWasteDrawerTaskCard_WhenBinMissingAndDrawerClosedUnlocked_DoesNotChangeTaskCard()
    {
        // Arrange
        _reagentStorageMock.Setup(x => x.GetSolidWaste(_instrumentNumber)).Returns(Array.Empty<WasteContainer>());
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.SolidWaste]).Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedUnlocked, false));
        var service = CreateService();

        // Act
        await service.UpdateCheckWasteDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        // Assert
        VerifyRemoveTaskCardNotSent(TaskCardType.CheckWasteDrawer);
        VerifyTaskCardNotSent(TaskCardType.CheckWasteDrawer);
    }

    [Fact]
    public async Task UpdateCheckWasteDrawerTaskCard_WhenBinHasErrorAndDrawerOpen_RaisesTaskCard()
    {
        _reagentStorageMock.Setup(x => x.GetSolidWaste(_instrumentNumber)).Returns([
            CreateSolidWasteContainer(1, EnumWasteContainerType.WasteContainerTypeBioHazard, EnumWasteContainerError.WasteContainerVolumeReadError)
        ]);
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.SolidWaste]).Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.Open, false));
        var service = CreateService();

        await service.UpdateCheckWasteDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        VerifyTaskCard(TaskCardType.CheckWasteDrawer, TaskCardState.Error);
    }

    [Fact]
    public async Task UpdateCheckWasteDrawerTaskCard_WhenInstrumentIsResting_RaisesWarning()
    {
        _reagentStorageMock.Setup(x => x.GetSolidWaste(_instrumentNumber)).Returns(Array.Empty<WasteContainer>());
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.SolidWaste]).Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));
        var service = CreateService();
        _instrumentMock.Setup(x => x.State).Returns(InstrumentState.Standby);

        await service.UpdateCheckWasteDrawerTaskCard(_instrumentNumber, TestContext.Current.CancellationToken);

        VerifyTaskCard(TaskCardType.CheckWasteDrawer, TaskCardState.Warning);
    }

    private void VerifyTaskCard(TaskCardType taskCardType, TaskCardState taskCardState)
    {
        _mediatorMock.Verify(x => x.Send(
            It.Is<TaskCardCommand>(cmd =>
                cmd.CardType == taskCardType &&
                cmd.InstrumentNumber == _instrumentNumber &&
                cmd.Rules.Count == 1 &&
                cmd.Rules[0].CardState == taskCardState),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyTaskCardNotSent(TaskCardType taskCardType)
    {
        _mediatorMock.Verify(x => x.Send(
            It.Is<TaskCardCommand>(cmd => cmd.CardType == taskCardType),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyRemoveTaskCard(TaskCardType taskCardType)
    {
        _mediatorMock.Verify(x => x.Send(
            It.Is<RemoveTaskCardCommand>(cmd =>
                cmd.CardType == taskCardType &&
                cmd.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyRemoveTaskCardNotSent(TaskCardType taskCardType)
    {
        _mediatorMock.Verify(x => x.Send(
            It.Is<RemoveTaskCardCommand>(cmd => cmd.CardType == taskCardType),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static WasteContainer CreateWasteContainer(int position, EnumWasteContainerError error)
    {
        return new WasteContainer
        {
            Position = position,
            Error = error
        };
    }

    private static WasteContainer CreateSolidWasteContainer(int position, EnumWasteContainerType type, EnumWasteContainerError error)
    {
        return new WasteContainer
        {
            Position = position,
            Type = type,
            Error = error
        };
    }
}

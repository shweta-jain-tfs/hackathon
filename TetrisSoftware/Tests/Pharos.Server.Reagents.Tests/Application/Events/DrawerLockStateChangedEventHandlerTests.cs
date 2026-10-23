using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Reagents.Application.Services;

namespace Pharos.Server.Reagents.Application.Events;

[Trait("ProductRequirement", "PR_737")]
[Trait("ModuleRequirement", "PR_737")]
public class DrawerLockStateChangedEventHandlerTests
{
    private const string _instrumentNumber = "I1";
    private readonly Mock<ICheckDrawerTaskCardService> _checkDrawerTaskCardServiceMock = new();

    [Fact]
    public async Task Handle_WhenRinseWasteDrawerStateChangedToClosedUnlocked_UpdatesCheckLiquidDrawerTaskCard()
    {
        var handler = CreateHandler();
        var notification = CreateDrawerLockStateChanged(DrawerType.RinseWaste, LockState.Open, LockState.ClosedUnlocked);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckLiquidDrawerTaskCard(_instrumentNumber, It.IsAny<CancellationToken>()),
            Times.Once);
        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckWasteDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSolidWasteDrawerStateChangedToClosed_UpdatesCheckWasteDrawerTaskCard()
    {
        var handler = CreateHandler();
        var notification = CreateDrawerLockStateChanged(DrawerType.SolidWaste, LockState.Open, LockState.ClosedLocked);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckWasteDrawerTaskCard(_instrumentNumber, It.IsAny<CancellationToken>()),
            Times.Once);
        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckLiquidDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRinseWasteDrawerStateChangedToClosedLocked_DoesNotUpdateCheckLiquidDrawerTaskCard()
    {
        var handler = CreateHandler();
        var notification = CreateDrawerLockStateChanged(DrawerType.RinseWaste, LockState.Open, LockState.ClosedLocked);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckLiquidDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckWasteDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSolidWasteDrawerStateChangedToClosedUnlocked_DoesNotUpdateCheckWasteDrawerTaskCard()
    {
        var handler = CreateHandler();
        var notification = CreateDrawerLockStateChanged(DrawerType.SolidWaste, LockState.Open, LockState.ClosedUnlocked);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckLiquidDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckWasteDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRinseWasteDrawerStateDidNotChange_DoesNotUpdateCheckLiquidDrawerTaskCard()
    {
        var handler = CreateHandler();
        var notification = CreateDrawerLockStateChanged(DrawerType.RinseWaste, LockState.Open, LockState.Open);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckLiquidDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckWasteDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRinseWasteDrawerStateChangedToOpen_DoesNotUpdateCheckLiquidDrawerTaskCard()
    {
        var handler = CreateHandler();
        var notification = CreateDrawerLockStateChanged(DrawerType.RinseWaste, LockState.ClosedLocked, LockState.Open);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckLiquidDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckWasteDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSolidWasteDrawerStateChangedToOpen_DoesNotUpdateCheckWasteDrawerTaskCard()
    {
        var handler = CreateHandler();
        var notification = CreateDrawerLockStateChanged(DrawerType.SolidWaste, LockState.ClosedLocked, LockState.Open);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckLiquidDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckWasteDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOtherDrawerStateChanged_DoesNotUpdateCheckDrawerTaskCards()
    {
        var handler = CreateHandler();
        var notification = CreateDrawerLockStateChanged(DrawerType.Reagent, LockState.Open, LockState.ClosedLocked);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckLiquidDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkDrawerTaskCardServiceMock.Verify(
            x => x.UpdateCheckWasteDrawerTaskCard(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private DrawerLockStateChangedEventHandler CreateHandler()
    {
        return new DrawerLockStateChangedEventHandler(_checkDrawerTaskCardServiceMock.Object);
    }

    private static DrawerLockStateChanged CreateDrawerLockStateChanged(
        DrawerType drawerType,
        LockState previousLockState,
        LockState currentLockState)
    {
        return new DrawerLockStateChanged(
            _instrumentNumber,
            CreateDrawerStates(drawerType, previousLockState),
            CreateDrawerStates(drawerType, currentLockState));
    }

    private static List<DrawerLockState> CreateDrawerStates(DrawerType targetDrawerType, LockState targetLockState)
    {
        return Enum.GetValues<DrawerType>()
            .Select(drawerType => new DrawerLockState(
                drawerType,
                drawerType == targetDrawerType ? targetLockState : LockState.ClosedLocked,
                false,
                null))
            .ToList();
    }
}

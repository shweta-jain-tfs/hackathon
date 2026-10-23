using Google.Protobuf.WellKnownTypes;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetSolidWasteDrawerDetailsQueryHandlerTests
{
    private const string _testInstrumentNumber = "Inst1";

    [Fact]
    public async Task Handle_ReturnsAlways2Containers_WhenNoContainers()
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedUnlocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.LockState.Should().Be(LockState.ClosedUnlocked);
        result.IsUnlockInProgress.Should().BeFalse();
        result.Containers.Should().HaveCount(2);
        result.Containers[0].Position.Should().Be(1);
        result.Containers[0].Type.Should().Be(WasteType.Biohazard);
        result.Containers[1].Position.Should().Be(2);
        result.Containers[1].Type.Should().Be(WasteType.Recyclable);
    }

    [Trait("ProductRequirement", "PR_2164")]
    [Trait("ModuleRequirement", "MR_5513")]
    [Trait("ModuleRequirement", "MR_5514")]
    [Trait("ModuleRequirement", "MR_5517")]
    [Theory]
    [InlineData(LockState.Open, false)]
    [InlineData(LockState.ClosedLocked, false)]
    [InlineData(LockState.ClosedUnlocked, true)]
    public async Task Handle_ReturnsCorrectLockState(LockState lockState, bool isUnlockInProgress)
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, lockState, isUnlockInProgress));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LockState.Should().Be(lockState);
        result.IsUnlockInProgress.Should().Be(isUnlockInProgress);
    }

    [Trait("ProductRequirement", "PR_273")]
    [Trait("ModuleRequirement", "MR_5520")]
    [Fact]
    public async Task Handle_PopulatesBiohazardContainerData_WhenBottlePresent()
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(1, 65, EnumWasteContainerError.UnspecifiedWasteContainerError);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var biohazard = result.Containers.First(c => c.Type == WasteType.Biohazard);
        biohazard.Should().NotBeNull();
        biohazard.IsPresent.Should().BeTrue();
        biohazard.Percentage.Should().Be(65);
        biohazard.Status.Should().Be(ReagentPositionStatus.Available);
        biohazard.Position.Should().Be(1);
    }

    [Trait("ProductRequirement", "PR_273")]
    [Trait("ModuleRequirement", "MR_5521")]
    [Fact]
    public async Task Handle_PopulatesRecyclableContainerData_WhenBottlePresent()
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(2, 45, EnumWasteContainerError.UnspecifiedWasteContainerError);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var recyclable = result.Containers.First(c => c.Type == WasteType.Recyclable);
        recyclable.Should().NotBeNull();
        recyclable.IsPresent.Should().BeTrue();
        recyclable.Percentage.Should().Be(45);
        recyclable.Status.Should().Be(ReagentPositionStatus.Available);
        recyclable.Position.Should().Be(2);
    }

    [Trait("ProductRequirement", "PR_65")]
    [Trait("ModuleRequirement", "MR_5525")]
    [Theory]
    [InlineData(EnumWasteContainerError.WasteContainerVolumeReadError, ReagentPositionStatus.UndetectedVolume)]
    public async Task Handle_MapsWasteErrorToStatus_Correctly(
        EnumWasteContainerError error,
        ReagentPositionStatus expectedStatus)
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(1, 50, error);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var biohazard = result.Containers.First(c => c.Type == WasteType.Biohazard);
        biohazard.Status.Should().Be(expectedStatus);
    }

    [Trait("ProductRequirement", "PR_273")]
    [Trait("ModuleRequirement", "MR_5524")]
    [Fact]
    public async Task Handle_SetsMissingBottleStatus_WhenBottleNotPresent()
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Containers.Should().AllSatisfy(c => c.Status.Should().Be(ReagentPositionStatus.MissingBottle));
        result.Containers.Should().AllSatisfy(c => c.IsPresent.Should().BeFalse());
    }

    [Fact]
    public async Task Handle_SetsEmptyStatus_WhenDrawerOpenAndBottleNotPresent()
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.Open, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Containers.Should().AllSatisfy(c => c.Status.Should().Be(ReagentPositionStatus.Empty));
    }

    [Trait("ProductRequirement", "PR_2146")]
    [Trait("ModuleRequirement", "MR_5523")]
    [Theory]
    [InlineData(60, false, FillOrEmptyIndicationType.Pending)]  // Percentage > 50% = Pending
    [InlineData(45, true, FillOrEmptyIndicationType.Alarm)]     // CapDispenseStopped = Alarm
    public async Task Handle_SetsCorrectEmptyIndication(
        int percentageFull,
        bool capDispenseStopped,
        FillOrEmptyIndicationType expectedIndication)
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(1, percentageFull, EnumWasteContainerError.UnspecifiedWasteContainerError, capDispenseStopped);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var biohazard = result.Containers.First(c => c.Type == WasteType.Biohazard);
        biohazard.EmptyIndicationType.Should().Be(expectedIndication);
    }

    [Fact]
    public async Task Handle_SetsWarningIndication_WhenTimeCapDispenseStopWithinWarningPeriod()
    {
        // Arrange
        var timeCapDispenseStop = DateTime.UtcNow.AddMinutes(30); // Within 1 hour
        var wasteContainer = CreateWasteContainer(
            1,
            45,
            EnumWasteContainerError.UnspecifiedWasteContainerError,
            capDispenseStopped: false,
            timeCapDispenseStop: timeCapDispenseStop);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var biohazard = result.Containers.First(c => c.Type == WasteType.Biohazard);
        biohazard.EmptyIndicationType.Should().Be(FillOrEmptyIndicationType.Warning);
    }

    [Fact]
    public async Task Handle_UsesDefaultWasteLevel_WhenNoProtocolData()
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Containers.Should().AllSatisfy(c => c.Percentage.Should().Be(Constants.DefaultWasteLevel));
    }

    [Trait("ProductRequirement", "PR_65")]
    [Trait("ModuleRequirement", "MR_5522")]
    [Fact]
    public async Task Handle_AggregatesDataFromMultipleContainers()
    {
        // Arrange
        var wasteContainers = new List<WasteContainer>
        {
            CreateWasteContainer(1, 65, EnumWasteContainerError.UnspecifiedWasteContainerError),
            CreateWasteContainer(2, 45, EnumWasteContainerError.UnspecifiedWasteContainerError)
        };

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(wasteContainers);

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Containers.Should().HaveCount(2);
        result.Containers.Should().Contain(c => c.Type == WasteType.Biohazard && c.Percentage == 65);
        result.Containers.Should().Contain(c => c.Type == WasteType.Recyclable && c.Percentage == 45);
    }

    [Fact]
    public async Task Handle_SetsNoneIndication_WhenPercentageBelow50()
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(1, 30, EnumWasteContainerError.UnspecifiedWasteContainerError);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var biohazard = result.Containers.First(c => c.Type == WasteType.Biohazard);
        biohazard.EmptyIndicationType.Should().Be(FillOrEmptyIndicationType.None);
    }

    [Fact]
    public async Task Handle_SetsNoneIndication_WhenStopTimeIsPastAndPercentageBelow50()
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(
            1,
            30,
            EnumWasteContainerError.UnspecifiedWasteContainerError,
            capDispenseStopped: false,
            timeCapDispenseStop: DateTime.UtcNow.AddMinutes(-30));
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var biohazard = result.Containers.First(c => c.Type == WasteType.Biohazard);
        biohazard.EmptyIndicationType.Should().Be(FillOrEmptyIndicationType.None);
    }

    [Fact]
    public async Task Handle_DefaultsEmptyIndicationToNone_WhenBottleNotPresent()
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetSolidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.SolidWaste])
            .Returns(new DrawerLockState(DrawerType.SolidWaste, LockState.ClosedUnlocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetSolidWasteDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Containers.Should().AllSatisfy(c =>
            c.EmptyIndicationType.Should().Be(FillOrEmptyIndicationType.None));
    }

    private static GetSolidWasteDrawerDetailsQueryHandler CreateHandler(
        IInstrumentReagentStorage reagentStorage,
        IInstrumentManager instrumentService)
    {
        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        return new GetSolidWasteDrawerDetailsQueryHandler(reagentStorage, instrumentService, dateTimeMock.Object);
    }

    private static WasteContainer CreateWasteContainer(
        int position,
        int percentageFull,
        EnumWasteContainerError error,
        bool capDispenseStopped = false,
        DateTime? timeCapDispenseStop = null)
    {
        var stopTime = timeCapDispenseStop ?? DateTime.UtcNow.AddHours(25);

        return new WasteContainer
        {
            Position = position,
            PercentageFull = percentageFull,
            Error = error,
            CapDispenseStopped = capDispenseStopped,
            TimeCapDispenseStop = Timestamp.FromDateTime(stopTime.ToUniversalTime())
        };
    }

    private static Mock<IInstrument> CreateInstrumentMock(Mock<IInstrumentManager> instrumentService)
    {
        var instrumentMock = new Mock<IInstrument>();
        instrumentService
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrumentMock.Object);

        return instrumentMock;
    }
}

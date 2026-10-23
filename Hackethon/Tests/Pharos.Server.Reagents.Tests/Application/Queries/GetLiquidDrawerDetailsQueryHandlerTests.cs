using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetLiquidDrawerDetailsQueryHandlerTests
{
    // Constants
    private const string _testInstrumentNumber = "Inst1";

    // Tests

    [Fact]
    public async Task Handle_ReturnsAlways4Containers_WhenNoContainers()
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(Array.Empty<RinseContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedUnlocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.LockState.Should().Be(LockState.ClosedUnlocked);
        result.Containers.Should().HaveCount(4);
        result.Containers[0].Type.Should().Be(LiquidContainerType.Waste);
        result.Containers[0].Position.Should().Be(1);
        result.Containers[1].Type.Should().Be(LiquidContainerType.Waste);
        result.Containers[1].Position.Should().Be(2);
        result.Containers[2].Type.Should().Be(LiquidContainerType.Rinse);
        result.Containers[2].Position.Should().Be(1);
        result.Containers[3].Type.Should().Be(LiquidContainerType.Rinse);
        result.Containers[3].Position.Should().Be(2);
    }

    [Trait("ProductRequirement", "PR_737")]
    [Trait("ModuleRequirement", "MR_5335")]
    [Theory]
    [InlineData(LockState.Open)]
    [InlineData(LockState.ClosedLocked)]
    [InlineData(LockState.ClosedUnlocked)]
    public async Task Handle_ReturnsCorrectLockState(LockState lockState)
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(Array.Empty<RinseContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, lockState, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LockState.Should().Be(lockState);
    }

    [Trait("ProductRequirement", "PR_737")]
    [Trait("ModuleRequirement", "MR_5342")]
    [Trait("ProductRequirement", "PR_43")]
    [Trait("ModuleRequirement", "MR_5344")]
    [Trait("ProductRequirement", "PR_1383")]
    [Trait("ModuleRequirement", "MR_5351")]
    [Theory]
    [InlineData(LiquidContainerType.Waste, 1, 45)]
    [InlineData(LiquidContainerType.Rinse, 2, 75)]
    public async Task Handle_PopulatesContainerData_WhenBottlePresent(
        LiquidContainerType containerType,
        int position,
        int percentage)
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();

        if (containerType == LiquidContainerType.Waste)
        {
            var wasteContainer = CreateWasteContainer(position, percentage, EnumWasteContainerError.UnspecifiedWasteContainerError, true);
            reagentStorage.Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
                .Returns(new List<WasteContainer> { wasteContainer });
            reagentStorage.Setup(x => x.GetRinseContainers(_testInstrumentNumber))
                .Returns(Array.Empty<RinseContainer>());
        }
        else
        {
            var rinseContainer = CreateRinseContainer(position, percentage, EnumRinseBottleError.UnspecifiedRinseBottleError, true);
            reagentStorage.Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
                .Returns(Array.Empty<WasteContainer>());
            reagentStorage.Setup(x => x.GetRinseContainers(_testInstrumentNumber))
                .Returns(new List<RinseContainer> { rinseContainer });
        }

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var container = result.Containers.First(c => c.Type == containerType && c.Position == position);
        container.Should().NotBeNull();
        container.IsMissing.Should().BeFalse();
        container.InUse.Should().BeTrue();
        container.Percentage.Should().Be(percentage);
        container.Status.Should().Be(ReagentPositionStatus.Available);
    }

    [Trait("ProductRequirement", "PR_737")]
    [Trait("ModuleRequirement", "MR_5350")]
    [Theory]
    [InlineData(EnumWasteContainerError.WasteContainerVolumeReadError, ReagentPositionStatus.UndetectedVolume)]
    public async Task Handle_MapsWasteErrorToStatus_Correctly(
        EnumWasteContainerError error,
        ReagentPositionStatus expectedStatus)
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(1, 50, error, false);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(Array.Empty<RinseContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var waste1 = result.Containers.First(c => c.Type == LiquidContainerType.Waste && c.Position == 1);
        waste1.Status.Should().Be(expectedStatus);
    }

    [Trait("ProductRequirement", "PR_43")]
    [Trait("ModuleRequirement", "MR_5359")]
    [Theory]
    [InlineData(EnumRinseBottleError.RinseBottleVolumeReadError, ReagentPositionStatus.UndetectedVolume)]
    [InlineData(EnumRinseBottleError.RinseBottleCannotPumpLiquidError, ReagentPositionStatus.DisconnectedBottle)]
    public async Task Handle_MapsRinseErrorToStatus_Correctly(
        EnumRinseBottleError error,
        ReagentPositionStatus expectedStatus)
    {
        // Arrange
        var rinseContainer = CreateRinseContainer(1, 50, error, false);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(new List<RinseContainer> { rinseContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var rinse1 = result.Containers.First(c => c.Type == LiquidContainerType.Rinse && c.Position == 1);
        rinse1.Status.Should().Be(expectedStatus);
    }

    [Trait("ProductRequirement", "PR_737")]
    [Trait("ModuleRequirement", "MR_5349")]
    [Fact]
    public async Task Handle_SetsMissingBottleStatus_WhenBottleNotPresent()
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(Array.Empty<RinseContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Containers.Should().AllSatisfy(c => c.Status.Should().Be(ReagentPositionStatus.MissingBottle));
    }

    [Trait("ProductRequirement", "PR_289")]
    [Trait("ModuleRequirement", "MR_5343")]
    [Trait("ModuleRequirement", "MR_5347")]
    [Fact]
    public async Task Handle_SetsEmptyStatus_WhenDrawerOpenAndBottleNotPresent()
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(Array.Empty<RinseContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.Open, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Containers.Should().AllSatisfy(c => c.Status.Should().Be(ReagentPositionStatus.Empty));
    }

    [Trait("ProductRequirement", "PR_289")]
    [Trait("ModuleRequirement", "MR_5346")]
    [Trait("ProductRequirement", "PR_2161")]
    [Trait("ModuleRequirement", "MR_5368")]
    [Trait("ModuleRequirement", "MR_5369")]
    [Theory]
    [InlineData(LiquidContainerType.Waste, true, false)]
    [InlineData(LiquidContainerType.Rinse, false, true)]
    public async Task Handle_SetsPipedEnabled_WhenConfigured(
        LiquidContainerType containerType,
        bool configureWaste,
        bool configureRinse)
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(Array.Empty<RinseContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var instrumentSettings = CreateInstrumentSettings(
            wastePipedInstalled: configureWaste,
            wastePipedActive: configureWaste,
            rinsePipedInstalled: configureRinse,
            rinsePipedActive: configureRinse);
        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object, instrumentSettings);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Containers.Where(c => c.Type == containerType)
            .Should().AllSatisfy(c => c.IsPipedEnabled.Should().BeTrue());
    }

    [Trait("ProductRequirement", "PR_289")]
    [Trait("ModuleRequirement", "MR_5348")]
    [Fact]
    public async Task Handle_SetsDisabledStatus_WhenPipedEnabledAndNotInUse()
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(1, 45, EnumWasteContainerError.UnspecifiedWasteContainerError, inUse: false);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(Array.Empty<RinseContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var instrumentSettings = CreateInstrumentSettings(wastePipedInstalled: true, wastePipedActive: true);
        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object, instrumentSettings);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var waste1 = result.Containers.First(c => c.Type == LiquidContainerType.Waste && c.Position == 1);
        waste1.Status.Should().Be(ReagentPositionStatus.Disabled);
    }

    [Trait("ProductRequirement", "PR_1677")]
    [Trait("ModuleRequirement", "MR_5345")]
    [Theory]
    [InlineData(60, false, false, FillOrEmptyIndicationType.Pending, 90)]  // Waste > 50% = Pending
    [InlineData(45, false, true, FillOrEmptyIndicationType.Alarm, 90)]     // CapDispenseStopped = Alarm
    [InlineData(45, false, false, FillOrEmptyIndicationType.Warning, 30)]   // CapDispenseStopTime within warning window = Warning
    public async Task Handle_SetsCorrectFillOrEmptyForWaste(
        int percentageFull,
        bool inUse,
        bool capDispenseStopped,
        FillOrEmptyIndicationType expectedIndication,
        int timeCapDispenseStopOffsetMinutes)
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(
            1,
            percentageFull,
            EnumWasteContainerError.UnspecifiedWasteContainerError,
            inUse,
            capDispenseStopped,
            DateTime.UtcNow.AddMinutes(timeCapDispenseStopOffsetMinutes));
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(Array.Empty<RinseContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var waste1 = result.Containers.First(c => c.Type == LiquidContainerType.Waste && c.Position == 1);
        waste1.FillOrEmpty.Should().Be(expectedIndication);
    }

    [Fact]
    public async Task Handle_SetsNoneFillOrEmptyForWaste_WhenStopTimeIsPastAndPercentageBelowCutoff()
    {
        // Arrange
        var wasteContainer = CreateWasteContainer(
            1,
            45,
            EnumWasteContainerError.UnspecifiedWasteContainerError,
            inUse: false,
            capDispenseStopped: false,
            DateTime.UtcNow.AddMinutes(-30));
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(new List<WasteContainer> { wasteContainer });
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(Array.Empty<RinseContainer>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var waste1 = result.Containers.First(c => c.Type == LiquidContainerType.Waste && c.Position == 1);
        waste1.FillOrEmpty.Should().Be(FillOrEmptyIndicationType.None);
    }

    [Theory]
    [InlineData(40, false, false, FillOrEmptyIndicationType.Pending, 90)]  // Rinse < 50% = Pending
    [InlineData(75, false, true, FillOrEmptyIndicationType.Alarm, 90)]     // CapDispenseStopped = Alarm
    [InlineData(75, false, false, FillOrEmptyIndicationType.Warning, 30)]   // CapDispenseStopTime within warning window = Warning
    public async Task Handle_SetsCorrectFillOrEmptyForRinse(
        int percentageFull,
        bool inUse,
        bool capDispenseStopped,
        FillOrEmptyIndicationType expectedIndication,
        int timeCapDispenseStopOffsetMinutes)
    {
        // Arrange
        var rinseContainer = CreateRinseContainer(
            1,
            percentageFull,
            EnumRinseBottleError.UnspecifiedRinseBottleError,
            inUse,
            capDispenseStopped,
            DateTime.UtcNow.AddMinutes(timeCapDispenseStopOffsetMinutes));
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(new List<RinseContainer> { rinseContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var rinse1 = result.Containers.First(c => c.Type == LiquidContainerType.Rinse && c.Position == 1);
        rinse1.FillOrEmpty.Should().Be(expectedIndication);
    }

    [Fact]
    public async Task Handle_SetsNoneFillOrEmptyForRinse_WhenStopTimeIsPastAndPercentageAboveCutoff()
    {
        // Arrange
        var rinseContainer = CreateRinseContainer(
            1,
            75,
            EnumRinseBottleError.UnspecifiedRinseBottleError,
            inUse: false,
            capDispenseStopped: false,
            DateTime.UtcNow.AddMinutes(-30));
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
            .Returns(Array.Empty<WasteContainer>());
        reagentStorage
            .Setup(x => x.GetRinseContainers(_testInstrumentNumber))
            .Returns(new List<RinseContainer> { rinseContainer });

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var rinse1 = result.Containers.First(c => c.Type == LiquidContainerType.Rinse && c.Position == 1);
        rinse1.FillOrEmpty.Should().Be(FillOrEmptyIndicationType.None);
    }

    [Theory]
    [InlineData(LiquidContainerType.Waste)]
    [InlineData(LiquidContainerType.Rinse)]
    public async Task Handle_UsesDefaultLevel_WhenPercentageIsNegative(
        LiquidContainerType containerType)
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();

        if (containerType == LiquidContainerType.Waste)
        {
            var wasteContainer = CreateWasteContainer(1, -1, EnumWasteContainerError.UnspecifiedWasteContainerError, true);
            reagentStorage.Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
                .Returns(new List<WasteContainer> { wasteContainer });
            reagentStorage.Setup(x => x.GetRinseContainers(_testInstrumentNumber))
                .Returns(Array.Empty<RinseContainer>());
        }
        else
        {
            var rinseContainer = CreateRinseContainer(1, -1, EnumRinseBottleError.UnspecifiedRinseBottleError, true);
            reagentStorage.Setup(x => x.GetLiquidWaste(_testInstrumentNumber))
                .Returns(Array.Empty<WasteContainer>());
            reagentStorage.Setup(x => x.GetRinseContainers(_testInstrumentNumber))
                .Returns(new List<RinseContainer> { rinseContainer });
        }

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService).Setup(instrument => instrument.DrawersLockState[DrawerType.RinseWaste])
            .Returns(new DrawerLockState(DrawerType.RinseWaste, LockState.ClosedLocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetLiquidDrawerDetailsQuery { InstrumentNumber = _testInstrumentNumber };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var container = result.Containers.First(c => c.Type == containerType && c.Position == 1);
        var expectedLevel = containerType == LiquidContainerType.Waste
            ? Constants.DefaultWasteLevel
            : Constants.DefaultRinseLevel;
        container.Percentage.Should().Be(expectedLevel);
    }

    // Helpers
    private static GetLiquidDrawerDetailsQueryHandler CreateHandler(
        IInstrumentReagentStorage reagentStorage,
        IInstrumentManager instrumentService,
        InstrumentSettings? instrumentSettings = null)
    {
        var settings = instrumentSettings ?? CreateInstrumentSettings();
        var optionsSnapshot = new Mock<IOptionsSnapshot<InstrumentSettings>>();
        optionsSnapshot.Setup(x => x.Value).Returns(settings);
        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        return new GetLiquidDrawerDetailsQueryHandler(
            reagentStorage,
            instrumentService,
            optionsSnapshot.Object,
            dateTimeMock.Object);
    }

    private static WasteContainer CreateWasteContainer(
        int position,
        int percentageFull,
        EnumWasteContainerError error,
        bool inUse,
        bool capDispenseStopped = false,
        DateTime? timeCapDispenseStop = null)
    {
        var stopTime = timeCapDispenseStop ?? DateTime.UtcNow.AddHours(25);

        return new WasteContainer
        {
            Position = position,
            PercentageFull = percentageFull,
            Error = error,
            InUse = inUse,
            CapDispenseStopped = capDispenseStopped,
            TimeCapDispenseStop = Timestamp.FromDateTime(stopTime.ToUniversalTime())
        };
    }

    private static RinseContainer CreateRinseContainer(
        int position,
        int percentageFull,
        EnumRinseBottleError error,
        bool inUse,
        bool capDispenseStopped = false,
        DateTime? timeCapDispenseStop = null)
    {
        var stopTime = timeCapDispenseStop ?? DateTime.UtcNow.AddHours(25);

        return new RinseContainer
        {
            Position = position,
            PercentageFull = percentageFull,
            Error = error,
            InUse = inUse,
            CapDispenseStopped = capDispenseStopped,
            TimeCapDispenseStop = Timestamp.FromDateTime(stopTime.ToUniversalTime())
        };
    }

    private static InstrumentSettings CreateInstrumentSettings(
        bool wastePipedInstalled = false,
        bool wastePipedActive = false,
        bool rinsePipedInstalled = false,
        bool rinsePipedActive = false)
    {
        return new InstrumentSettings
        {
            Instruments =
            [
                new InstrumentSpecificSettings
                {
                    SerialNumber = _testInstrumentNumber,
                    Waste = new Waste
                    {
                        PipedOutWasteInstalled = wastePipedInstalled,
                        PipedOutActive = wastePipedActive
                    },
                    Rinse = new Rinse
                    {
                        PipedInRinseInstalled = rinsePipedInstalled,
                        PipedInActive = rinsePipedActive
                    }
                }
            ]
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

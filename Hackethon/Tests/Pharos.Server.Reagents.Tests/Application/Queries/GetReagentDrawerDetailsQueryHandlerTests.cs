using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetReagentDrawerDetailsQueryHandlerTests
{
    private const string _testInstrumentNumber = "Inst1";
    private const string _testCartridgeBarcode = "CART_123";
    private const string _testCarrierBarcode = "CARRIER_123";

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4511")]
    [Fact]
    public async Task Handle_ReturnsEmptyDrawerDetails_WhenNoLoadersPresent()
    {
        // Arrange
        var reagentDrawer = CreateReagentDrawer();
        var handler = CreateHandler(reagentDrawer);
        var query = new GetReagentDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.ReagentLoaders.Should().HaveCount(Constants.MaxReagentLoaders);
        result.ReagentLoaders.Should().AllSatisfy(loader => loader.IsEmpty.Should().BeTrue());
        result.DilutionPlateStacks.Should().HaveCount(Constants.MaxDilutionPlateStack);
        result.DilutionPlateStacks.Should().AllSatisfy(stack => stack.IsEmpty.Should().BeTrue());
    }

    [Trait("ProductRequirement", "PR_1358")]
    [Trait("ModuleRequirement", "MR_4491")]
    [Theory]
    [InlineData(LockState.ClosedLocked, false, false)]     // Locked, no scan
    [InlineData(LockState.Open, false, false)]              // Open, no scan
    [InlineData(LockState.ClosedLocked, false, true)]      // Locked, scanning
    public async Task Handle_SetsDrawerStateCorrectly(
        LockState lockState,
        bool isUnlockInProgress,
        bool scanningOngoing)
    {
        // Arrange
        var reagentDrawer = CreateReagentDrawer(scanningOngoing);
        var handler = CreateHandler(reagentDrawer, lockState: lockState, isUnlockInProgress: isUnlockInProgress);
        var query = new GetReagentDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LockState.Should().Be(lockState);
        result.IsUnlockInProgress.Should().Be(isUnlockInProgress);
        result.IsScanOngoing.Should().Be(scanningOngoing);
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_5028")]
    [Fact]
    public async Task Handle_SetsNeedEmptyLoader_WhenDrawerNeedsEmptyLoader()
    {
        // Arrange
        var reagentDrawer = CreateReagentDrawer();
        reagentDrawer.NeedEmptyLoader = true;
        var handler = CreateHandler(reagentDrawer);
        var query = new GetReagentDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.NeedEmptyLoader.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4512")]
    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4513")]
    [Fact]
    public async Task Handle_PopulatesLoaderWithCompartments_WhenLoaderIsPresent()
    {
        // Arrange
        var loader = CreateReagentLoader(1);
        var reagentDrawer = CreateReagentDrawer();
        reagentDrawer.ReagentLoaders.Add(loader);

        var handler = CreateHandler(reagentDrawer, instrumentType: InstrumentType.Medium);
        var query = new GetReagentDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var loaderDto = result.ReagentLoaders.First(l => l.Position == 1);
        loaderDto.IsEmpty.Should().BeFalse();
        loaderDto.Compartments.Should().HaveCount(Constants.MaxReagentCompartments);
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4514")]
    [Fact]
    public async Task Handle_PopulatesCartridgeCompartment_WhenCartridgePresent()
    {
        // Arrange
        var loader = CreateReagentLoader(1);
        var cartridge = CreateCartridge(1, 1, _testCartridgeBarcode);
        var reagentDrawer = CreateReagentDrawer();
        reagentDrawer.ReagentLoaders.Add(loader);

        var handler = CreateHandler(
            reagentDrawer,
            cartridges: new List<Cartridge> { cartridge },
            instrumentType: InstrumentType.Medium);
        var query = new GetReagentDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var loaderDto = result.ReagentLoaders.First(l => l.Position == 1);
        var compartment = loaderDto.Compartments.First(c => c.Position == 1);
        compartment.Type.Should().Be(CompartmentReagentType.Cartridge);
        compartment.IsEmpty.Should().BeFalse();
        compartment.CellContent.Should().HaveCount(1);
        compartment.CellContent[0].ReagentBarcode.Should().Be(_testCartridgeBarcode);
        compartment.CellContent[0].Status.Should().Be(ReagentPositionStatus.Identified);
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4570")]
    [Fact]
    public async Task Handle_SetsNotIdentifiedStatus_WhenCartridgeHasNoBarcode()
    {
        // Arrange
        var loader = CreateReagentLoader(1);
        var cartridge = CreateCartridge(1, 1, string.Empty);
        var reagentDrawer = CreateReagentDrawer();
        reagentDrawer.ReagentLoaders.Add(loader);
        var handler = CreateHandler(reagentDrawer, cartridges: new List<Cartridge> { cartridge });

        // Act
        var result = await handler.Handle(new GetReagentDrawerDetailsQuery(_testInstrumentNumber), TestContext.Current.CancellationToken);

        // Assert
        var cell = result.ReagentLoaders.First(loader => loader.Position == 1).Compartments.First(compartment => compartment.Position == 1).CellContent.Single();
        cell.Status.Should().Be(ReagentPositionStatus.NotIdentified);
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4514")]
    [Fact]
    public async Task Handle_PopulatesCarrierCompartment_WhenCarriersPresent()
    {
        // Arrange
        var loader = CreateReagentLoader(1);
        var carrier = CreateCarrier(1, 1, 1, _testCarrierBarcode, identified: true);
        var reagentDrawer = CreateReagentDrawer();
        reagentDrawer.ReagentLoaders.Add(loader);

        var handler = CreateHandler(
            reagentDrawer,
            carriers: new List<CarrierInformation> { carrier },
            instrumentType: InstrumentType.Medium);
        var query = new GetReagentDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var loaderDto = result.ReagentLoaders.First(l => l.Position == 1);
        var compartment = loaderDto.Compartments.First(c => c.Position == 1);
        compartment.Type.Should().Be(CompartmentReagentType.Carrier);
        compartment.IsEmpty.Should().BeFalse();
        compartment.CellContent.Should().HaveCount(Constants.MaxReagentCarriersInCompartment);

        var cell = compartment.CellContent.First(cc => cc.Position == 1);
        cell.ReagentBarcode.Should().Be(_testCarrierBarcode);
        cell.IsEmpty.Should().BeFalse();
        cell.Status.Should().Be(ReagentPositionStatus.Identified);
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4570")]
    [Fact]
    public async Task Handle_PreservesCartridgeErrorAndSetsUnloadRequested_WhenUnloadIsPending()
    {
        // Arrange
        var loader = CreateReagentLoader(1);
        var cartridge = CreateCartridge(1, 1, _testCartridgeBarcode, EnumCartridgeError.CartridgeGripError, unloadPending: true);
        var reagentDrawer = CreateReagentDrawer();
        reagentDrawer.ReagentLoaders.Add(loader);

        var handler = CreateHandler(reagentDrawer, cartridges: new List<Cartridge> { cartridge });

        // Act
        var result = await handler.Handle(new GetReagentDrawerDetailsQuery(_testInstrumentNumber), TestContext.Current.CancellationToken);

        // Assert
        var cell = result.ReagentLoaders.First(loader => loader.Position == 1).Compartments.First(compartment => compartment.Position == 1).CellContent.Single();
        cell.Status.Should().Be(ReagentPositionStatus.TransferNotPossible);
        cell.IsUnloadRequested.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4570")]
    [Fact]
    public async Task Handle_PreservesCarrierStatusAndSetsUnloadRequested_WhenUnloadIsPending()
    {
        // Arrange
        var loader = CreateReagentLoader(1);
        var carrier = CreateCarrier(1, 1, 1, _testCarrierBarcode, identified: true, unloadPending: true);
        var reagentDrawer = CreateReagentDrawer();
        reagentDrawer.ReagentLoaders.Add(loader);

        var handler = CreateHandler(reagentDrawer, carriers: new List<CarrierInformation> { carrier });

        // Act
        var result = await handler.Handle(new GetReagentDrawerDetailsQuery(_testInstrumentNumber), TestContext.Current.CancellationToken);

        // Assert
        var cell = result.ReagentLoaders.First(loader => loader.Position == 1).Compartments.First(compartment => compartment.Position == 1).CellContent.First(cell => cell.Position == 1);
        cell.Status.Should().Be(ReagentPositionStatus.Identified);
        cell.IsUnloadRequested.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_1396")]
    [Trait("ModuleRequirement", "MR_4570")]
    [Fact]
    public async Task Handle_PopulatesDilutionPlateStack_WhenPlatesPresent()
    {
        // Arrange
        var dilutionPlate = CreateDilutionPlate(1, numberOfPlates: 5);
        var reagentDrawer = CreateReagentDrawer();

        var handler = CreateHandler(reagentDrawer, dilutionPlates: new List<DilutionPlateStack> { dilutionPlate });
        var query = new GetReagentDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var plateStack = result.DilutionPlateStacks.First(p => p.Position == 1);
        plateStack.NumberOfPlates.Should().Be(5);
        plateStack.IsEmpty.Should().BeFalse();
        plateStack.Status.Should().Be(ReagentPositionStatus.Identified);
    }

    private static GetReagentDrawerDetailsQueryHandler CreateHandler(
        ReagentDrawer? reagentDrawer = null,
        List<Cartridge>? cartridges = null,
        List<CarrierInformation>? carriers = null,
        List<DilutionPlateStack>? dilutionPlates = null,
        LockState lockState = LockState.ClosedLocked,
        bool isUnlockInProgress = false,
        InstrumentType instrumentType = InstrumentType.Medium)
    {
        var instrumentService = new Mock<IInstrumentManager>();
        var drawerLockStates = new Mock<IDrawerLockStateList>();
        drawerLockStates
            .Setup(x => x[DrawerType.Reagent])
            .Returns(new DrawerLockState(DrawerType.Reagent, lockState, isUnlockInProgress));
        instrumentService
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(new TestInstrument("SN123", instrumentType, "TestInstrument")
            {
                DrawersLockState = drawerLockStates.Object
            });

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage.Setup(x => x.GetDrawer(_testInstrumentNumber)).Returns(reagentDrawer ?? CreateReagentDrawer());
        reagentStorage.Setup(x => x.GetCartridges(_testInstrumentNumber)).Returns(cartridges ?? new List<Cartridge>());
        reagentStorage.Setup(x => x.GetCarriers(_testInstrumentNumber)).Returns(carriers ?? new List<CarrierInformation>());
        reagentStorage.Setup(x => x.GetDilutionPlatesDrawer(_testInstrumentNumber)).Returns(dilutionPlates ?? new List<DilutionPlateStack>());

        return new GetReagentDrawerDetailsQueryHandler(instrumentService.Object, reagentStorage.Object);
    }

    private static ReagentDrawer CreateReagentDrawer(bool scanningOngoing = false)
    {
        return new ReagentDrawer
        {
            ScanningOngoing = scanningOngoing
        };
    }

    private static ReagentLoader CreateReagentLoader(int position, EnumReagentLoaderError error = EnumReagentLoaderError.UnspecifiedReagentLoaderError)
    {
        return new ReagentLoader
        {
            ReagentDrawerPosition = position,
            ReagentLoaderError = error
        };
    }

    private static Cartridge CreateCartridge(int drawerPosition, int compartmentPosition, string barcode = "CARTRIDGE_BARCODE", EnumCartridgeError error = EnumCartridgeError.UnspecifiedCartridgeError, bool unloadPending = false)
    {
        return new Cartridge
        {
            Position = new CartridgePosition
            {
                Area = EnumCartridgeArea.ReagentDrawerArea,
                PositionDrawer = new ReagentDrawerPosition
                {
                    DrawerPosition = drawerPosition,
                    CompartmentPosition = compartmentPosition
                }
            },
            ReagentBarcode = barcode,
            Error = error,
            UnloadPending = unloadPending
        };
    }

    private static CarrierInformation CreateCarrier(int drawerPosition, int compartmentPosition, int subCompartmentPosition, string barcode = "", bool identified = false, EnumCarrierError error = EnumCarrierError.UnspecifiedCarrierError, bool unloadPending = false)
    {
        return new CarrierInformation
        {
            Position = new CarrierPosition
            {
                PositionInternalStorage = 0,
                PositionDrawer = new ReagentDrawerPosition
                {
                    DrawerPosition = drawerPosition,
                    CompartmentPosition = compartmentPosition,
                    SubCompartmentPosition = subCompartmentPosition
                }
            },
            ReagentBarcode = barcode,
            ReagentIdentified = identified,
            Error = error,
            UnloadPending = unloadPending
        };
    }

    private static DilutionPlateStack CreateDilutionPlate(int position, int numberOfPlates = 0, EnumDilutionPlatePositionError error = EnumDilutionPlatePositionError.UnspecifiedDilutionPlatePositionError)
    {
        return new DilutionPlateStack
        {
            Position = position,
            NumberOfPlates = numberOfPlates,
            Error = error
        };
    }
}

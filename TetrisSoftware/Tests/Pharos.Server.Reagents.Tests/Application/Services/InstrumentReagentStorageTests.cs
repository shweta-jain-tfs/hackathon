using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;

namespace Pharos.Server.Reagents.Application.Services;

public class InstrumentReagentStorageTests
{
    private const string _instrumentNumber = "INSTR1";

    [Fact]
    public void GetDrawer_ReturnsNewDrawer_WhenNotExists()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        // Act
        var result = storage.GetDrawer(_instrumentNumber);
        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDrawer_AddsAndUpdatesDrawer()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        var drawer = new ReagentDrawer { ScanningOngoing = true };
        // Act
        var result = storage.UpdateDrawer(_instrumentNumber, drawer);
        // Assert
        result.DrawerChanged.Should().BeTrue();
        result.HasErrors.Should().BeFalse();
        var fetched = storage.GetDrawer(_instrumentNumber);
        fetched.ScanningOngoing.Should().BeTrue();
    }

    [Fact]
    public void UpdateDrawer_DetectsErrors()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        var drawer = new ReagentDrawer();
        drawer.ReagentLoaders.AddRange(
            new List<ReagentLoader>
            {
                new() { ReagentDrawerPosition = 1, ReagentLoaderError = EnumReagentLoaderError.ReagentLoaderErrorUnknownObject }
            });
        // Act
        var result = storage.UpdateDrawer(_instrumentNumber, drawer);
        // Assert
        result.HasErrors.Should().BeTrue();
        result.DrawerChanged.Should().BeTrue();
    }

    [Fact]
    public void GetCartridges_ReturnsEmptyList_WhenNotExists()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        // Act
        var result = storage.GetCartridges(_instrumentNumber);
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateCartridges_AddsAndUpdatesCartridges_FullList()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        var cartridge = new Cartridge
        {
            Position = new CartridgePosition { Area = EnumCartridgeArea.ReagentDrawerArea },
            ChangeType = EnumChangeType.AddedObject
        };
        // Act
        var result = storage.UpdateCartridges(_instrumentNumber, new List<Cartridge> { cartridge }, true);
        // Assert
        result.DrawerChanged.Should().BeTrue();
        result.InternalStorageChanged.Should().BeTrue();
        var fetched = storage.GetCartridges(_instrumentNumber);
        fetched.Should().ContainSingle();
    }

    [Fact]
    public void UpdateCartridges_AddsUpdatesAndDeletesCartridge_PartialList()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        var pos = new CartridgePosition { Area = EnumCartridgeArea.ReagentDrawerArea };
        var cartridge = new Cartridge
        {
            Position = pos,
            ChangeType = EnumChangeType.AddedObject,
            ReagentBarcode = "RB123",
            Error = EnumCartridgeError.UnspecifiedCartridgeError,
            UnloadPending = false
        };
        storage.UpdateCartridges(_instrumentNumber, new List<Cartridge> { cartridge }, true);
        // Act
        var updated = new Cartridge
        {
            Position = pos,
            ChangeType = EnumChangeType.UpdatedObject,
            ReagentBarcode = "RB123-UPDATED",
            Error = EnumCartridgeError.CartridgeStorageFullError,
            UnloadPending = true
        };
        storage.UpdateCartridges(_instrumentNumber, new List<Cartridge> { updated }, false);
        var fetched = storage.GetCartridges(_instrumentNumber);
        // Assert
        fetched.Should().ContainSingle();
        fetched[0].ReagentBarcode.Should().Be("RB123-UPDATED");
        fetched[0].Error.Should().Be(EnumCartridgeError.CartridgeStorageFullError);
        fetched[0].UnloadPending.Should().BeTrue();
        // Act
        var deleted = new Cartridge { Position = pos, ChangeType = EnumChangeType.DeletedObject };
        storage.UpdateCartridges(_instrumentNumber, new List<Cartridge> { deleted }, false);
        // Assert
        storage.GetCartridges(_instrumentNumber).Should().BeEmpty();
    }

    [Fact]
    public void GetCarriers_ReturnsEmptyList_WhenNotExists()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        // Act
        var result = storage.GetCarriers(_instrumentNumber);
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateCarriers_AddsAndUpdatesCarriers_FullList()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        var carrier = new CarrierInformation
        {
            Position = new CarrierPosition { PositionInternalStorage = 0 },
            ChangeType = EnumChangeType.AddedObject
        };
        // Act
        var result = storage.UpdateCarriers(_instrumentNumber, new List<CarrierInformation> { carrier }, true);
        // Assert
        result.DrawerChanged.Should().BeTrue();
        var fetched = storage.GetCarriers(_instrumentNumber);
        fetched.Should().ContainSingle();
    }

    [Fact]
    public void UpdateCarriers_AddsUpdatesAndDeletesCarrier_PartialList()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        var pos = new CarrierPosition { PositionInternalStorage = 0 };
        var carrier = new CarrierInformation
        {
            Position = pos,
            ChangeType = EnumChangeType.AddedObject,
            Error = EnumCarrierError.UnspecifiedCarrierError,
            UnloadPending = false
        };
        storage.UpdateCarriers(_instrumentNumber, new List<CarrierInformation> { carrier }, true);
        // Act
        var updated = new CarrierInformation
        {
            Position = pos,
            ChangeType = EnumChangeType.UpdatedObject,
            Error = EnumCarrierError.CarrierStorageFullError,
            UnloadPending = true
        };
        storage.UpdateCarriers(_instrumentNumber, new List<CarrierInformation> { updated }, false);
        var fetched = storage.GetCarriers(_instrumentNumber);
        // Assert
        fetched.Should().ContainSingle();
        fetched[0].Error.Should().Be(EnumCarrierError.CarrierStorageFullError);
        fetched[0].UnloadPending.Should().BeTrue();
        // Act
        var deleted = new CarrierInformation { Position = pos, ChangeType = EnumChangeType.DeletedObject };
        storage.UpdateCarriers(_instrumentNumber, new List<CarrierInformation> { deleted }, false);
        // Assert
        storage.GetCarriers(_instrumentNumber).Should().BeEmpty();
    }

    [Fact]
    public void GetDilutionPlatesDrawer_ReturnsEmptyList_WhenNotExists()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        // Act
        var result = storage.GetDilutionPlatesDrawer(_instrumentNumber);
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDilutionPlatesDrawer_AddsAndUpdatesPlates()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        var plate = new DilutionPlateStack { Position = 1, NumberOfPlates = 2 };
        // Act
        var changed = storage.UpdateDilutionPlatesDrawer(_instrumentNumber, new List<DilutionPlateStack> { plate }, true);
        // Assert
        changed.Should().BeTrue();
        var fetched = storage.GetDilutionPlatesDrawer(_instrumentNumber);
        fetched.Should().ContainSingle();
        fetched[0].NumberOfPlates.Should().Be(2);
    }

    [Fact]
    public void GetDilutionPlatesStation_ReturnsEmptyList_WhenNotExists()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        // Act
        var result = storage.GetDilutionPlatesStation(_instrumentNumber);
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDilutionPlatesStation_AddsUpdatesAndDeletesPlates()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        var plate = new DilutionPlateInStation { Position = 1, ChangeType = EnumChangeType.AddedObject };
        storage.UpdateDilutionPlatesStation(_instrumentNumber, new List<DilutionPlateInStation> { plate }, true);
        // Act
        var updated = new DilutionPlateInStation { Position = 1, ChangeType = EnumChangeType.UpdatedObject };
        storage.UpdateDilutionPlatesStation(_instrumentNumber, new List<DilutionPlateInStation> { updated }, false);
        var fetched = storage.GetDilutionPlatesStation(_instrumentNumber);
        // Assert
        fetched.Should().ContainSingle();
        // Act
        var deleted = new DilutionPlateInStation { Position = 1, ChangeType = EnumChangeType.DeletedObject };
        storage.UpdateDilutionPlatesStation(_instrumentNumber, new List<DilutionPlateInStation> { deleted }, false);
        // Assert
        storage.GetDilutionPlatesStation(_instrumentNumber).Should().BeEmpty();
    }

    [Fact]
    public void GetAvailablePositions_Carrier_ReturnsCapacityMinusUsedInternalPositions()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        storage.UpdateCarriers(_instrumentNumber,
        [
            new CarrierInformation { Position = new CarrierPosition { PositionInternalStorage = 1 }, ChangeType = EnumChangeType.AddedObject },
            new CarrierInformation { Position = new CarrierPosition { PositionInternalStorage = 2 }, ChangeType = EnumChangeType.AddedObject },
            new CarrierInformation { Position = new CarrierPosition { PositionInternalStorage = 0 }, ChangeType = EnumChangeType.AddedObject }
        ], true);

        // Act
        var result = storage.GetAvailablePositions(_instrumentNumber, ReagentStorageType.Carrier);

        // Assert
        result.Should().Be(Constants.MaxCarrierStoragePositions - 2);
    }

    [Fact]
    public void GetAvailablePositions_Cartridge_ReturnsCapacityMinusUsedStoragePositions()
    {
        // Arrange
        var storage = new InstrumentReagentStorage();
        storage.UpdateCartridges(_instrumentNumber,
        [
            new Cartridge { Position = new CartridgePosition { Area = EnumCartridgeArea.FrontStorageArea, PositionInternalStorage = 1 }, ChangeType = EnumChangeType.AddedObject },
            new Cartridge { Position = new CartridgePosition { Area = EnumCartridgeArea.RearStorageArea, PositionInternalStorage = 1 }, ChangeType = EnumChangeType.AddedObject },
            new Cartridge { Position = new CartridgePosition { Area = EnumCartridgeArea.ReagentDrawerArea, PositionInternalStorage = 1 }, ChangeType = EnumChangeType.AddedObject }
        ], true);

        // Act
        var result = storage.GetAvailablePositions(_instrumentNumber, ReagentStorageType.Cartridge);

        // Assert
        result.Should().Be(Constants.MaxCartridgeStoragePositionsFront + Constants.MaxCartridgeStoragePositionsRear - 2);
    }
}

using System.Collections.Concurrent;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Services;

internal class InstrumentReagentStorage : IInstrumentReagentStorage, IInstrumentReagentStorageUtilization
{
    private readonly ConcurrentDictionary<string, ReagentDrawer> _drawers = new();
    private readonly ConcurrentDictionary<string, List<CarrierInformation>> _carriers = new();
    private readonly ConcurrentDictionary<string, List<Cartridge>> _cartridges = new();
    private readonly ConcurrentDictionary<string, List<DilutionPlateStack>> _dilutionPlatesDrawer = new();
    private readonly ConcurrentDictionary<string, List<DilutionPlateInStation>> _dilutionPlatesStation = new();
    private readonly ConcurrentDictionary<string, List<BottledReagent>> _bottles = new();
    private readonly ConcurrentDictionary<string, List<WasteContainer>> _liquidWaste = new();
    private readonly ConcurrentDictionary<string, List<RinseContainer>> _rinseContainers = new();
    private readonly ConcurrentDictionary<string, List<WasteContainer>> _solidWaste = new();

    public ReagentDrawer GetDrawer(string instrumentNumber)
    {
        return _drawers.FirstOrDefault(x => x.Key == instrumentNumber).Value ?? new ReagentDrawer();
    }

    public ReagentStorageUpdateResult UpdateDrawer(string instrumentNumber, ReagentDrawer drawer)
    {
        var result = new ReagentStorageUpdateResult();
        var drawerChanged = false;
        _drawers.AddOrUpdate(
            instrumentNumber,
            _ =>
            {
                drawerChanged = true;
                return drawer;
            }, (_, oldValue) =>
            {
                drawerChanged |= oldValue.ScanningOngoing != drawer.ScanningOngoing;
                drawerChanged |= oldValue.NeedEmptyLoader != drawer.NeedEmptyLoader;
                drawerChanged |= oldValue.ReagentLoaders.Count != drawer.ReagentLoaders.Count;
                if (!drawerChanged)
                {
                    for (var i = 0; i < oldValue.ReagentLoaders.Count; i++)
                    {
                        var oldLoader = oldValue.ReagentLoaders[i];
                        var newLoader = drawer.ReagentLoaders[i];
                        drawerChanged |= oldLoader.ReagentDrawerPosition != newLoader.ReagentDrawerPosition;
                        drawerChanged |= oldLoader.ReagentLoaderError != newLoader.ReagentLoaderError;
                    }
                }
                return drawer;
            });
        result.DrawerChanged = drawerChanged;
        result.HasErrors = drawer.ReagentLoaders.Any(x => x.ReagentLoaderError == EnumReagentLoaderError.ReagentLoaderErrorUnknownObject);
        return result;
    }

    public IReadOnlyList<Cartridge> GetCartridges(string instrumentNumber)
    {
        return _cartridges.GetOrAdd(instrumentNumber, []);
    }

    public ReagentStorageUpdateResult UpdateCartridges(string instrumentNumber, IReadOnlyCollection<Cartridge> cartridges, bool fullList)
    {
        var result = new ReagentStorageUpdateResult();
        List<Cartridge> updatedCartridges;
        if (fullList)
        {
            result.DrawerChanged = true;
            result.InternalStorageChanged = true;
            updatedCartridges = cartridges.ToList();
        }
        else
        {
            if (cartridges.Count == 0)
            {
                // no changes
                return result;
            }
            updatedCartridges = _cartridges.GetOrAdd(instrumentNumber, []);
            foreach (var cartridge in cartridges)
            {
                if (cartridge.Position.Area == EnumCartridgeArea.ReagentDrawerArea)
                {
                    result.DrawerChanged = true;
                }
                else
                {
                    result.InternalStorageChanged = true;
                }

                if (cartridge.ChangeType == EnumChangeType.AddedObject)
                {
                    updatedCartridges.Add(cartridge);
                }
                else if (cartridge.ChangeType == EnumChangeType.DeletedObject)
                {
                    var index = updatedCartridges.FindIndex(x => x.Position.Equals(cartridge.Position));
                    if (index < 0)
                    {
                        throw new FluentValidation.ValidationException("The cartridge cannot be deleted from the storage, because it had not sent in the full storage message earlier.");
                    }
                    updatedCartridges.RemoveAt(index);
                }
                else if (cartridge.ChangeType == EnumChangeType.UpdatedObject)
                {
                    var index = updatedCartridges.FindIndex(x => x.Position.Equals(cartridge.Position));
                    if (index < 0)
                    {
                        throw new FluentValidation.ValidationException("The cartridge cannot be updated in the storage, because it had not sent in the full storage message earlier.");
                    }
                    updatedCartridges[index] = cartridge;
                }
                else
                {
                    throw new FluentValidation.ValidationException("Invalid change type for a cartridge.");
                }
            }
        }

        result.HasErrors = updatedCartridges.Any(x => x.Error is not (EnumCartridgeError.UnspecifiedCartridgeError or EnumCartridgeError.CartridgeStorageFullError));
        result.InternalStorageFull = updatedCartridges.Any(x => x.Error == EnumCartridgeError.CartridgeStorageFullError);

        _cartridges.AddOrUpdate(instrumentNumber, updatedCartridges, (_, _) => updatedCartridges);
        return result;
    }

    public IReadOnlyList<CarrierInformation> GetCarriers(string instrumentNumber)
    {
        return _carriers.GetOrAdd(instrumentNumber, []);
    }

    public ReagentStorageUpdateResult UpdateCarriers(string instrumentNumber, IReadOnlyCollection<CarrierInformation> carriers, bool fullList)
    {
        var result = new ReagentStorageUpdateResult();
        List<CarrierInformation> updatedCarriers;
        if (fullList)
        {
            result.DrawerChanged = true;
            updatedCarriers = carriers.ToList();
        }
        else
        {
            if (carriers.Count == 0)
            {
                // no changes
                return result;
            }
            updatedCarriers = _carriers.GetOrAdd(instrumentNumber, []);

            foreach (var carrier in carriers)
            {
                if (carrier.Position.PositionInternalStorage > 0)
                {
                    result.InternalStorageChanged = true;
                }
                else
                {
                    result.DrawerChanged = true;
                }

                if (carrier.ChangeType == EnumChangeType.AddedObject)
                {
                    updatedCarriers.Add(carrier);
                }
                else if (carrier.ChangeType == EnumChangeType.DeletedObject)
                {
                    var index = updatedCarriers.FindIndex(x => x.Position.Equals(carrier.Position));
                    if (index < 0)
                    {
                        throw new FluentValidation.ValidationException("The carrier cannot be deleted from the storage, because it had not sent in the full storage message earlier.");
                    }
                    updatedCarriers.RemoveAt(index);
                }
                else if (carrier.ChangeType == EnumChangeType.UpdatedObject)
                {
                    var index = updatedCarriers.FindIndex(x => x.Position.Equals(carrier.Position));
                    if (index < 0)
                    {
                        throw new FluentValidation.ValidationException("The carrier cannot be updated in the storage, because it had not sent in the full storage message earlier.");
                    }
                    updatedCarriers[index] = carrier;
                }
                else
                {
                    throw new FluentValidation.ValidationException("Invalid change type for a carrier.");
                }
            }
        }

        result.HasErrors = updatedCarriers.Any(x => x.Error is not (EnumCarrierError.UnspecifiedCarrierError or EnumCarrierError.CarrierStorageFullError));
        result.InternalStorageFull = updatedCarriers.Any(x => x.Error == EnumCarrierError.CarrierStorageFullError);

        _carriers.AddOrUpdate(instrumentNumber, updatedCarriers, (_, _) => updatedCarriers);
        return result;
    }

    public IReadOnlyList<DilutionPlateStack> GetDilutionPlatesDrawer(string instrumentNumber)
    {
        return _dilutionPlatesDrawer.GetOrAdd(instrumentNumber, _ => []);
    }

    public bool UpdateDilutionPlatesDrawer(string instrumentNumber, IReadOnlyCollection<DilutionPlateStack> plates, bool fullList)
    {
        var drawerChanged = false;
        List<DilutionPlateStack> updatedPlates;

        if (fullList)
        {
            drawerChanged = true;
            updatedPlates = plates.ToList();
        }
        else
        {
            if (plates.Count == 0)
            {
                // No changes
                return drawerChanged;
            }

            updatedPlates = _dilutionPlatesDrawer.GetOrAdd(instrumentNumber, _ => new List<DilutionPlateStack>());

            foreach (var plate in plates)
            {
                var indexToUpdate = updatedPlates.FindIndex(x => x.Position.Equals(plate.Position));
                if (indexToUpdate < 0)
                {
                    updatedPlates.Add(plate);
                }
                else
                {
                    updatedPlates[indexToUpdate] = plate;
                }
                drawerChanged = true;
            }
        }

        _dilutionPlatesDrawer.AddOrUpdate(instrumentNumber, updatedPlates, (_, _) => updatedPlates);
        return drawerChanged;
    }

    public IReadOnlyList<DilutionPlateInStation> GetDilutionPlatesStation(string instrumentNumber)
    {
        return _dilutionPlatesStation.GetOrAdd(instrumentNumber, _ => []);
    }

    public bool UpdateDilutionPlatesStation(string instrumentNumber, IReadOnlyCollection<DilutionPlateInStation> plates, bool fullList)
    {
        var stationChanged = false;
        List<DilutionPlateInStation> updatedPlates;

        if (fullList)
        {
            stationChanged = true;
            updatedPlates = plates.ToList();
        }
        else
        {
            if (plates.Count == 0)
            {
                // No changes
                return stationChanged;
            }

            updatedPlates = _dilutionPlatesStation.GetOrAdd(instrumentNumber, _ => new List<DilutionPlateInStation>());

            foreach (var plate in plates)
            {
                switch (plate.ChangeType)
                {
                    case EnumChangeType.AddedObject:
                        updatedPlates.Add(plate);
                        stationChanged = true;
                        break;

                    case EnumChangeType.DeletedObject:
                        var indexToDelete = updatedPlates.FindIndex(x => x.Position.Equals(plate.Position));
                        if (indexToDelete < 0)
                        {
                            throw new FluentValidation.ValidationException("The plate cannot be deleted from the station because it was not sent in the full storage message earlier.");
                        }
                        updatedPlates.RemoveAt(indexToDelete);
                        stationChanged = true;
                        break;

                    case EnumChangeType.UpdatedObject:
                        var indexToUpdate = updatedPlates.FindIndex(x => x.Position.Equals(plate.Position));
                        if (indexToUpdate < 0)
                        {
                            throw new FluentValidation.ValidationException("The plate cannot be updated in the station because it was not sent in the full storage message earlier.");
                        }
                        updatedPlates[indexToUpdate] = plate;
                        stationChanged = true;
                        break;

                    case EnumChangeType.UnspecifiedChangeType:
                    default:
                        throw new FluentValidation.ValidationException("Invalid change type for a dilution plate in the station.");
                }
            }
        }

        _dilutionPlatesStation.AddOrUpdate(instrumentNumber, updatedPlates, (_, _) => updatedPlates);
        return stationChanged;
    }

    public IReadOnlyList<BottledReagent> GetBottles(string instrumentNumber)
    {
        return _bottles.GetOrAdd(instrumentNumber, _ => new List<BottledReagent>());
    }

    public bool UpdateBottles(string instrumentNumber, IReadOnlyCollection<BottledReagent> bottles, bool fullList)
    {
        var bottlesChanged = false;
        List<BottledReagent> updatedBottles;

        if (fullList)
        {
            updatedBottles = bottles.ToList();
            bottlesChanged = true;
        }
        else
        {
            if (bottles.Count == 0)
            {
                return false;
            }

            updatedBottles = _bottles.GetOrAdd(instrumentNumber, _ => new List<BottledReagent>());

            foreach (var bottle in bottles)
            {
                switch (bottle.ChangeType)
                {
                    case EnumChangeType.AddedObject:
                        updatedBottles.Add(bottle);
                        bottlesChanged = true;
                        break;

                    case EnumChangeType.DeletedObject:
                        {
                            var indexToDelete = updatedBottles.FindIndex(x => x.Position == bottle.Position && x.ReagentType == bottle.ReagentType);
                            if (indexToDelete < 0)
                            {
                                throw new FluentValidation.ValidationException("The bottle cannot be deleted from the storage because it was not sent in the full storage message earlier.");
                            }
                            updatedBottles.RemoveAt(indexToDelete);
                            bottlesChanged = true;
                            break;
                        }

                    case EnumChangeType.UpdatedObject:
                        {
                            var indexToUpdate = updatedBottles.FindIndex(x => x.Position == bottle.Position && x.ReagentType == bottle.ReagentType);
                            if (indexToUpdate < 0)
                            {
                                throw new FluentValidation.ValidationException("The bottle cannot be updated in the storage because it was not sent in the full storage message earlier.");
                            }
                            updatedBottles[indexToUpdate] = bottle;
                            bottlesChanged = true;
                            break;
                        }

                    case EnumChangeType.UnspecifiedChangeType:
                    default:
                        throw new FluentValidation.ValidationException("Invalid change type for a bottled reagent.");
                }
            }
        }

        _bottles.AddOrUpdate(instrumentNumber, updatedBottles, (_, _) => updatedBottles);
        return bottlesChanged;
    }

    public IReadOnlyList<WasteContainer> GetLiquidWaste(string instrumentNumber)
    {
        return _liquidWaste.GetOrAdd(instrumentNumber, []);
    }

    public void UpdateLiquidWaste(string instrumentNumber, IReadOnlyCollection<WasteContainer> wasteContainers)
    {
        var updatedWaste = wasteContainers.ToList();
        _liquidWaste.AddOrUpdate(instrumentNumber, updatedWaste, (_, _) => updatedWaste);
    }

    public IReadOnlyList<RinseContainer> GetRinseContainers(string instrumentNumber)
    {
        return _rinseContainers.GetOrAdd(instrumentNumber, []);
    }

    public void UpdateRinseContainers(string instrumentNumber, IReadOnlyCollection<RinseContainer> rinseContainers)
    {
        var updatedRinse = rinseContainers.ToList();
        _rinseContainers.AddOrUpdate(instrumentNumber, updatedRinse, (_, _) => updatedRinse);
    }

    public IReadOnlyList<WasteContainer> GetSolidWaste(string instrumentNumber)
    {
        return _solidWaste.GetOrAdd(instrumentNumber, []);
    }

    public void UpdateSolidWaste(string instrumentNumber, IReadOnlyCollection<WasteContainer> solidWasteContainer)
    {
        var updatedSolidWaste = solidWasteContainer.ToList();
        _solidWaste.AddOrUpdate(instrumentNumber, updatedSolidWaste, (_, _) => updatedSolidWaste);
    }

    public int GetAvailablePositions(string instrumentNumber, ReagentStorageType storageType)
    {
        return storageType switch
        {
            ReagentStorageType.Carrier => Math.Max(0, Constants.MaxCarrierStoragePositions - GetUsedCarrierPositions(instrumentNumber)),
            ReagentStorageType.Cartridge => Math.Max(0, Constants.MaxCartridgeStoragePositionsFront + Constants.MaxCartridgeStoragePositionsRear - GetUsedCartridgePositions(instrumentNumber)),
            ReagentStorageType.DilutionPlate => Math.Max(0, (Constants.MaxDilutionPlateStack * Constants.MaxDilutionPlateNumber) - GetUsedDilutionPlatePositions(instrumentNumber)),
            _ => throw new ArgumentOutOfRangeException(nameof(storageType), storageType, null)
        };
    }

    private int GetUsedCarrierPositions(string instrumentNumber)
    {
        return GetCarriers(instrumentNumber)
            .Where(x => x.Position.PositionInternalStorage > 0)
            .Select(x => x.Position.PositionInternalStorage)
            .Distinct()
            .Count();
    }

    private int GetUsedCartridgePositions(string instrumentNumber)
    {
        return GetCartridges(instrumentNumber)
            .Where(x => x.Position.Area is EnumCartridgeArea.FrontStorageArea or EnumCartridgeArea.RearStorageArea)
            .Select(x => (x.Position.Area, x.Position.PositionInternalStorage))
            .Distinct()
            .Count();
    }
    
    private int GetUsedDilutionPlatePositions(string instrumentNumber)
    {
        return GetDilutionPlatesDrawer(instrumentNumber)
            .Sum(x => x.NumberOfPlates);
    }
}
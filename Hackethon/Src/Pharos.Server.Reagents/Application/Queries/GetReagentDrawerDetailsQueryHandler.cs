using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Localization;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

internal record GetReagentDrawerDetailsQuery(string InstrumentNumber) : IRequest<ReagentDrawerDto>;

internal class GetReagentDrawerDetailsQueryHandler(
    IInstrumentManager instrumentManager,
    IInstrumentReagentStorage reagentStorage) : IRequestHandler<GetReagentDrawerDetailsQuery, ReagentDrawerDto>
{
    private string _instrumentNumber = null!;

    public Task<ReagentDrawerDto> Handle(GetReagentDrawerDetailsQuery request, CancellationToken cancellationToken)
    {
        _instrumentNumber = request.InstrumentNumber;

        var reagentDrawerDto = FillReagentDrawerDetails();
        var drawerState = instrumentManager.GetInstrument(_instrumentNumber).DrawersLockState[DrawerType.Reagent];
        reagentDrawerDto.LockState = drawerState.LockState;
        reagentDrawerDto.IsUnlockInProgress = drawerState.IsUnlockInProgress;

        var reagentDrawer = reagentStorage.GetDrawer(_instrumentNumber);
        reagentDrawerDto.IsScanOngoing = reagentDrawer.ScanningOngoing;
        reagentDrawerDto.NeedEmptyLoader = reagentDrawer.NeedEmptyLoader;

        ProcessLoaders(reagentDrawerDto, reagentDrawer);
        ProcessDilutionPlateStacks(reagentDrawerDto);

        return Task.FromResult(reagentDrawerDto);
    }

    private static ReagentDrawerDto FillReagentDrawerDetails()
    {
        var reagentDrawerDto = new ReagentDrawerDto
        {
            ReagentLoaders = new List<ReagentLoaderDto>(Constants.MaxReagentLoaders),
            DilutionPlateStacks = new List<DilutionPlateStackDto>(Constants.MaxDilutionPlateStack)
        };

        for (var loaderPosition = 1; loaderPosition <= Constants.MaxReagentLoaders; loaderPosition++)
        {
            reagentDrawerDto.ReagentLoaders.Add(new ReagentLoaderDto
            {
                Position = loaderPosition,
                IsEmpty = true,
                Compartments = new List<ReagentCompartmentDto>()
            });
        }

        for (var plateStackPosition = 1; plateStackPosition <= Constants.MaxDilutionPlateStack; plateStackPosition++)
        {
            reagentDrawerDto.DilutionPlateStacks.Add(new DilutionPlateStackDto
            {
                Position = plateStackPosition,
                IsEmpty = true
            });
        }

        return reagentDrawerDto;
    }

    private static List<ReagentCompartmentDto> FillCompartmentDetails()
    {
        var reagentCompartmentsList = new List<ReagentCompartmentDto>();

        for (var compartmentPosition = 1; compartmentPosition <= Constants.MaxReagentCompartments; compartmentPosition++)
        {
            reagentCompartmentsList.Add(new ReagentCompartmentDto
            {
                Position = compartmentPosition,
                IsEmpty = true,
            });
        }

        return reagentCompartmentsList;
    }

    private static List<CompartmentCellContentDto> FillCompartmentCellContentDetails(int compartmentPosition)
    {
        var reagentCompartmentCellContentsList = new List<CompartmentCellContentDto>();

        for (var carrierPosition = 1; carrierPosition <= Constants.MaxReagentCarriersInCompartment; carrierPosition++)
        {
            reagentCompartmentCellContentsList.Add(new CompartmentCellContentDto
            {
                CompartmentPosition = compartmentPosition,
                Status = ReagentPositionStatus.Empty,
                Position = carrierPosition,
                IsEmpty = true,
            });
        }

        return reagentCompartmentCellContentsList;
    }

    private void ProcessLoaders(ReagentDrawerDto drawerDto, ReagentDrawer reagentDrawer)
    {
        foreach (var loader in drawerDto.ReagentLoaders)
        {
            var reagentLoader = reagentDrawer.ReagentLoaders.FirstOrDefault(x => x.ReagentDrawerPosition == loader.Position);
            loader.Location = ProcessLoaderLocation(reagentLoader, drawerDto.LockState, loader.Position);
            if (reagentLoader?.ReagentLoaderError == EnumReagentLoaderError.ReagentLoaderErrorUnknownObject)
            {
                loader.Status = ReagentPositionStatus.ReagentLoaderErrorUnknownObject;
                loader.IsEmpty = true;
                continue;
            }

            if (reagentLoader != null)
            {
                loader.IsEmpty = false;
                loader.Compartments = FillCompartmentDetails();
                ProcessCompartments(loader);
            }
        }
    }

    private string ProcessLoaderLocation(ReagentLoader? reagentLoader, LockState lockState, int position)
    {
        if (reagentLoader == null || lockState == LockState.Open)
        {
            return Translations.LoaderLocation_NotLoaded;
        }
        else
        {
            var instrumentDisplayName = instrumentManager.FindInstrument(_instrumentNumber)?.DisplayName ?? Translations.LoaderLocation_NotLoaded;
            return $"{instrumentDisplayName}, {Translations.LoaderPosition} {position}";
        }
    }

    private void ProcessCompartments(ReagentLoaderDto loader)
    {
        foreach (var compartment in loader.Compartments)
        {
            var cartridgesInLoaderCompartment = reagentStorage.GetCartridges(_instrumentNumber)
                .Where(x => x.Position.Area == EnumCartridgeArea.ReagentDrawerArea
                    && x.Position.PositionDrawer.DrawerPosition == loader.Position
                    && x.Position.PositionDrawer.CompartmentPosition == compartment.Position)
                .ToList();

            var carriersInLoaderCompartment = reagentStorage.GetCarriers(_instrumentNumber)
                .Where(x => x.Position.PositionInternalStorage == 0
                    && x.Position.PositionDrawer.DrawerPosition == loader.Position
                    && x.Position.PositionDrawer.CompartmentPosition == compartment.Position)
                .ToList();

            var cartridge = cartridgesInLoaderCompartment.FirstOrDefault();
            if (cartridge != null)
            {
                SetCartridgeCompartment(compartment, cartridge); //can only be one cartridge in a compartment
            }
            else if (carriersInLoaderCompartment.Count > 0)
            {
                SetCarrierCompartment(compartment, carriersInLoaderCompartment);
            }
            else
            {
                //Compartment when present and empty then with 72 carriers as default view
                compartment.IsEmpty = false;
                compartment.Type = CompartmentReagentType.Carrier;
                compartment.CellContent = FillCompartmentCellContentDetails(compartment.Position);
            }
        }
    }

    private static void SetCartridgeCompartment(ReagentCompartmentDto compartment, Cartridge cartridge)
    {
        compartment.Type = CompartmentReagentType.Cartridge;
        compartment.IsEmpty = false;
        compartment.CellContent =
        [
            new()
            {
                //For Cartridge compartment cell content always has 1 position
                Position = 1,
                CompartmentPosition = compartment.Position,
                IsEmpty = false,
                ReagentBarcode = cartridge.ReagentBarcode,
                Status = MapReagentPositionErrorToStatus(cartridge.ReagentBarcode.Length > 0, cartridge.Error),
                IsUnloadRequested = cartridge.UnloadPending
            }
        ];
    }

    private static void SetCarrierCompartment(ReagentCompartmentDto compartment, List<CarrierInformation> carriers)
    {
        compartment.Type = CompartmentReagentType.Carrier;
        compartment.IsEmpty = false;
        compartment.CellContent = FillCompartmentCellContentDetails(compartment.Position);
        foreach (var cell in compartment.CellContent)
        {
            var reagentSubCompartment = carriers.FirstOrDefault(x => x.Position.PositionDrawer.SubCompartmentPosition == cell.Position);
            if (reagentSubCompartment != null)
            {
                cell.IsEmpty = false;
                cell.ReagentBarcode = string.IsNullOrWhiteSpace(reagentSubCompartment.ReagentBarcode) ? string.Empty : reagentSubCompartment.ReagentBarcode;
                cell.Status = MapReagentPositionErrorToStatus(reagentSubCompartment.ReagentIdentified, reagentSubCompartment.Error);
                cell.IsUnloadRequested = reagentSubCompartment.UnloadPending;
            }
        }
    }

    private void ProcessDilutionPlateStacks(ReagentDrawerDto reagentDrawer)
    {
        foreach (var dilutionPlateStack in reagentDrawer.DilutionPlateStacks)
        {
            var reagentDilutionPlate = reagentStorage.GetDilutionPlatesDrawer(_instrumentNumber).FirstOrDefault(x => x.Position == dilutionPlateStack.Position);

            if (reagentDilutionPlate == null)
            {
                continue;
            }

            dilutionPlateStack.NumberOfPlates = reagentDilutionPlate.NumberOfPlates;
            dilutionPlateStack.IsEmpty = reagentDilutionPlate.NumberOfPlates == 0;
            dilutionPlateStack.Status = MapDilutionPlateErrorToStatus(reagentDilutionPlate.Error, reagentDilutionPlate.NumberOfPlates > 0);
        }
    }

    private static ReagentPositionStatus MapDilutionPlateErrorToStatus(EnumDilutionPlatePositionError error, bool isIdentified)
    {
        return error switch
        {
            EnumDilutionPlatePositionError.CouldNotPickupDilutionPlates => ReagentPositionStatus.DilutionPlateNotPickedUp,
            EnumDilutionPlatePositionError.CouldNotDetectDilutionPlates => ReagentPositionStatus.DilutionPlateNotDetected,
            _ => isIdentified ? ReagentPositionStatus.Identified : ReagentPositionStatus.Empty
        };
    }

    private static ReagentPositionStatus MapReagentPositionErrorToStatus(bool isIdentified, EnumCartridgeError error)
    {
        return error switch
        {
            EnumCartridgeError.UnreadableCartridgeBarcodeError => ReagentPositionStatus.UnreadableBarcode,
            EnumCartridgeError.CartridgeGripError => ReagentPositionStatus.TransferNotPossible,
            EnumCartridgeError.UnknownCartridgeError => ReagentPositionStatus.UnknownArticle,
            EnumCartridgeError.CartridgeStorageFullError => ReagentPositionStatus.InternalStorageFull,
            _ => MapReagentNonErrorStatus(isIdentified)
        };
    }

    private static ReagentPositionStatus MapReagentPositionErrorToStatus(bool isIdentified, EnumCarrierError error)
    {
        return error switch
        {
            EnumCarrierError.CarrierUnreadableBarcodeError => ReagentPositionStatus.UnreadableBarcode,
            EnumCarrierError.CarrierUnreadableOcrError => ReagentPositionStatus.UnreadableBarcode,
            EnumCarrierError.CarrierGripError => ReagentPositionStatus.TransferNotPossible,
            EnumCarrierError.CarrierUnknownCarrierError => ReagentPositionStatus.UnknownArticle,
            EnumCarrierError.CarrierStorageFullError => ReagentPositionStatus.InternalStorageFull,
            _ => MapReagentNonErrorStatus(isIdentified)
        };
    }

    private static ReagentPositionStatus MapReagentNonErrorStatus(bool isIdentified)
    {
        return isIdentified ? ReagentPositionStatus.Identified : ReagentPositionStatus.NotIdentified;
    }
}


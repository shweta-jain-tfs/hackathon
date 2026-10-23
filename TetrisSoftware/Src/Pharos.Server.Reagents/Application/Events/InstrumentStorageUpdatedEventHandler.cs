using Google.Protobuf.WellKnownTypes;
using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.LoadList;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain;
using Pharos.Server.Infrastructure;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Mapping;
using Pharos.Server.Reagents.Application.Services;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Events;

internal class InstrumentStorageUpdatedEventHandler(
    IInstrumentReagentStorage reagentStorage,
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub currentInstrumentHub,
    IInventoryHub inventoryHub,
    IMediator mediator,
    ICheckDrawerTaskCardService checkDrawerTaskCardService,
    IDateTime dateTime,
    IStorageToLoadListKeyService keyService) : INotificationHandler<InstrumentStorageUpdatedEvent>
{
    public async Task Handle(InstrumentStorageUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var instrumentNumber = notification.InstrumentNumber;
        var newStorage = notification.UpdatedStorage;

        var drawerUpdateResult = await HandleDrawerChanges(instrumentNumber, newStorage, cancellationToken);

        var carrierUpdateResult = await HandleCarriersChanges(instrumentNumber, newStorage, cancellationToken);

        var cartridgeUpdateResult = await HandleCartridgesChanges(instrumentNumber, newStorage, cancellationToken);

        var isDilutionPlateStackUpdated = await HandleDilutionPlateStacksChanges(instrumentNumber, newStorage);

        await HandleDilutionPlateStationsChanges(instrumentNumber, newStorage);

        var bottleUpdateResult = await HandleBottlesChanges(instrumentNumber, newStorage, cancellationToken);

        await HandleLiquidDrawerChanges(instrumentNumber, newStorage, cancellationToken);

        await HandleSolidWasteDrawerChanges(instrumentNumber, newStorage, cancellationToken);

        await HandleReagentDrawerTaskCard(instrumentNumber, cancellationToken);

        await HandleRemoveReagentsFromDrawerTaskCard(instrumentNumber, carrierUpdateResult.Result, cartridgeUpdateResult.Result, cancellationToken);

        if (drawerUpdateResult.DrawerChanged || carrierUpdateResult.Result.DrawerChanged || cartridgeUpdateResult.Result.DrawerChanged || isDilutionPlateStackUpdated)
        {
            var instrumentId = instrumentManager.FindInstrument(instrumentNumber)?.Id;
            if (instrumentId.HasValue)
            {
                await currentInstrumentHub.ReagentDrawerContentsChanged(instrumentId.Value);
            }
        }

        await mediator.Publish(new ReagentStorageUpdatedEvent(instrumentNumber, newStorage.IsFullMessage, carrierUpdateResult.LoadedAmount, cartridgeUpdateResult.LoadedAmount, bottleUpdateResult), cancellationToken);
    }

    private async Task<ReagentStorageUpdateResult> HandleDrawerChanges(string instrumentNumber, InstrumentStorage newStorage, CancellationToken cancellationToken)
    {
        //Skip drawer updates when a partial message does not include ReagentDrawer.
        if (!newStorage.IsFullMessage && newStorage.ReagentDrawer is null)
        {
            return new ReagentStorageUpdateResult();
        }

        var drawerUpdateResult = reagentStorage.UpdateDrawer(instrumentNumber, newStorage.ReagentDrawer);
        await HandleTaskCard(drawerUpdateResult.HasErrors, TaskCardType.ReagentLoaderWithError, instrumentNumber, cancellationToken);
        await HandleTaskCard(newStorage.ReagentDrawer.NeedEmptyLoader, TaskCardType.NeedEmptyLoader, instrumentNumber, cancellationToken, TaskCardState.Information);
        return drawerUpdateResult;
    }

    private async Task<(ReagentStorageUpdateResult Result, Dictionary<string, int> LoadedAmount)> HandleCarriersChanges(string instrumentNumber, InstrumentStorage newStorage, CancellationToken cancellationToken)
    {
        var carrierUpdateResult = reagentStorage.UpdateCarriers(instrumentNumber, newStorage.Carriers, newStorage.IsFullMessage);
        if (carrierUpdateResult.DrawerChanged || carrierUpdateResult.InternalStorageChanged)
        {
            await inventoryHub.CarriersListChanged();
        }

        var updatedCarriers = reagentStorage.GetCarriers(instrumentNumber);

        await HandleTaskCard(CheckInventoryCarrierErrors(updatedCarriers), TaskCardType.InventoryCarrierError, instrumentNumber, cancellationToken);

        var loadedAmount = GetLoadedAmount(newStorage, updatedCarriers);

        return (carrierUpdateResult, loadedAmount);
    }

    private Dictionary<string, int> GetLoadedAmount(InstrumentStorage newStorage, IReadOnlyList<CarrierInformation> updatedCarriers)
    {
        var loadedAmount = new Dictionary<string, int>();

        if (newStorage.IsFullMessage)
        {
            //Calculate total amount based on the updated carriers
            var carriersOfSameTest = updatedCarriers.Where(ValidReagent).GroupBy(keyService.GetKey);
            foreach (var item in carriersOfSameTest)
            {
                var count = 0;
                foreach (var carrierInformation in item)
                {
                    if (carrierInformation.ReagentIdentified && !carrierInformation.Content.Unusable)
                    {
                        count += carrierInformation.Content.RemainingQuantity;
                    }
                }

                loadedAmount[item.Key] = count;
            }
        }
        else
        {
            // get keys of the updated carriers from the newStorage.Carriers list
            var keys = newStorage.Carriers.Select(keyService.GetKey).ToHashSet();

            // get the total amount per carrier type
            foreach (var key in keys)
            {
                var totalAmount = updatedCarriers.Where(x => ValidReagent(x) && keyService.GetKey(x) == key).Sum(x => x.Content.RemainingQuantity);
                loadedAmount[key] = totalAmount;
            }
        }

        return loadedAmount;

        static bool ValidReagent(CarrierInformation carrier) => carrier.ReagentType != EnumReagentType.UnspecifiedReagentType &&
                                                                carrier.ReagentIdentified &&
                                                                carrier.Content is { Unusable: false } &&
                                                                carrier.Error == EnumCarrierError.CarrierNoError &&
                                                                !string.IsNullOrEmpty(carrier.CarrierOcr);
    }

    private async Task<(ReagentStorageUpdateResult Result, Dictionary<string, int> LoadedAmount)> HandleCartridgesChanges(string instrumentNumber, InstrumentStorage newStorage, CancellationToken cancellationToken)
    {
        var cartridgeUpdateResult = reagentStorage.UpdateCartridges(instrumentNumber, newStorage.Cartridges, newStorage.IsFullMessage);
        if (cartridgeUpdateResult.DrawerChanged || cartridgeUpdateResult.InternalStorageChanged)
        {
            await inventoryHub.CartridgesListChanged();
        }

        var updatedCartridges = reagentStorage.GetCartridges(instrumentNumber);

        await HandleTaskCard(CheckInventoryCartridgeErrors(updatedCartridges), TaskCardType.InventoryCartridgeError, instrumentNumber, cancellationToken);

        var loadedAmount = await GetLoadedAmount(newStorage, updatedCartridges);

        return (cartridgeUpdateResult, loadedAmount);
    }

    private async Task<Dictionary<string, int>> GetLoadedAmount(InstrumentStorage newStorage, IReadOnlyList<Cartridge> updatedCartridges)
    {
        var loadedAmount = new Dictionary<string, int>();

        if (newStorage.IsFullMessage)
        {
            foreach (var cartridge in updatedCartridges.Where(ValidReagent))
            {
                var key = await keyService.GetKey(cartridge);
                var amount = cartridge.Cells.Where(ValidCell).Sum(cell => cell.Content.RemainingQuantity);
                loadedAmount[key] = loadedAmount.GetValueOrDefault(key) + amount;
            }
        }
        else
        {
            // get keys of the updated carriers from the newStorage.Cartridges list
            var keys = new HashSet<string>();
            foreach (var cartridge in newStorage.Cartridges)
            {
                var key = await keyService.GetKey(cartridge);
                if (keys.Add(key))
                {
                    loadedAmount[key] = 0;
                }
            }

            foreach (var cartridge in updatedCartridges.Where(ValidReagent))
            {
                var key = await keyService.GetKey(cartridge);
                if (!keys.Contains(key))
                {
                    continue;
                }

                var amount = cartridge.Cells.Where(ValidCell).Sum(cell => cell.Content.RemainingQuantity);
                loadedAmount[key] = loadedAmount.GetValueOrDefault(key) + amount;
            }
        }

        return loadedAmount;

        static bool ValidCell(CartridgeCell cell) => cell.Content is { Unusable: false } &&
                                                     cell.Error == EnumCartridgeCellError.UnspecifiedCartridgeCellError;

        static bool ValidReagent(Cartridge cartridge) => cartridge.ReagentType != EnumReagentType.UnspecifiedReagentType &&
                                                         cartridge.ReagentIdentified &&
                                                         cartridge.Error == EnumCartridgeError.CartridgeNoError;
    }

    private async Task<bool> HandleDilutionPlateStacksChanges(string instrumentNumber, InstrumentStorage newStorage)
    {
        var isDilutionPlateStackUpdated = reagentStorage.UpdateDilutionPlatesDrawer(instrumentNumber, newStorage.DilutionPlateStacks, newStorage.IsFullMessage);
        if (isDilutionPlateStackUpdated)
        {
            await inventoryHub.DrawerDilutionPlatesChanged();
        }

        return isDilutionPlateStackUpdated;
    }

    private async Task<bool> HandleDilutionPlateStationsChanges(string instrumentNumber, InstrumentStorage newStorage)
    {
        var isDilutionPlateStationUpdated = reagentStorage.UpdateDilutionPlatesStation(instrumentNumber, newStorage.DilutionStationPlates, newStorage.IsFullMessage);
        if (isDilutionPlateStationUpdated)
        {
            await inventoryHub.InternalDilutionPlatesChanged();
        }

        return isDilutionPlateStationUpdated;
    }

    private async Task<Dictionary<string, int>> HandleBottlesChanges(string instrumentNumber, InstrumentStorage newStorage, CancellationToken cancellationToken)
    {
        var instrumentId = instrumentManager.GetInstrument(instrumentNumber).Id;
        foreach (var bottle in newStorage.BottledReagents)
        {
            if (bottle.ChangeType is EnumChangeType.AddedObject or EnumChangeType.DeletedObject)
            {
                var articleType = bottle.ReagentType.ToArticleType();
                await currentInstrumentHub.BottlePresenceChanged(instrumentId, (int)articleType, bottle.Position, bottle.ChangeType == EnumChangeType.AddedObject);
            }
        }

        var bottlesChanged = reagentStorage.UpdateBottles(instrumentNumber, newStorage.BottledReagents, newStorage.IsFullMessage);
        if (bottlesChanged)
        {
            await inventoryHub.BottlesListChanged();
            await currentInstrumentHub.BottleDrawerContentsChanged(instrumentId);
        }

        var updatedBottles = reagentStorage.GetBottles(instrumentNumber);

        await HandleTaskCard(CheckDrawerBottleErrors(updatedBottles), TaskCardType.BottleDrawerError, instrumentNumber, cancellationToken);

        return await GetLoadedAmount(newStorage, updatedBottles);
    }

    private async Task<Dictionary<string, int>> GetLoadedAmount(InstrumentStorage newStorage, IReadOnlyList<BottledReagent> updatedBottles)
    {
        var loadedAmount = new Dictionary<string, int>();

        if (newStorage.IsFullMessage)
        {
            foreach (var bottle in updatedBottles.Where(ValidReagent))
            {
                var key = await keyService.GetKey(bottle);
                loadedAmount[key] = loadedAmount.GetValueOrDefault(key) + bottle.Content.RemainingQuantity;
            }
        }
        else
        {
            // get keys of the updated carriers from the newStorage.Cartridges list
            var keys = new HashSet<string>();
            foreach (var bottle in newStorage.BottledReagents)
            {
                var key = await keyService.GetKey(bottle);
                if (keys.Add(key))
                {
                    loadedAmount[key] = 0;
                }
            }

            foreach (var bottle in updatedBottles.Where(ValidReagent))
            {
                var key = await keyService.GetKey(bottle);
                if (!keys.Contains(key))
                {
                    continue;
                }

                loadedAmount[key] = loadedAmount.GetValueOrDefault(key) + bottle.Content.RemainingQuantity;
            }
        }

        return loadedAmount;

        static bool ValidReagent(BottledReagent bottle) => bottle.ReagentType != EnumReagentType.UnspecifiedReagentType &&
                                                           bottle.Content is { Unusable: false } &&
                                                           bottle.Error == EnumBottledReagentError.UnspecifiedBottledReagentError;
    }

    private async Task HandleLiquidDrawerChanges(string instrumentNumber, InstrumentStorage newStorage, CancellationToken cancellationToken)
    {
        // Handle liquid waste containers
        reagentStorage.UpdateLiquidWaste(instrumentNumber, newStorage.LiquidWaste);

        // Handle rinse containers  
        reagentStorage.UpdateRinseContainers(instrumentNumber, newStorage.RinseContainers);

        var instrumentId = instrumentManager.GetInstrument(instrumentNumber).Id;
        await currentInstrumentHub.LiquidDrawerContentsChanged(instrumentId);

        await HandleLiquidBottleTaskCards(instrumentNumber, cancellationToken);
    }

    private async Task HandleSolidWasteDrawerChanges(string instrumentNumber, InstrumentStorage newStorage, CancellationToken cancellationToken)
    {
        reagentStorage.UpdateSolidWaste(instrumentNumber, newStorage.SolidWasteBins);

        var instrumentId = instrumentManager.GetInstrument(instrumentNumber).Id;
        await currentInstrumentHub.WasteDrawerContentsChanged(instrumentId);

        await HandleSolidWasteTaskCards(instrumentNumber, cancellationToken);
    }

    private async Task HandleTaskCard(bool hasError, TaskCardType type, string instrumentNumber, CancellationToken cancellationToken, TaskCardState state = TaskCardState.Warning)
    {
        if (hasError)
        {
            await mediator.Send(new TaskCardCommand(type, state, instrumentNumber), cancellationToken);
        }
        else
        {
            await mediator.Send(new RemoveTaskCardCommand(type, instrumentNumber), cancellationToken);
        }
    }

    private static bool CheckInventoryCarrierErrors(IReadOnlyCollection<CarrierInformation> carriers)
    {
        var hasErrors = carriers.Any(c => c.Position.PositionInternalStorage > 0
                                            && c.Error != EnumCarrierError.CarrierNoError
                                            && c.Error != EnumCarrierError.UnspecifiedCarrierError);

        return hasErrors;
    }

    private static bool CheckInventoryCartridgeErrors(IReadOnlyCollection<Cartridge> cartridges)
    {
        var hasErrors = cartridges.Any(c => (c.Position.Area == EnumCartridgeArea.FrontStorageArea
                                            || c.Position.Area == EnumCartridgeArea.RearStorageArea)
                                            && c.Error != EnumCartridgeError.CartridgeNoError
                                            && c.Error != EnumCartridgeError.UnspecifiedCartridgeError);

        return hasErrors;
    }

    private static bool CheckDrawerCarrierErrors(IReadOnlyCollection<CarrierInformation> carriers)
    {
        var hasErrors = carriers.Any(c => c.Position.PositionInternalStorage == 0
                                            && c.Error != EnumCarrierError.CarrierNoError
                                            && c.Error != EnumCarrierError.UnspecifiedCarrierError
                                            && c.Error != EnumCarrierError.CarrierStorageFullError);

        return hasErrors;
    }

    private static bool CheckDrawerCartridgeErrors(IReadOnlyCollection<Cartridge> cartridges)
    {
        var hasErrors = cartridges.Any(c => c.Position.Area == EnumCartridgeArea.ReagentDrawerArea
                                            && c.Error != EnumCartridgeError.CartridgeNoError
                                            && c.Error != EnumCartridgeError.UnspecifiedCartridgeError
                                            && c.Error != EnumCartridgeError.CartridgeStorageFullError);

        return hasErrors;
    }

    private static bool CheckDrawerBottleErrors(IReadOnlyCollection<BottledReagent> bottles)
    {
        var hasErrors = bottles.Any(b => b.Error != EnumBottledReagentError.UnspecifiedBottledReagentError);

        return hasErrors;
    }

    private async Task HandleReagentDrawerTaskCard(string instrumentNumber, CancellationToken cancellationToken)
    {
        var updatedCarriers = reagentStorage.GetCarriers(instrumentNumber);
        var updatedCartridges = reagentStorage.GetCartridges(instrumentNumber);

        var reagentDrawerHasErrors = CheckDrawerCarrierErrors(updatedCarriers) || CheckDrawerCartridgeErrors(updatedCartridges);

        await HandleTaskCard(reagentDrawerHasErrors, TaskCardType.ReagentDrawerError, instrumentNumber, cancellationToken);
    }

    private async Task HandleRemoveReagentsFromDrawerTaskCard(string instrumentNumber, ReagentStorageUpdateResult carrierUpdateResult, ReagentStorageUpdateResult cartridgeUpdateResult, CancellationToken cancellationToken)
    {
        var updatedCarriers = reagentStorage.GetCarriers(instrumentNumber);
        var updatedCartridges = reagentStorage.GetCartridges(instrumentNumber);

        var hasUnloadPending = updatedCarriers.Any(c => c.Position.PositionInternalStorage == 0 && c.UnloadPending)
           || updatedCartridges.Any(c => c.Position.Area == EnumCartridgeArea.ReagentDrawerArea && c.UnloadPending);

        if (carrierUpdateResult.InternalStorageFull || cartridgeUpdateResult.InternalStorageFull)
        {
            // Yellow task card: internal storage is full
            await HandleTaskCard(true, TaskCardType.RemoveReagentsFromDrawer, instrumentNumber, cancellationToken, TaskCardState.Warning);
        }
        else if (hasUnloadPending)
        {
            // Blue task card: manual unload is pending
            await HandleTaskCard(true, TaskCardType.RemoveReagentsFromDrawer, instrumentNumber, cancellationToken, TaskCardState.Information);
        }
        else
        {
            await HandleTaskCard(false, TaskCardType.RemoveReagentsFromDrawer, instrumentNumber, cancellationToken);
        }
    }

    private async Task HandleLiquidBottleTaskCards(string instrumentNumber, CancellationToken cancellationToken)
    {
        var updatedLiquidWaste = reagentStorage.GetLiquidWaste(instrumentNumber);
        var updatedRinseContainers = reagentStorage.GetRinseContainers(instrumentNumber);

        await checkDrawerTaskCardService.UpdateCheckLiquidDrawerTaskCard(instrumentNumber, cancellationToken);

        await HandleFillOrEmptyTaskCards(instrumentNumber, updatedLiquidWaste, updatedRinseContainers, cancellationToken);
    }

    private async Task HandleFillOrEmptyTaskCards(string instrumentNumber, IReadOnlyCollection<WasteContainer> liquidWasteContainers, IReadOnlyCollection<RinseContainer> rinseContainer, CancellationToken cancellationToken)
    {
        await CheckWasteNeedsEmptying(liquidWasteContainers, instrumentNumber, cancellationToken);

        await CheckRinseNeedsRefilling(rinseContainer, instrumentNumber, cancellationToken);
    }

    private async Task CheckWasteNeedsEmptying(IReadOnlyCollection<WasteContainer> wasteContainers, string instrumentNumber, CancellationToken cancellationToken)
    {
        var anyStopped = wasteContainers.Any(container => !container.InUse && container.CapDispenseStopped);
        if (anyStopped)
        {
            await HandleTaskCard(true, TaskCardType.LiquidEmptyWaste, instrumentNumber, cancellationToken, TaskCardState.Error);
            return;
        }

        var anyNearingStop = wasteContainers
            .Where(container =>
                !container.InUse &&
                !container.CapDispenseStopped &&
                IsWithinCapDispenseWarningWindow(container.TimeCapDispenseStop))
            .OrderBy(container => container.TimeCapDispenseStop)
            .FirstOrDefault();


        if (anyNearingStop != null)
        {
            var warningToErrorDelay = anyNearingStop.TimeCapDispenseStop.ToDateTime() - dateTime.UtcNow;

            await mediator.Send(new TaskCardCommand(
                TaskCardType.LiquidEmptyWaste,
                [
                    new EscalationRule(TaskCardState.Warning, TimeSpan.Zero),
                    new EscalationRule(TaskCardState.Error, warningToErrorDelay)
                ],
                warningToErrorDelay,
                instrumentNumber), cancellationToken);
            return;
        }

        await HandleTaskCard(false, TaskCardType.LiquidEmptyWaste, instrumentNumber, cancellationToken);
    }

    private async Task CheckRinseNeedsRefilling(IReadOnlyCollection<RinseContainer> rinseContainers, string instrumentNumber, CancellationToken cancellationToken)
    {
        var anyStopped = rinseContainers.Any(container => !container.InUse && container.CapDispenseStopped);
        if (anyStopped)
        {
            await HandleTaskCard(true, TaskCardType.LiquidRefillRinse, instrumentNumber, cancellationToken, TaskCardState.Error);
            return;
        }

        var anyNearingStop = rinseContainers
            .Where(container =>
                !container.InUse &&
                !container.CapDispenseStopped &&
                IsWithinCapDispenseWarningWindow(container.TimeCapDispenseStop))
            .OrderBy(container => container.TimeCapDispenseStop)
            .FirstOrDefault();

        if (anyNearingStop != null)
        {
            var warningToErrorDelay = anyNearingStop.TimeCapDispenseStop.ToDateTime() - dateTime.UtcNow;

            await mediator.Send(new TaskCardCommand(
                TaskCardType.LiquidRefillRinse,
                [
                    new EscalationRule(TaskCardState.Warning, TimeSpan.Zero),
                    new EscalationRule(TaskCardState.Error, warningToErrorDelay)
                ],
                warningToErrorDelay,
                instrumentNumber), cancellationToken);
            return;
        }

        await HandleTaskCard(false, TaskCardType.LiquidRefillRinse, instrumentNumber, cancellationToken);
    }

    private async Task HandleSolidWasteTaskCards(string instrumentNumber, CancellationToken cancellationToken)
    {
        var wasteContainers = reagentStorage.GetSolidWaste(instrumentNumber);
        var biohazardContainer = wasteContainers.FirstOrDefault(wc => wc.Type == EnumWasteContainerType.WasteContainerTypeBioHazard);
        var recyclableContainer = wasteContainers.FirstOrDefault(wc => wc.Type == EnumWasteContainerType.WasteContainerTypeRecyclable);

        await checkDrawerTaskCardService.UpdateCheckWasteDrawerTaskCard(instrumentNumber, cancellationToken);

        await CheckSolidWasteNeedsEmptying(biohazardContainer, recyclableContainer, instrumentNumber, cancellationToken);
    }

    private async Task CheckSolidWasteNeedsEmptying(WasteContainer? biohazardContainer, WasteContainer? recyclableContainer, string instrumentNumber, CancellationToken cancellationToken)
    {
        var wasteContainers = new[] { biohazardContainer, recyclableContainer }
            .Where(container => container != null)
            .Cast<WasteContainer>()
            .ToList();

        if (wasteContainers.Any(container => container.CapDispenseStopped))
        {
            await HandleTaskCard(true, TaskCardType.SolidWasteEmpty, instrumentNumber, cancellationToken, TaskCardState.Error);
            return;
        }

        var anyNearingStop = wasteContainers
            .Where(container =>
                !container.CapDispenseStopped &&
                IsWithinCapDispenseWarningWindow(container.TimeCapDispenseStop))
            .OrderBy(container => container.TimeCapDispenseStop)
            .FirstOrDefault();

        if (anyNearingStop != null)
        {
            var warningToErrorDelay = anyNearingStop.TimeCapDispenseStop.ToDateTime() - dateTime.UtcNow;

            await mediator.Send(new TaskCardCommand(
                TaskCardType.SolidWasteEmpty,
                [
                    new EscalationRule(TaskCardState.Warning, TimeSpan.Zero),
                    new EscalationRule(TaskCardState.Error, warningToErrorDelay)
                ],
                warningToErrorDelay,
                instrumentNumber), cancellationToken);
            return;
        }

        await HandleTaskCard(false, TaskCardType.SolidWasteEmpty, instrumentNumber, cancellationToken);
    }

    private bool IsWithinCapDispenseWarningWindow(Timestamp? timeCapDispenseStop)
    {
        if (timeCapDispenseStop == null)
        {
            return false;
        }

        var stopTime = timeCapDispenseStop.ToDateTime();
        var utcNow = dateTime.UtcNow;
        return stopTime >= utcNow && stopTime < utcNow.AddHours(Constants.DefaultCapDispenseWarningHours);
    }
}

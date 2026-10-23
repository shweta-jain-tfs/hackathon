using Pharos.Server.Application.LoadList;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.LoadList.Application.Events;
using Tetris.Protocol.Pharos.Consumables;

namespace Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;

internal class MissingConsumablesUpdatedCommandHandler(ILoadListService loadListService, IMediator mediator, IArticleUsageService articleUsageService, ILoadListKeyService keyService) : IRequestHandler<MissingConsumablesUpdatedCommand>
{
    public async Task Handle(MissingConsumablesUpdatedCommand request, CancellationToken cancellationToken)
    {
        var instrumentNumber = request.InstrumentNumber;

        //Reset quantity
        loadListService.ResetPlannedQuantity(instrumentNumber);

        foreach (var missingConsumable in request.MissingConsumablesInfo.MissingConsumables)
        {
            var timestamp = missingConsumable.TimeCapDispenseStop;

            DateTimeOffset? startTime =
                timestamp is null or { Seconds: 0, Nanos: 0 }
                    ? null
                    : timestamp.ToDateTimeOffset();

            if (startTime == DateTimeOffset.MinValue)
            {
                startTime = null;
            }

            var key = keyService.GetKey(missingConsumable);

            switch (missingConsumable.Type)
            {
                //Bottles
                case EnumMissingConsumableType.MissingStop:
                    await loadListService.UpdateMissingStop(key, missingConsumable, startTime, ArticleType.StopSolutionBottle, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                case EnumMissingConsumableType.MissingWashConcentrate:
                    await loadListService.UpdateMissingBottleReagents(key, missingConsumable, startTime, ArticleType.WashConcentrateBottle, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                case EnumMissingConsumableType.MissingWashAdditive:
                    await loadListService.UpdateMissingBottleReagents(key, missingConsumable, startTime, ArticleType.WashAdditiveBottle, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                //Carriers
                case EnumMissingConsumableType.MissingImmunoCapCarrier:
                    await loadListService.UpdateMissingCarrier(key, missingConsumable, startTime, ArticleType.ImmunoCapCarrier, instrumentNumber, articleUsageService, cancellationToken);
                    break;
                case EnumMissingConsumableType.MissingEliaWellCarrier:
                    await loadListService.UpdateMissingCarrier(key, missingConsumable, startTime, ArticleType.EliaWellCarrier, instrumentNumber, articleUsageService, cancellationToken);
                    break;
                case EnumMissingConsumableType.MissingEliaCalibrationWellCarrier:
                    await loadListService.UpdateMissingCarrier(key, missingConsumable, startTime, ArticleType.EliaCalibratorWellCarrier, instrumentNumber, articleUsageService, cancellationToken);
                    break;
                case EnumMissingConsumableType.MissingDilutionWellCarrier:
                    await loadListService.UpdateMissingCarrier(key, missingConsumable, startTime, ArticleType.DilutionWellCarrier, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                //Cartridges
                case EnumMissingConsumableType.MissingCalibrator:
                    await loadListService.UpdateMissingCalibrators(key, missingConsumable, startTime, ArticleType.CalibratorCartridge, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                case EnumMissingConsumableType.MissingConjugate:
                    await loadListService.UpdateMissingCartridges(key, missingConsumable, startTime, ArticleType.ConjugateCartridge, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                case EnumMissingConsumableType.MissingCurveControl:
                    await loadListService.UpdateMissingCurveControls(key, missingConsumable, startTime, ArticleType.CurveControlCartridge, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                case EnumMissingConsumableType.MissingDiluent:
                    await loadListService.UpdateMissingCartridges(key, missingConsumable, startTime, ArticleType.DiluentCartridge, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                case EnumMissingConsumableType.MissingDevelopment:
                    await loadListService.UpdateMissingCartridges(key, missingConsumable, startTime, ArticleType.DevelopmentSolutionCartridge, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                //Dilution plate
                case EnumMissingConsumableType.MissingDilutionPlateCell:
                    await loadListService.UpdateMissingDilutionCells(key, missingConsumable, startTime, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                //Sample
                case EnumMissingConsumableType.MissingSample:
                    loadListService.UpdateMissingSampleTubes(key, missingConsumable, startTime, instrumentNumber);
                    break;

                //Quality control tube or cartridge
                case EnumMissingConsumableType.MissingQualityControl:
                    await loadListService.UpdateMissingQualityControl(key, missingConsumable, startTime, instrumentNumber, articleUsageService, cancellationToken);
                    break;

                // These types do not have a direct mapping to an article, so we don't need to do anything with them here
                case EnumMissingConsumableType.MissingRinse:
                case EnumMissingConsumableType.MissingLiquidWasteSpace:
                case EnumMissingConsumableType.MissingBiohazardSolidWasteSpace:
                case EnumMissingConsumableType.MissingRecyclableSolidWasteSpace:
                case EnumMissingConsumableType.MissingMaintenanceConcentrate:
                case EnumMissingConsumableType.UnspecifiedMissingConsumableType:
                    break;
            }
        }

        await UpdateLoadListsAndNotify(instrumentNumber, cancellationToken);
    }

    private async Task UpdateLoadListsAndNotify(string instrumentNumber, CancellationToken cancellationToken)
    {
        await mediator.Publish(new BottleLoadListUpdatedEvent(), cancellationToken);
        await mediator.Publish(new CarrierLoadListUpdatedEvent(), cancellationToken);
        await mediator.Publish(new CartridgeLoadListUpdatedEvent(), cancellationToken);
        await mediator.Publish(new DilutionPlateLoadListUpdatedEvent(), cancellationToken);
        await mediator.Publish(new SampleTubeLoadListUpdatedEvent(), cancellationToken);

        var urgencyLevel = loadListService.GetUrgencyLevel(instrumentNumber);

        if (urgencyLevel.AnythingMissing)
        {
            await mediator.Send(new TaskCardCommand(TaskCardType.UrgentLoading, urgencyLevel.TaskCardState!.Value, urgencyLevel.ExpiresAt, instrumentNumber), cancellationToken);
        }
        else
        {
            await mediator.Send(new RemoveTaskCardCommand(TaskCardType.UrgentLoading, instrumentNumber), cancellationToken);
        }
    }
}
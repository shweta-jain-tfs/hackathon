using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Application.Reagents;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Events;

internal class ApplicationStartedEventHandler : INotificationHandler<ApplicationStartedEvent>, INotificationHandler<LoadListConfigurationUpdatedEvent>, INotificationHandler<ReagentStorageUpdatedEvent>
{
    private readonly ILoadListDbContext _dbContext;
    private readonly ILoadListService _loadListService;
    private readonly ILoadListKeyService _keyService;
    private readonly IPublisher _publisher;

    public ApplicationStartedEventHandler(ILoadListDbContext dbContext, ILoadListService loadListService, ILoadListKeyService keyService, IPublisher publisher)
    {
        _dbContext = dbContext;
        _loadListService = loadListService;
        _keyService = keyService;
        _publisher = publisher;
    }

    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        var items = await _dbContext.LoadListConfigurations.AsNoTracking().ToListAsync(cancellationToken);

        AddOrUpdateItems(items);
    }

    private void AddOrUpdateItems(List<LoadListConfiguration> items)
    {
        foreach (var item in items)
        {
            var key = _keyService.GetKey(item);
            switch (item.ArticleType)
            {
                case ArticleType.CalibratorCartridge:
                    _loadListService.UpdateTriggerCalibrator(key, item);
                    break;

                case ArticleType.ConjugateCartridge:
                case ArticleType.DevelopmentSolutionCartridge:
                case ArticleType.DiluentCartridge:
                    _loadListService.UpdateTriggerCartridge(key, item);
                    break;

                case ArticleType.CurveControlCartridge:
                    _loadListService.UpdateTriggerCurveControl(key, item);
                    break;

                case ArticleType.DilutionPlate:
                    _loadListService.UpdateTriggerDilutionPlate(key, item);
                    break;

                case ArticleType.DilutionWellCarrier:
                case ArticleType.EliaCalibratorWellCarrier:
                case ArticleType.EliaWellCarrier:
                case ArticleType.ImmunoCapCarrier:
                    _loadListService.UpdateTriggerCarrier(key, item);
                    break;

                case ArticleType.QualityControlCartridge:
                case ArticleType.QualityControlTube:
                    _loadListService.UpdateTriggerQualityControl(key, item);
                    break;

                case ArticleType.StopSolutionBottle:
                    _loadListService.UpdateTriggerStop(key, item);
                    break;

                case ArticleType.WashAdditiveBottle:
                case ArticleType.WashConcentrateBottle:
                    _loadListService.UpdateTriggerBottle(key, item);
                    break;

                //ignore these
                case ArticleType.FluoroCVial:
                case ArticleType.MaintenanceConcentrateBottle:
                case ArticleType.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Unexpected ArticleType: {item.ArticleType}");
            }
        }
    }
    
    public async Task Handle(LoadListConfigurationUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var items = await _dbContext.LoadListConfigurations.Where(x => notification.UpdatedLoadListConfigurationIds.Contains(x.Id)).AsNoTracking().ToListAsync(cancellationToken);

        AddOrUpdateItems(items);
    }

    public async Task Handle(ReagentStorageUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _loadListService.UpdateLoadedCarriers(notification.InstrumentNumber, notification.IsFullUpdate, notification.Carriers);
        _loadListService.UpdateLoadedCartridges(notification.InstrumentNumber, notification.IsFullUpdate, notification.Cartridges);
        _loadListService.UpdateLoadedBottles(notification.InstrumentNumber, notification.IsFullUpdate, notification.Bottles);

        if (notification.Bottles.Count > 0 || notification.IsFullUpdate)
        {
            await _publisher.Publish(new BottleLoadListUpdatedEvent(), cancellationToken);
        }

        if (notification.Carriers.Count > 0 || notification.IsFullUpdate)
        {
            await _publisher.Publish(new CarrierLoadListUpdatedEvent(), cancellationToken);
        }
        
        if(notification.Cartridges.Count > 0 || notification.IsFullUpdate)
        {
            await _publisher.Publish(new CartridgeLoadListUpdatedEvent(), cancellationToken);
        }
    }
}

internal record LoadListConfigurationUpdatedEvent(List<int> UpdatedLoadListConfigurationIds) : INotification;
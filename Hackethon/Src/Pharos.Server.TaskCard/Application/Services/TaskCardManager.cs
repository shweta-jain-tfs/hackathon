using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.TaskCard.Application.Services;

internal class TaskCardManager : BackgroundService
{
    private readonly ITaskCardService _taskCardService;
    private readonly IInstrumentManager _instrumentManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskCardManager> _logger;

    public TaskCardManager(ITaskCardService taskCardService, IInstrumentManager instrumentManager, IServiceScopeFactory scopeFactory, ILogger<TaskCardManager> logger)
    {
        _taskCardService = taskCardService;
        _instrumentManager = instrumentManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting TaskCard Manager");
        _taskCardService.TaskCardDeleted += TaskCardServiceOnTaskCardDeleted;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITaskCardDbContext>();

            var taskCards = await dbContext.TaskCards
                .Include(x => x.Rules)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            _taskCardService.Initialize(taskCards);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add Task Cards from database");
        }

        _taskCardService.TaskCardsChanged += TaskCardServiceOnTaskCardsChanged;
        _taskCardService.TaskCardAdded += TaskCardServiceOnTaskCardAdded;
        _taskCardService.TaskCardCreatedOrChanged += TaskCardServiceOnTaskCardCreatedOrChanged;

        await base.StartAsync(cancellationToken);
    }

    private async Task TaskCardServiceOnTaskCardDeleted(Domain.TaskCard taskCard)
    {
        if (taskCard.Id <= 0)
        {
            return;
        }

        //only remove task cards that are persisted
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ITaskCardDbContext>();

        // Delete by key so an already-removed task card is treated as a no-op instead of an EF concurrency error.
        await dbContext.TaskCards
            .Where(x => x.Id == taskCard.Id)
            .ExecuteDeleteAsync(CancellationToken.None);
    }

    private async Task TaskCardServiceOnTaskCardAdded(Domain.TaskCard taskCard)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ITaskCardDbContext>();

        await dbContext.TaskCards.AddAsync(taskCard);
        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping TaskCard Manager");
        _taskCardService.TaskCardsChanged -= TaskCardServiceOnTaskCardsChanged;
        _taskCardService.TaskCardAdded -= TaskCardServiceOnTaskCardAdded;
        _taskCardService.TaskCardDeleted -= TaskCardServiceOnTaskCardDeleted;
        _taskCardService.TaskCardCreatedOrChanged -= TaskCardServiceOnTaskCardCreatedOrChanged;

        return base.StopAsync(cancellationToken);
    }

    private async Task TaskCardServiceOnTaskCardCreatedOrChanged(Domain.TaskCard taskCard)
    {
        using var scope = _scopeFactory.CreateScope();
        var instrumentHub = scope.ServiceProvider.GetRequiredService<ICurrentInstrumentServerHub>();

        var instrumentIds = GetInstrumentIdsForTaskCard(taskCard);
        var cardType = GetClientTaskCardType(taskCard.CardType);
        var updates = instrumentIds
            .Select(instrumentId => instrumentHub.TaskCardCreatedOrChanged(instrumentId, (int)cardType, (int)taskCard.CurrentCardState));

        await Task.WhenAll(updates);
    }

    private static TaskCardType GetClientTaskCardType(TaskCardType taskCardType)
    {
        return taskCardType == TaskCardType.SystemError ? TaskCardType.Error : taskCardType;
    }

    private IEnumerable<int> GetInstrumentIdsForTaskCard(Domain.TaskCard taskCard)
    {
        if (!string.IsNullOrWhiteSpace(taskCard.InstrumentNumber))
        {
            var instrumentId = _instrumentManager.FindInstrument(taskCard.InstrumentNumber)?.Id;
            return instrumentId != null ? new[] { instrumentId.Value } : Array.Empty<int>();
        }

        return _instrumentManager.GetInstruments().Select(instrument => instrument.Id);
    }

    private async Task TaskCardServiceOnTaskCardsChanged()
    {
        using var scope = _scopeFactory.CreateScope();
        var hub = scope.ServiceProvider.GetRequiredService<ITaskCardHub>();
        await hub.TaskCardsUpdated();

        var instrumentHub = scope.ServiceProvider.GetRequiredService<ICurrentInstrumentServerHub>();
        var instruments = _instrumentManager.GetInstruments();

        var instrumentUpdates = instruments
            .Select(async instrument =>
            {
                var taskCardInfo = _taskCardService.GetTaskCardsForInstrument(instrument.SerialNumber);
                var taskCardCount = taskCardInfo.Count;
                var highestTaskCardState = taskCardCount == 0
                    ? 0
                    : (int)taskCardInfo.Max(x => x.CurrentCardState);

                await instrumentHub.TaskCardInformationChanged(instrument.Id, taskCardCount, highestTaskCardState);
            });

        await Task.WhenAll(instrumentUpdates);

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        await ControlSignalTowers(mediator);
    }

    private async Task ControlSignalTowers(ISender sender)
    {
        var controlTower = _taskCardService.GetControlSignalTowerRequests();
        await sender.Send(new MassControlSignalTowerCommand(controlTower.InstrumentSpecific, controlTower.AllInstruments));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _taskCardService.AutoEscalate();

            //TODO, how often should we auto escalate?
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }
}

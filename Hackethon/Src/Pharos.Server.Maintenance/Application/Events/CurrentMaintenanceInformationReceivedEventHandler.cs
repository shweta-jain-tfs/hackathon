using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Maintenance.Application.Commands;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Maintenance.Application.Events;

internal class CurrentMaintenanceInformationReceivedEventHandler : INotificationHandler<CurrentMaintenanceInformationReceivedEvent>
{
    private readonly IMaintenanceDbContext _dbContext;
    private readonly IMaintenanceActivityHub _hub;
    private readonly IMaintenanceDefinitionProvider _definitionProvider;
    private readonly ISender _sender;

    public CurrentMaintenanceInformationReceivedEventHandler(IMaintenanceDbContext dbContext, IMaintenanceActivityHub hub, IMaintenanceDefinitionProvider definitionProvider, ISender sender)
    {
        _dbContext = dbContext;
        _hub = hub;
        _definitionProvider = definitionProvider;
        _sender = sender;
    }

    public async Task Handle(CurrentMaintenanceInformationReceivedEvent notification, CancellationToken cancellationToken)
    {
        var activities = await _dbContext.Activities.Where(x => x.InstrumentNumber == notification.InstrumentNumber).ToListAsync(cancellationToken);

        foreach (var item in notification.Message.MaintenanceInformation)
        {
            UpdateActivities(activities, item);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _sender.Send(new CreateMaintenanceTaskCardsCommand(), cancellationToken);

        await _hub.MaintenanceActivitiesUpdated();
    }

    private void UpdateActivities(List<MaintenanceActivity> activities, MaintenanceInformation item)
    {
        var type = GetMaintenanceActivityType(item.Type);
        var activity = activities.FirstOrDefault(x => x.ActivityType == type);
        if (activity != null)
        {
            UpdateActivity(activity, item);
        }
    }

    private void UpdateActivity(MaintenanceActivity activity, MaintenanceInformation item)
    {
        if (item.Status == EnumMaintenanceStatus.MaintenanceStatusOk)
        {
            activity.LastCompleted = item.LastEnded.ToDateTimeOffset();
        }

        if (item.Status is EnumMaintenanceStatus.MaintenanceStatusAborted or EnumMaintenanceStatus.MaintenanceStatusNotPerformed)
        {
            activity.LastCompleted = DateTimeOffset.MinValue;
        }

        if (activity.ScheduledType is MaintenanceScheduledType.Scheduled or MaintenanceScheduledType.Both)
        {
            activity.Next = activity.LastCompleted.AddDays(_definitionProvider.GetDaysUntilNextScheduledEvent(activity.ActivityType));
        }
        else
        {
            activity.Next = null;
        }
    }

    private static MaintenanceActivityType? GetMaintenanceActivityType(EnumMaintenance itemType)
    {
        return itemType switch
        {
            EnumMaintenance.DailyRinse => MaintenanceActivityType.Rinse,
            EnumMaintenance.ExtendedRinse => MaintenanceActivityType.ExtendedRinse,
            EnumMaintenance.Cleaning => MaintenanceActivityType.Cleaning,
            EnumMaintenance.ManualCleaning => MaintenanceActivityType.ManualCleaning,
            _ => null
        };
    }
}
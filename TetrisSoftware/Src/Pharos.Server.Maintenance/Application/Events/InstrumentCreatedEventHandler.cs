using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Domain;
using Pharos.Server.Maintenance.Services;

namespace Pharos.Server.Maintenance.Application.Events;

internal sealed class InstrumentCreatedEventHandler : INotificationHandler<InstrumentCreatedEvent>
{
    private readonly IMaintenanceDbContext _dbContext;
    private readonly IMaintenanceDefinitionProvider _definitionProvider;

    public InstrumentCreatedEventHandler(IMaintenanceDbContext dbContext, IMaintenanceDefinitionProvider definitionProvider)
    {
        _dbContext = dbContext;
        _definitionProvider = definitionProvider;
    }

    public async Task Handle(InstrumentCreatedEvent notification, CancellationToken cancellationToken)
    {
        await EnsureAllActivitiesExists(notification.InstrumentNumber, cancellationToken);
    }

    private async Task EnsureAllActivitiesExists(string instrumentNumber, CancellationToken cancellation)
    {
        var definitions = _definitionProvider.GetDefinitions();

        var existingActivities = await _dbContext.Activities.Where(x => x.InstrumentNumber == instrumentNumber).ToListAsync(cancellation);

        foreach (var definition in definitions)
        {
            var activity = existingActivities.FirstOrDefault(x => x.ActivityType == definition.Key);
            if (activity is null)
            {
                CreateActivity(definition.Key, definition.Value, instrumentNumber);
            }
            else
            {
                UpdateActivity(activity, definition.Value);
            }
        }

        if (_dbContext.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellation);
        }
    }

    private void CreateActivity(MaintenanceActivityType activityType, MaintenanceDefinition item, string instrumentNumber)
    {
        var activity = new MaintenanceActivity
        {
            MaintenanceId = Guid.NewGuid(),
            ActivityType = activityType,
            Duration = item.Duration,
            LastCompleted = DateTimeOffset.MinValue,
            ScheduledType = item.ScheduledType,
            InstrumentNumber = instrumentNumber,
            Next = null
        };

        if (item.ScheduledType is MaintenanceScheduledType.Scheduled or MaintenanceScheduledType.Both)
        {
            SetNextDate(activity, item.DaysUntilNext);
        }

        _dbContext.Activities.Add(activity);
    }

    private static void SetNextDate(MaintenanceActivity activity, int? daysUntilNext)
    {
        if (!daysUntilNext.HasValue)
        {
            throw new ArgumentNullException(nameof(daysUntilNext), "Days until next date has to be provided");
        }
        activity.Next = DateTimeOffset.UtcNow.AddDays(daysUntilNext.Value);
    }

    private static void UpdateActivity(MaintenanceActivity activity, MaintenanceDefinition item)
    {
        if (activity.Duration != item.Duration)
        {
            activity.Duration = item.Duration;
        }

        if (activity.ScheduledType != item.ScheduledType)
        {
            if (item.ScheduledType == MaintenanceScheduledType.OnDemand)
            {
                activity.Next = null;
            }

            if (activity.ScheduledType == MaintenanceScheduledType.OnDemand && item.ScheduledType is MaintenanceScheduledType.Scheduled or MaintenanceScheduledType.Both)
            {
                SetNextDate(activity, item.DaysUntilNext);
            }
        }
    }
}
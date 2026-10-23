using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Domain;
using Tetris.Protocol.Common;

namespace Pharos.Server.Maintenance.Services;

internal record MaintenanceDefinition(
    MaintenanceScheduledType ScheduledType,
    int Duration,
    int? DaysUntilNext,
    EnumMaintenance MaintenanceType,
    MaintenanceTaskCardDefinition? TaskCardDefinition = null);

// Some of the maintenance activities can raise task cards
// On the escalation rules Trigger TimeSpan should be subtracted from the "next" date
internal record MaintenanceTaskCardDefinition(TaskCardType TaskCardType, List<EscalationRule> EscalationRules);

internal class MaintenanceDefinitionProvider(IDateTime dateTime) : IMaintenanceDefinitionProvider
{
    private readonly Dictionary<MaintenanceActivityType, MaintenanceDefinition> _definitions = new()
    {
        { MaintenanceActivityType.Cleaning, new MaintenanceDefinition(MaintenanceScheduledType.Scheduled, 15, 14, EnumMaintenance.Cleaning, new MaintenanceTaskCardDefinition(TaskCardType.MaintenanceCleaning, [new EscalationRule(TaskCardState.Hidden, TimeSpan.FromDays(6)), new EscalationRule(TaskCardState.Information, TimeSpan.FromDays(5)), new EscalationRule(TaskCardState.Warning, TimeSpan.FromDays(2)), new EscalationRule(TaskCardState.Error, TimeSpan.FromDays(0))])) },
        { MaintenanceActivityType.ManualCleaning, new MaintenanceDefinition(MaintenanceScheduledType.Scheduled, 45, 21, EnumMaintenance.ManualCleaning, new MaintenanceTaskCardDefinition(TaskCardType.MaintenanceManualCleaning, [new EscalationRule(TaskCardState.Hidden, TimeSpan.FromDays(6)), new EscalationRule(TaskCardState.Information, TimeSpan.FromDays(5)), new EscalationRule(TaskCardState.Warning, TimeSpan.FromDays(2)), new EscalationRule(TaskCardState.Error, TimeSpan.FromDays(0))])) },
        { MaintenanceActivityType.ExtendedRinse, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 45, 7, EnumMaintenance.ExtendedRinse) },
        { MaintenanceActivityType.Rinse, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 65, null, EnumMaintenance.DailyRinse) },
        { MaintenanceActivityType.PipetteCleaning, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 90, null, EnumMaintenance.ManualCleaning) },
        { MaintenanceActivityType.CleanWasteChute, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 25, null, EnumMaintenance.ManualCleaning) },
        { MaintenanceActivityType.CleanLiquidWasteContainer, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 45, null, EnumMaintenance.ManualCleaning) },
        { MaintenanceActivityType.CleanRinseContainer, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 70, null, EnumMaintenance.ManualCleaning) },
        { MaintenanceActivityType.CleanSolidWasteBins, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 60, null, EnumMaintenance.ManualCleaning) },
        { MaintenanceActivityType.CleanWashStations, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 5, null, EnumMaintenance.ManualCleaning) },
        { MaintenanceActivityType.CleanTransferUnitTips, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 80, null, EnumMaintenance.ManualCleaning) },
        { MaintenanceActivityType.CleanEjectUnitTips, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 30, null, EnumMaintenance.ManualCleaning) },
        { MaintenanceActivityType.InspectDispenseUnit, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 30, null, EnumMaintenance.ManualCleaning) },
        { MaintenanceActivityType.InspectCapWellRemovers, new MaintenanceDefinition(MaintenanceScheduledType.OnDemand, 20, null, EnumMaintenance.ManualCleaning) }
    };

    public IReadOnlyDictionary<MaintenanceActivityType, MaintenanceDefinition> GetDefinitions()
    {
        return _definitions.AsReadOnly();
    }

    public int GetDaysUntilNextScheduledEvent(MaintenanceActivityType activityType)
    {
        var item = _definitions[activityType];
        if (item.ScheduledType is MaintenanceScheduledType.Scheduled or MaintenanceScheduledType.Both)
        {
            if (!item.DaysUntilNext.HasValue)
            {
                throw new ArgumentNullException(nameof(activityType), "Days until next date has to be provided");
            }

            return item.DaysUntilNext.Value;
        }

        throw new ArgumentNullException(nameof(activityType), "The activity type is only on demand");
    }

    public EnumMaintenance GetMaintenanceType(MaintenanceActivityType activityType)
    {
        var item = _definitions[activityType];
        return item.MaintenanceType;
    }

    public bool IsAllowedToSendDone(MaintenanceActivityType activityType)
    {
        var item = _definitions[activityType];
        return item.MaintenanceType == EnumMaintenance.ManualCleaning;
    }

    public bool IsAllowedToBeMarkedAsPerformedFromClient(MaintenanceActivityType activityType)
    {
        var item = _definitions[activityType];
        return item.MaintenanceType == EnumMaintenance.ManualCleaning;
    }

    public MaintenanceTaskCardDefinition? GetTaskCardDefinition(MaintenanceActivityType activityType)
    {
        var item = _definitions[activityType];
        return item.TaskCardDefinition;
    }

    public MaintenanceActivityStatus GetMaintenanceStatus(MaintenanceActivityType activityType, DateTimeOffset? next)
    {
        if (!next.HasValue)
        {
            return MaintenanceActivityStatus.None;
        }

        var taskCardDefinition = GetTaskCardDefinition(activityType);

        if (taskCardDefinition?.EscalationRules is not null)
        {
            var timeUntilNext = next.Value.Subtract(dateTime.Now);

            // Find the current escalation level based on time remaining
            var highestEscalation = taskCardDefinition.EscalationRules
                .Where(r => timeUntilNext <= r.Trigger)
                .OrderBy(r => r.Trigger)
                .FirstOrDefault();

            return highestEscalation?.CardState switch
            {
                TaskCardState.Information => MaintenanceActivityStatus.DueLater,
                TaskCardState.Warning => MaintenanceActivityStatus.DueSoon,
                TaskCardState.Error => MaintenanceActivityStatus.Overdue,
                TaskCardState.FlashingRed => MaintenanceActivityStatus.Overdue,
                TaskCardState.Hidden or null or _ => MaintenanceActivityStatus.None
            };
        }

        return MaintenanceActivityStatus.None;
    }
}
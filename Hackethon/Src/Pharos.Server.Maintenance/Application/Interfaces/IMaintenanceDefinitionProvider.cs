using Pharos.Server.Maintenance.Domain;
using Pharos.Server.Maintenance.Services;
using Tetris.Protocol.Common;

namespace Pharos.Server.Maintenance.Application.Interfaces;

internal interface IMaintenanceDefinitionProvider
{
    IReadOnlyDictionary<MaintenanceActivityType, MaintenanceDefinition> GetDefinitions();
    int GetDaysUntilNextScheduledEvent(MaintenanceActivityType activityType);
    EnumMaintenance GetMaintenanceType(MaintenanceActivityType activityType);
    bool IsAllowedToSendDone(MaintenanceActivityType activityType);
    bool IsAllowedToBeMarkedAsPerformedFromClient(MaintenanceActivityType activityType);
    MaintenanceTaskCardDefinition? GetTaskCardDefinition(MaintenanceActivityType activityType);
    MaintenanceActivityStatus GetMaintenanceStatus(MaintenanceActivityType activityType, DateTimeOffset? next);
}
namespace Pharos.Contracts.Notifications;

public interface IMaintenanceActivityHub : IHub
{
    Task MaintenanceActivitiesUpdated();
}
namespace Pharos.Contracts.Notifications;
public interface IInventoryHub : IHub
{
    Task CarriersListChanged();
    Task CartridgesListChanged();
    Task BottlesListChanged();
    Task DrawerDilutionPlatesChanged();
    Task InternalDilutionPlatesChanged();
}

using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.Reagents.Models.Inventory.Carriers;
internal interface ICarriersNotificationProvider : INotificationProvider
{
    event Func<Task> CarriersListChanged;
}

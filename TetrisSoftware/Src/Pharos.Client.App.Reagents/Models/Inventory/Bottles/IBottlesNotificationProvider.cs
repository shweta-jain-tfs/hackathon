using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.Reagents.Models.Inventory.Bottles;
interface IBottlesNotificationProvider : INotificationProvider
{
    event Func<Task> BottlesListChanged;
}

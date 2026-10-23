using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.Reagents.Models.Inventory.Cartridges;
internal interface ICartridgesNotificationProvider : INotificationProvider
{
    event Func<Task> CartridgesListChanged;
}

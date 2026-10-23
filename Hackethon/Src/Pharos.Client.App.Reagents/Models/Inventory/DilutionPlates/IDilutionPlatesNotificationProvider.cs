using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.Reagents.Models.Inventory.DilutionPlates;

public interface IDilutionPlatesNotificationProvider : INotificationProvider
{
    event Func<Task> DrawerDilutionPlatesChanged;

    event Func<Task> InternalDilutionPlatesChanged;
}
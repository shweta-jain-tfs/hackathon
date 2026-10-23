namespace Pharos.Client.Common.Models.Notifications;

public interface IRefreshNotificationProvider
{
    event EventHandler RefreshNeeded;
}
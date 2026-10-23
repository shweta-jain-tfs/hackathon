namespace Pharos.Client.Common.Models.Notifications;

public interface ICurrentUserNotificationProvider : INotificationProvider
{
    event Func<Task> UserInfoChanged;
    event Func<Task> AutoLogoutSettingsChanged;
}

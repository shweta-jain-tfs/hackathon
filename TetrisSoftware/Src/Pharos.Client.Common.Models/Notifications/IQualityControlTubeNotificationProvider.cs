namespace Pharos.Client.Common.Models.Notifications;

public interface IQualityControlTubeNotificationProvider
{
    event EventHandler<string> TubeUpdated;
}

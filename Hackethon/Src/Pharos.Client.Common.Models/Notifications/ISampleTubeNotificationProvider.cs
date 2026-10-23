namespace Pharos.Client.Common.Models.Notifications;

public interface ISampleTubeNotificationProvider : INotificationProvider
{
    event EventHandler<int> SampleTubeUpdated;
}
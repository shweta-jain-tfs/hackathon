namespace Pharos.Contracts.Notifications;

public interface IQualityControlTubeHub : IHub
{
    Task TubeUpdated(string tubeId);
}
namespace Pharos.Contracts.Notifications;

public interface ISampleTubeHub : IHub
{
    Task SampleTubeUpdated(int requestId);
}
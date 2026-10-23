namespace Pharos.Contracts.Notifications;

public interface ILabCommunityConnectionHub : IHub
{
    Task LabCommunityStatusChanged();
}

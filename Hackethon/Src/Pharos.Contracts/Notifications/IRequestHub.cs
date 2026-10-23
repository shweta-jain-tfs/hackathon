namespace Pharos.Contracts.Notifications;

public interface IRequestHub : IEntityHub
{
    Task RequestsDeleted(List<int> requestIds);
}
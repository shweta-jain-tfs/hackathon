namespace Pharos.Contracts.Notifications;

public interface ITaskCardHub : IHub
{
    Task TaskCardsUpdated();
}
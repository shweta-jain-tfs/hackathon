namespace Pharos.Server.LoadList.Application.Events;

public record BottleLoadListUpdatedEvent : INotification;

internal class BottleLoadListUpdatedEventHandler : INotificationHandler<BottleLoadListUpdatedEvent>
{
    private readonly ILoadListHub _loadListHub;

    public BottleLoadListUpdatedEventHandler(ILoadListHub loadListHub)
    {
        _loadListHub = loadListHub;
    }

    public async Task Handle(BottleLoadListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await _loadListHub.BottleListChanged();
    }
}
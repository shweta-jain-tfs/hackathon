namespace Pharos.Server.LoadList.Application.Events;

public record CartridgeLoadListUpdatedEvent : INotification;

internal class CartridgeLoadListUpdatedEventHandler : INotificationHandler<CartridgeLoadListUpdatedEvent>
{
    private readonly ILoadListHub _loadListHub;

    public CartridgeLoadListUpdatedEventHandler(ILoadListHub loadListHub)
    {
        _loadListHub = loadListHub;
    }

    public async Task Handle(CartridgeLoadListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await _loadListHub.CartridgeListChanged();
    }
}
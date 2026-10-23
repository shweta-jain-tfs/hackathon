namespace Pharos.Server.LoadList.Application.Events;

public record CarrierLoadListUpdatedEvent : INotification;

internal class CarrierLoadListUpdatedEventHandler : INotificationHandler<CarrierLoadListUpdatedEvent>
{
    private readonly ILoadListHub _loadListHub;

    public CarrierLoadListUpdatedEventHandler(ILoadListHub loadListHub)
    {
        _loadListHub = loadListHub;
    }

    public async Task Handle(CarrierLoadListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await _loadListHub.CarrierListChanged();
    }
}

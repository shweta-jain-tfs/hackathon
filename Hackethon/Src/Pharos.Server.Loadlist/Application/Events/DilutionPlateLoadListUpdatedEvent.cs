namespace Pharos.Server.LoadList.Application.Events;

public record DilutionPlateLoadListUpdatedEvent : INotification;

internal class DilutionPlateLoadListUpdatedEventHandler : INotificationHandler<DilutionPlateLoadListUpdatedEvent>
{
    private readonly ILoadListHub _loadListHub;

    public DilutionPlateLoadListUpdatedEventHandler(ILoadListHub loadListHub)
    {
        _loadListHub = loadListHub;
    }

    public async Task Handle(DilutionPlateLoadListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await _loadListHub.DilutionPlateListChanged();
    }
}
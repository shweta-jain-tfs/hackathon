namespace Pharos.Server.LoadList.Application.Events;

public record SampleTubeLoadListUpdatedEvent : INotification;

internal class SampleTubeLoadListUpdatedEventHandler : INotificationHandler<SampleTubeLoadListUpdatedEvent>
{
    private readonly ILoadListHub _loadListHub;

    public SampleTubeLoadListUpdatedEventHandler(ILoadListHub loadListHub)
    {
        _loadListHub = loadListHub;
    }

    public async Task Handle(SampleTubeLoadListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await _loadListHub.SampleTubeListChanged();
    }
}
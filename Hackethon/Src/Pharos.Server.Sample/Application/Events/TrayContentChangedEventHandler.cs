using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Sample.Application.Events;

internal class TrayContentChangedEventHandler : INotificationHandler<InstrumentStorageUpdatedEvent>
{
    //private readonly SampleTrayHub _sampleTrayHub;

    //public TrayContentChangedEventHandler(SampleTrayHub sampleTrayHub)
    //{
    //    _sampleTrayHub = sampleTrayHub;
    //}

    public async Task Handle(InstrumentStorageUpdatedEvent notification, CancellationToken cancellationToken)
    {
        //TODO, look at notification.Old and notification.Updated to determine if there are any changes for any of the trays

        //await _sampleTrayHub.Updated();

        await Task.CompletedTask;
    }
}
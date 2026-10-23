using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Events;

internal class QualityControlFailedEventHandler : INotificationHandler<QualityControlFailedEvent>
{
    private readonly IInstrumentSampleStorage _storage;
    private readonly ISender _sender;

    public QualityControlFailedEventHandler(IInstrumentSampleStorage storage, ISender sender)
    {
        _storage = storage;
        _sender = sender;
    }

    public async Task Handle(QualityControlFailedEvent notification, CancellationToken cancellationToken)
    {
        var tube = _storage.GetTube(notification.TubeId);
        if (tube == null)
        {
            return; //don't return if exist in db
        }

        tube.InternalError = SampleTubeError.QcOutOfRange;

        //TODO, update database?
        //TODO, fire refresh to UI
        //TODO, task cards?

        if (string.IsNullOrEmpty(tube.InstrumentNumber))
        {
            return;
        }
        
        await _sender.Send(new SendMoveSampleQcCommand(tube.InstrumentNumber, notification.TubeId, Guid.NewGuid(), true, false), cancellationToken);
    }
}
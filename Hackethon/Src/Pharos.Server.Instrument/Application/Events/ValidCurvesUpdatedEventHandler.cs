using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Commands.SendCalibrationInformation;

namespace Pharos.Server.Instrument.Application.Events;

internal class ValidCurvesUpdatedEventHandler : INotificationHandler<ValidCurvesUpdatedEvent>
{
    private readonly ISender _sender;

    public ValidCurvesUpdatedEventHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task Handle(ValidCurvesUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _sender.Send(new SendCalibrationInformationCommand(notification.InstrumentNumber), cancellationToken);
        }
        catch
        {
            // We are ignoring exceptions here because the instrument could be offline.
            // In that case, the calibration information will be sent the next time the instrument connects.
        }
    }
}
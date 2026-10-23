using MediatR;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Events;

internal class CurrentMaintenanceInformationReceivedEventHandler(
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<CurrentMaintenanceInformationReceivedEvent>
{
    public async Task Handle(CurrentMaintenanceInformationReceivedEvent notification, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)instrumentManager.GetInstrument(notification.InstrumentNumber);

        if (instrument.MaintenanceNeeded == notification.Message.MaintenanceNeeded)
        {
            return;
        }

        instrument.MaintenanceNeeded = notification.Message.MaintenanceNeeded;
        await currentInstrumentHub.MaintenanceInfoChanged(instrument.Id, instrument.MaintenanceNeeded);
    }
}

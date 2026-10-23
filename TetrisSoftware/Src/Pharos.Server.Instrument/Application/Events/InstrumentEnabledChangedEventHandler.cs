using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Events;

internal class InstrumentEnabledChangedEventHandler(
    IInstrumentManager instrumentManager,
    ISender sender,
    ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<InstrumentEnabledChangedEvent>
{
    public async Task Handle(InstrumentEnabledChangedEvent notification, CancellationToken cancellationToken)
    {
        var instrument = instrumentManager.GetInstrument(notification.InstrumentNumber);

        await currentInstrumentHub.InstrumentEnabledChanged(instrument.Id, instrument.Enabled);

        if (instrument.Enabled && instrument.State == InstrumentState.Offline)
        {
            await sender.Send(new TaskCardCommand(TaskCardType.InstrumentSoftwareOffline, TaskCardState.Error, instrument.SerialNumber), cancellationToken);
        }
        else
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.InstrumentSoftwareOffline, instrument.SerialNumber), cancellationToken);
        }
    }
}

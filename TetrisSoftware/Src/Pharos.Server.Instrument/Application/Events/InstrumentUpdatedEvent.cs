using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Events;

internal record InstrumentUpdatedEvent(Domain.Instrument Instrument, bool NameChanged) : INotification;

internal class InstrumentUpdatedEventHandler(
    IInstrumentHub instrumentHub,
    ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<InstrumentUpdatedEvent>
{
    public async Task Handle(InstrumentUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var instrument = notification.Instrument;
        await instrumentHub.Updated(instrument.Id);
        if (notification.NameChanged)
        {
            await currentInstrumentHub.InstrumentNameChanged(instrument.Id, instrument.DisplayName ?? "");
        }
    }
}
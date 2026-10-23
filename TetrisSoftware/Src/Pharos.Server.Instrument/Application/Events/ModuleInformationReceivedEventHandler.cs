using MediatR;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Events;

internal class ModuleInformationReceivedEventHandler(
    ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<ModuleInformationReceivedEvent>
{
    public async Task Handle(ModuleInformationReceivedEvent notification, CancellationToken cancellationToken)
    {
        await currentInstrumentHub.ModuleInformationReceived(notification.InstrumentId);
    }
}

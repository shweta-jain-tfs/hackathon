using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Services;

namespace Pharos.Server.Instrument.Application.Events;

public class UnlockDrawerAuthorizationEventHandler(IDrawerAuthorizationService drawerAuthorizationService, ICurrentInstrumentServerHub currentInstrument, IInstrumentManager instrumentManager) : INotificationHandler<UnlockDrawerAuthorizationEvent>
{
    public async Task Handle(UnlockDrawerAuthorizationEvent notification, CancellationToken cancellationToken)
    {
        if (instrumentManager.FindInstrument(notification.InstrumentNumber) is Domain.Instrument instrument)
        {
            drawerAuthorizationService.Add(notification.InstrumentNumber, notification.Drawer);
            await currentInstrument.UnlockDrawerAuthorizationRequest(notification.InstrumentNumber, instrument.Id);
        }
    }
}

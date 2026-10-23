using MediatR;
using ICurrentInstrumentServerHub = Pharos.Server.Application.Instrument.ICurrentInstrumentServerHub;

namespace Pharos.Server.Instrument.Application.Events;

public record InstrumentEnvironmentChanged(int InstrumentId) : INotification;

internal class InstrumentEnvironmentChangedEventHandler : INotificationHandler<InstrumentEnvironmentChanged>
{
    private readonly ICurrentInstrumentServerHub _hub;

    public InstrumentEnvironmentChangedEventHandler(ICurrentInstrumentServerHub hub)
    {
        _hub = hub;
    }

    public async Task Handle(InstrumentEnvironmentChanged notification, CancellationToken cancellationToken)
    {
        await _hub.InstrumentEnvironmentChanged(notification.InstrumentId);
    }
}
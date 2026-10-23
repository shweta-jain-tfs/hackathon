using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Events;

internal class QualityControlSchedulingChangedEventHandler(ISender sender, IInstrumentManager instrumentManager, ICommunicationSessionManager sessionManager) : INotificationHandler<QcEnableDisableEvent>, INotificationHandler<QualityControlScheduleUpdatedEvent>
{
    public async Task Handle(QcEnableDisableEvent notification, CancellationToken cancellationToken)
    {
        await SendQualityControlScheduleToAllInstruments(cancellationToken);
    }

    public async Task Handle(QualityControlScheduleUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await SendQualityControlScheduleToAllInstruments(cancellationToken);
    }

    private async Task SendQualityControlScheduleToAllInstruments(CancellationToken cancellationToken)
    {
        var schedule = await sender.Send(new GetQualityControlSchedulingQuery(), cancellationToken);

        foreach (var instrument in instrumentManager.GetInstruments())
        {
            var session = sessionManager.GetSession(instrument.SerialNumber);
            if (session is { CompatibilityStatus: EnumCompatibilityStatus.FullyCompatible, IsSynchronized: true })
            {
                await session.Connection.SendQcSchedulingAsync(schedule, cancellationToken);
            }
        }
    }
}
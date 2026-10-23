using MediatR;
using Pharos.Contracts.Notifications;

namespace Pharos.Server.Result.Application.Events;

internal record CurveControlChartUpdatedEvent(string InstrumentNumber, string MethodName) : INotification;

internal sealed class CurveControlChartUpdatedEventHandler(IResultHub hub) : INotificationHandler<CurveControlChartUpdatedEvent>
{
    public Task Handle(CurveControlChartUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return hub.CurveControlChartUpdated(notification.InstrumentNumber, notification.MethodName);
    }
}
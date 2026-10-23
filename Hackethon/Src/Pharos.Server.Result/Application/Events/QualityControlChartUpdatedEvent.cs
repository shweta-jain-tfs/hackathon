using MediatR;
using Pharos.Contracts.Notifications;

namespace Pharos.Server.Result.Application.Events;

internal record QualityControlChartUpdatedEvent(string InstrumentNumber, string MethodName) : INotification;

internal sealed class QualityControlChartUpdatedEventHandler(IResultHub hub) : INotificationHandler<QualityControlChartUpdatedEvent>
{
    public Task Handle(QualityControlChartUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return hub.QualityControlChartUpdated(notification.InstrumentNumber, notification.MethodName);
    }
}
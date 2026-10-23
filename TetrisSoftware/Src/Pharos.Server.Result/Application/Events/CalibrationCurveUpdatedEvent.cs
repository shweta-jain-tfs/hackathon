using MediatR;
using Pharos.Contracts.Notifications;

namespace Pharos.Server.Result.Application.Events;

internal record CalibrationCurveChartUpdatedEvent(string InstrumentNumber, string MethodName) : INotification;

internal sealed class CalibrationCurveChartUpdatedEventHandler(IResultHub hub) : INotificationHandler<CalibrationCurveChartUpdatedEvent>
{
    public Task Handle(CalibrationCurveChartUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return hub.CalibratorCurveChartUpdated(notification.InstrumentNumber, notification.MethodName);
    }
}
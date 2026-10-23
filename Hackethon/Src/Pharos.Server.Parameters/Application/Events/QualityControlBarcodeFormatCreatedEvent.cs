using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Application.Events;

internal class QualityControlBarcodeFormatCreatedEvent(QualityControlBarcodeFormat entity) : EntityCreatedEvent<QualityControlBarcodeFormat>(entity);

internal sealed class QualityControlBarcodeFormatCreatedEventHandler(IBarcodeFormatHub hub) : INotificationHandler<QualityControlBarcodeFormatCreatedEvent>
{
    public async Task Handle(QualityControlBarcodeFormatCreatedEvent notification, CancellationToken cancellationToken)
    {
        //Send notification to clients
        await hub.Created(notification.NewEntity.Id);
    }
}
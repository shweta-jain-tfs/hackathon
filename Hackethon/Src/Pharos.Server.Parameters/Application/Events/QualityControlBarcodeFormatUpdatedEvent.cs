using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Application.Events;

internal class QualityControlBarcodeFormatUpdatedEvent(QualityControlBarcodeFormat entity) : EntityUpdatedEvent<QualityControlBarcodeFormat>(entity);

internal sealed class QualityControlBarcodeFormatUpdatedEventHandler(IBarcodeFormatHub hub) : INotificationHandler<QualityControlBarcodeFormatUpdatedEvent>
{
    public async Task Handle(QualityControlBarcodeFormatUpdatedEvent notification, CancellationToken cancellationToken)
    {
        //Send notification to clients
        await hub.Updated(notification.UpdatedEntity.Id);
    }
}
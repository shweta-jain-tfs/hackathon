using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Application.Events;

internal class QualityControlBarcodeFormatDeletedEvent(QualityControlBarcodeFormat entity) : EntityDeletedEvent<QualityControlBarcodeFormat>(entity);

internal sealed class QualityControlBarcodeFormatDeletedEventHandler(IBarcodeFormatHub hub) : INotificationHandler<QualityControlBarcodeFormatDeletedEvent>
{
    public async Task Handle(QualityControlBarcodeFormatDeletedEvent notification, CancellationToken cancellationToken)
    {
        //Send notification to clients
        await hub.Deleted(notification.DeletedEntity.Id);
    }
}
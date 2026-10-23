using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Instrument.Domain;
namespace Pharos.Server.Instrument.Application.Events;

internal record AssaySchedulerChangedEvent(AssayScheduler AssayScheduler) : INotification;

internal class AssaySchedulerChangedEventHandler(IInstrumentHub instrumentHub) : INotificationHandler<AssaySchedulerChangedEvent>
{
    public async Task Handle(AssaySchedulerChangedEvent notification, CancellationToken cancellationToken)
    {
        await instrumentHub.AssaySchedulerChanged(notification.AssayScheduler.InstrumentId, notification.AssayScheduler.Id, notification.AssayScheduler.IsActive, notification.AssayScheduler.PausedUntilDate);
    }
}
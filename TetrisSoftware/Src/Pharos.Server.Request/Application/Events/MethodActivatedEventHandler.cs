using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Request.Application.Jobs;
using Quartz;

namespace Pharos.Server.Request.Application.Events;

internal class MethodActivatedEventHandler(
    ISchedulerFactory schedulerFactory) : INotificationHandler<MethodActivatedEvent>
{
    public async Task Handle(MethodActivatedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Active)
        {
            // The instrument software must cancel all test orders for the method when deactivating the method.
            // We don't need to do anything here.
            return;
        }
        await schedulerFactory.TriggerTrySendAllNotStartedTestsToInstrumentsJobImmediately(cancellationToken);
    }
}

using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Request.Application.Jobs;
using Quartz;

namespace Pharos.Server.Request.Application.Events;

internal class MethodEnabledEventHandler(
    ISchedulerFactory schedulerFactory) : INotificationHandler<MethodEnabledEvent>
{
    public async Task Handle(MethodEnabledEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Enabled)
        {
            // The instrument software must cancel all test orders for the method when deactivating the method.
            // We don't need to do anything here.
            return;
        }
        await schedulerFactory.TriggerTrySendAllNotStartedTestsToInstrumentsJobImmediately(cancellationToken);
    }
}

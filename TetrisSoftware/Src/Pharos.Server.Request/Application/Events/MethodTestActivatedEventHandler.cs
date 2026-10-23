using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Request.Application.Commands;
using Pharos.Server.Request.Application.Jobs;
using Quartz;

namespace Pharos.Server.Request.Application.Events;

internal class MethodTestActivatedEventHandler(ISchedulerFactory schedulerFactory, ISender sender) : INotificationHandler<MethodTestActivatedEvent>
{
    public async Task Handle(MethodTestActivatedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Active)
        {
            await schedulerFactory.TriggerTrySendAllNotStartedTestsToInstrumentsJobImmediately(cancellationToken);
            return;
        }

        await sender.Send(new CancelTestOrdersForMethodCommand(notification.MethodName, notification.TestName), cancellationToken);
    }
}

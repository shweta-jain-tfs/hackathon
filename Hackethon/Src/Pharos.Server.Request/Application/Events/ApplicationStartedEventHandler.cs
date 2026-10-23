using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Request.Application.Commands.RequestIssue;
using Pharos.Server.Request.Application.Jobs;
using Quartz;

namespace Pharos.Server.Request.Application.Events;

internal class ApplicationStartedEventHandler(ISchedulerFactory schedulerFactory, ISender sender) : INotificationHandler<ApplicationStartedEvent>
{
    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        await schedulerFactory.ConfigureTrySendAllNotStartedTestsToInstrumentsJob(cancellationToken);
        await sender.Send(new SyncNotReportedRequestIssuesCommand(), cancellationToken);
    }
}

using MediatR;
using Pharos.Server.Application.Lis;
using Pharos.Server.Lis.Application.Jobs;
using Quartz;

namespace Pharos.Server.Lis.Application.Commands;

internal sealed class RemoveMissingTestOrdersJobCommandHandler(ISchedulerFactory schedulerFactory) : IRequestHandler<RemoveMissingTestOrdersJobCommand>
{
    public async Task Handle(RemoveMissingTestOrdersJobCommand request, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        foreach (var sampleId in request.SampleIds)
        {
            var jobKey = PublishMissingTestOrdersJob.Key(sampleId);
            await scheduler.DeleteJob(jobKey, cancellationToken);
        }
    }
}
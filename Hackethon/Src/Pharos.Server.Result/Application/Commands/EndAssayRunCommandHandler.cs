using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Result;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Job;
using Pharos.Server.Result.Domain;
using Quartz;

namespace Pharos.Server.Result.Application.Commands;

internal sealed class EndAssayRunCommandHandler(
    IResultDbContext dbContext,
    ISchedulerFactory schedulerFactory,
    IMediator mediator,
    ILogger<EndAssayRunCommandHandler> logger) : IRequestHandler<EndAssayRunCommand>
{
    public async Task Handle(EndAssayRunCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Assay run complete: Instrument = {Instrument}, AssayRunId = {AssayRunId}, CustomRunId = {CustomRunId}", request.InstrumentNumber, request.AssayRunId, request.CustomRunId);

        var runsToCleanup = await dbContext.AnalyticalRuns
            .Where(x => x.InstrumentNumber == request.InstrumentNumber
                        && x.InstrumentRunId.ToString() == request.AssayRunId
                        && (x.EndDateTime == null || x.RunStatus == RunStatus.Processing))
            .ToListAsync(cancellationToken);

        foreach (var run in runsToCleanup)
        {
            await mediator.Send(
                new EndAnalyticalRunCommand(
                    request.InstrumentNumber,
                    run.MethodName,
                    run.InstrumentAnalyticalRunId.ToString(),
                    run.InstrumentRunId.ToString()),
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(request.CustomRunId))
        {
            return;
        }

        // run the result export in a background job to avoid blocking the gPRC call, as it could be a long-running operation.
        var job = JobBuilder.Create<ExportCustomRunJob>()
            .WithIdentity(ExportCustomRunJob.Key)
            .UsingJobData(new JobDataMap {
                { ExportCustomRunJob.CustomRunId, request.CustomRunId },
                { ExportCustomRunJob.InstrumentNumber, request.InstrumentNumber },
            })
            .Build();
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.AddJob(job, replace: true, storeNonDurableWhileAwaitingScheduling: true, cancellationToken: cancellationToken);
        await scheduler.TriggerJob(ExportCustomRunJob.Key, cancellationToken);
    }
}
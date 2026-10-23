using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Job;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;
using Quartz;

namespace Pharos.Server.Result.Application.Commands;

internal sealed class EndAnalyticalRunCommandHandler(
    IResultDbContext dbContext,
    ILogger<EndAnalyticalRunCommandHandler> logger,
    IDateTime dateTime,
    ISchedulerFactory schedulerFactory,
    IMediator mediator) : IRequestHandler<EndAnalyticalRunCommand>
{
    public async Task Handle(EndAnalyticalRunCommand request, CancellationToken cancellationToken)
    {
        var ar = await dbContext.AnalyticalRuns
            .FirstOrDefaultAsync(x => x.InstrumentNumber == request.InstrumentNumber && x.InstrumentAnalyticalRunId.ToString() == request.AnalyticalRunId, cancellationToken)
            .ConfigureAwait(false);

        if (ar == null)
        {
            // we do not throw an exception here, as this could be a cached message with on a Pharos with an empty database.
            logger.LogError("Analytical run is not found: Instrument = {Instrument}, MethodName = '{MethodName}', AnalyticalRunId = {AnalyticalRunId}", request.InstrumentNumber, request.MethodName, request.AnalyticalRunId);
            return;
        }

        if (ar is { EndDateTime: not null, RunStatus: RunStatus.Ready })
        {
            // This is a repeated message (could happen during FLR-import), we just log a message and ignore it.
            logger.LogInformation("Analytical run has already been ended: Instrument = {Instrument}, MethodName = '{MethodName}', AnalyticalRunId = {AnalyticalRunId}", request.InstrumentNumber, request.MethodName, request.AnalyticalRunId);
            return;
        }

        if (ar is { EndDateTime: not null, RunStatus: RunStatus.Processing })
        {
            logger.LogInformation("Analytical run has already been ended but state is Processing (will now be processed again): Instrument = {Instrument}, MethodName = '{MethodName}', AnalyticalRunId = {AnalyticalRunId}", request.InstrumentNumber, request.MethodName, request.AnalyticalRunId);
        }

        await ResultHelper.CleanupAnalyticalRun(ar, dbContext, dateTime, cancellationToken);

        ar.AddDomainEvent(new AnalyticalRunUpdatedEvent(ar));

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Analytical run complete: Instrument = {Instrument}, MethodName = '{MethodName}', AnalyticalRunId = {AnalyticalRunId}", request.InstrumentNumber, request.MethodName, request.AnalyticalRunId);

        await ResultHelper.UpdateApprovalStatusForAnalyticalRun(dbContext, ar.Id, cancellationToken);

        // Track changes for state-driven task card evaluation.
        // Reevaluate cards when curve state changed or approval/issues changed after run end.
        // TODO: This should be refactored to use the AnalyticalRunIssuesEvaluated event when analytical run has ResultReview issues
        await ResultHelper.UpdateTaskCardState(mediator, dbContext, ar.InstrumentNumber, new[] { TaskCardType.ResultsIssue }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        // run the result export in a background job to avoid blocking the gPRC call, as it could be a long-running operation.
        var job = JobBuilder.Create<ExportAnalyticalRunJob>()
            .WithIdentity(ExportAnalyticalRunJob.Key)
            .UsingJobData(new JobDataMap {
                { ExportAnalyticalRunJob.AnalyticalRunId, request.AnalyticalRunId },
                { ExportAnalyticalRunJob.AnalyticalRunDbId, ar.Id },
                { ExportAnalyticalRunJob.InstrumentNumber, ar.InstrumentNumber },
                { ExportAnalyticalRunJob.MethodName, request.MethodName }
            })
            .Build();
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.AddJob(job, replace: true, storeNonDurableWhileAwaitingScheduling: true, cancellationToken: cancellationToken);
        await scheduler.TriggerJob(ExportAnalyticalRunJob.Key, cancellationToken);
    }
}
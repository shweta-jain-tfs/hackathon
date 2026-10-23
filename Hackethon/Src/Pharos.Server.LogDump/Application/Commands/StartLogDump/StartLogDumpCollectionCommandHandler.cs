using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Application.LogDump;
using Pharos.Server.LogDump.Application.Jobs;
using Quartz;

namespace Pharos.Server.LogDump.Application.Commands.StartLogDump;

internal sealed class StartLogDumpCollectionCommandHandler(
    ISchedulerFactory schedulerFactory,
    ICurrentUserService currentUserService) : IRequestHandler<StartLogDumpCollectionCommand, bool>
{
    public async Task<bool> Handle(StartLogDumpCollectionCommand command, CancellationToken cancellationToken)
    {
        if (await schedulerFactory.IsLogDumpJobInProgress(cancellationToken))
        {
            return false;
        }

        try
        {
            var userId = currentUserService.UserId ?? Guid.Empty;
            var dateRange = GetLogCollectionDateRange(command.Request);

            await schedulerFactory.RegisterLogDumpJob(
                userId,
                command.Source,
                dateRange.FromDate,
                dateRange.ToDate,
                command.Request.PharosFiles,
                command.Request.InstrumentFiles,
                cancellationToken);

            await schedulerFactory.TriggerLogDumpJob(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static (DateOnly FromDate, DateOnly ToDate) GetLogCollectionDateRange(LogDumpCollectionRequest request)
    {
        if (request.DateMode is LogCollectionDateMode.SingleDate && request.SingleDate.HasValue)
        {
            return (request.SingleDate.Value.AddDays(-4), request.SingleDate.Value.AddDays(2));
        }
        else
        {
            return (request.FromDate!.Value, request.ToDate!.Value);
        }
    }
}

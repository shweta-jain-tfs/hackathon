using MediatR;
using Pharos.Server.LogDump.Application.Jobs;
using Quartz;

namespace Pharos.Server.LogDump.Application.Commands;

public sealed record CancelLogDumpCollectionCommand : IRequest;

internal sealed class CancelLogDumpCollectionCommandHandler(ISchedulerFactory schedulerFactory) : IRequestHandler<CancelLogDumpCollectionCommand>
{
    public async Task Handle(CancelLogDumpCollectionCommand request, CancellationToken cancellationToken)
    {
        await schedulerFactory.CancelLogDumpJob(cancellationToken);
    }
}

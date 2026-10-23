using MediatR;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries;

public record AnyTestResultsInPendingReflexQuery(int AnalyticalRunId) : IRequest<bool>;

internal class GetTestResultsPendingReflexQueryHandler(IResultDbContext context) : IRequestHandler<AnyTestResultsInPendingReflexQuery, bool>
{
    public async Task<bool> Handle(AnyTestResultsInPendingReflexQuery request, CancellationToken cancellationToken)
    {
        return await context.SampleTestResults
            .Where(r => r.AnalyticalRunId == request.AnalyticalRunId)
            .Where(x => x.Status == TestResultStatus.ApprovedPendingReflex)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
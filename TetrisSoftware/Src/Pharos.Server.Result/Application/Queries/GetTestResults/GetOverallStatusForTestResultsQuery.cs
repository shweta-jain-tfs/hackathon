using MediatR;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Enums;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public record GetOverallStatusForTestResultsQuery(int AnalyticalRunId) : IRequest<TestResultSummaryStatus>;

internal sealed class GetOverallStatusForTestResultsQueryHandler(IResultDbContext context) : IRequestHandler<GetOverallStatusForTestResultsQuery, TestResultSummaryStatus>
{
    public async Task<TestResultSummaryStatus> Handle(GetOverallStatusForTestResultsQuery request, CancellationToken cancellationToken)
    {
        var hasWarning = await context.SampleTestResults
            .Where(x => x.AnalyticalRunId == request.AnalyticalRunId && (x.Status == TestResultStatus.Aborted || x.Status == TestResultStatus.Flagged))
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (hasWarning)
        {
            return TestResultSummaryStatus.Warning;
        }

        return TestResultSummaryStatus.NoSymbol;
    }
}
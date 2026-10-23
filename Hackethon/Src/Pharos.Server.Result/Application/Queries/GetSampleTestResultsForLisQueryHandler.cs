using MediatR;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Result;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Mapping;

namespace Pharos.Server.Result.Application.Queries;

internal class GetSampleTestResultsForLisQueryHandler(
    IResultDbContext dbContext,
    ISender sender,
    LisTestResultMapper mapper) : IRequestHandler<GetSampleTestResultsForLisQuery, List<LisSampleTestResult>>
{
    public async Task<List<LisSampleTestResult>> Handle(GetSampleTestResultsForLisQuery request, CancellationToken cancellationToken)
    {
        var testResults = await dbContext.SampleTestResults
            .AsNoTracking()
            .Where(x => request.TestOrderIds.Contains(x.TestOrderId))
            .Include(x => x.Replicates!)
            .ThenInclude(r => r.TestReplicateReagents)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = await mapper.ToLisTestResults(testResults);

        // set the approver name
        var userIds = testResults
            .Where(x => x.LastModifiedBy != null && x.LastModifiedBy != Guid.Empty)
            .Select(x => x.LastModifiedBy!.Value)
            .ToHashSet();

        if (userIds.Count > 0)
        {
            var approvers = new Dictionary<string, string>();
            foreach (var userId in userIds)
            {
                var user = await sender.Send(new GetApplicationUserByIdQuery(userId), cancellationToken);
                approvers[userId.ToString()] = user?.UserName ?? string.Empty;
            }

            result.ForEach(x => x.ApprovedByUser = string.IsNullOrEmpty(x.ApprovedByUser) ? null : approvers[x.ApprovedByUser]);
        }

        // A requested test order without a result row is still exportable to LIS as pending.
        var foundTestResults = testResults.Select(x => x.TestOrderId).ToHashSet();
        result.AddRange(request.TestOrderIds
            .Where(testOrderId => !foundTestResults.Contains(testOrderId))
            .Select(CreatePendingLisTestResult));

        return result;
    }

    private static LisSampleTestResult CreatePendingLisTestResult(Guid testOrderId)
    {
        return new LisSampleTestResult
        {
            TestOrderId = testOrderId,
            Pending = true,
            Reagents = []
        };
    }
}
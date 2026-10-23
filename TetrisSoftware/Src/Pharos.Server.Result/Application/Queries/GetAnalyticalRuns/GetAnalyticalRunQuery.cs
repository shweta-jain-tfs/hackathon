using MediatR;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;

internal record GetAnalyticalRunQuery(int Id) : IRequest<AnalyticalRunDto>;

internal sealed class GetAnalyticalRunQueryHandler(IResultDbContext context, IAuthorizationService authorizationService) : IRequestHandler<GetAnalyticalRunQuery, AnalyticalRunDto>
{
    public async Task<AnalyticalRunDto> Handle(GetAnalyticalRunQuery request, CancellationToken cancellationToken)
    {
        var isRndMode = authorizationService.RndMode;

        var ar = await context.AnalyticalRuns
                     .Where(x => x.Id == request.Id)
                     .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException();

        var hasPendingReflex = await context.SampleTestResults
            .AnyAsync(r => r.AnalyticalRunId == ar.Id && r.Status == TestResultStatus.ApprovedPendingReflex, cancellationToken);

        var testsPendingApproval = await context.SampleTestResults
            .Where(r => r.AnalyticalRunId == ar.Id
                && (r.Status == TestResultStatus.PendingApproval || r.Status == TestResultStatus.ApprovedPendingReflex))
            .CountAsync(cancellationToken);

        var issues = AnalyticalRunStatusHelper.GetIssueStatuses(
            ar.CalibrationCurveIssue,
            ar.CurveControlIssue,
            ar.TestIssue,
            ar.QualityControlIssue,
            ar.RunStatus,
            ar.ApprovalStatus,
            testsPendingApproval,
            hasApprovedPendingReflex: hasPendingReflex);

        var analyticalRun = new AnalyticalRunDto
        {
            Id = ar.Id,
            MethodName = ar.MethodName,
            StartDateTime = ar.StartDateTime,
            EndDateTime = ar.EndDateTime,
            InstrumentName = ar.InstrumentNumber,
            IssueStatus = issues.Count > 0 ? issues[0] : IssueStatus.NoIssue,
            Issues = issues,
            StateStatus = AnalyticalRunStatusHelper.GetStateStatus(ar.RunStatus, ar.ApprovalStatus),
            CustomRunId = isRndMode ? (ar.CmrRunId ?? string.Empty) : string.Empty,
            NumberOfTests = await context.SampleTestResults.CountAsync(r => r.AnalyticalRunId == ar.Id, cancellationToken)
        };

        return analyticalRun;
    }
}

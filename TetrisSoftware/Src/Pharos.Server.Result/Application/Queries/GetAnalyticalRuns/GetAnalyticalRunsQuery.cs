using System.Linq.Expressions;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;

public class GetAnalyticalRunsQuery : SortedPartialQuery<AnalyticalRunDto>
{
    public List<StateStatus>? Status { get; set; }
    public List<string>? InstrumentNumber { get; set; }
    public DateTimeOffset? FromStartDate { get; set; }
    public DateTimeOffset? ToStartDate { get; set; }
    public List<IssueStatus>? Issues { get; set; }

    public bool BypassRowLimit { get; set; }
}

internal sealed class GetAnalyticalRunsQueryHandler(IResultDbContext context, IInstrumentManager instrumentManager, IAuthorizationService authorizationService, IMethodsService methodsService) : PartialQueryHandlerBase<GetAnalyticalRunsQuery, AnalyticalRun, AnalyticalRunDto>, IRequestHandler<GetAnalyticalRunsQuery, PartialQueryResult<AnalyticalRunDto>>
{
    // If you change this expression, please make sure it stays in sync with AnalyticalRunStatusHelper.GetIssueStatus and the filter below
    private static readonly Expression<Func<AnalyticalRun, IssueStatus>> _issueStatusKeySelector = x =>
        x.CalibrationCurveIssue == AnalyticalRunIssue.Error ? IssueStatus.CalibrationFailed :
        x.CurveControlIssue == AnalyticalRunIssue.Error ? IssueStatus.CurveControlFailed :
        x.CalibrationCurveIssue == AnalyticalRunIssue.Warning ? IssueStatus.CalibrationFlagged :
        x.CurveControlIssue == AnalyticalRunIssue.Warning ? IssueStatus.CurveControlFlagged :
        x.QualityControlIssue == AnalyticalRunIssue.Error ? IssueStatus.QualityControlFailed :
        x.QualityControlIssue == AnalyticalRunIssue.Warning ? IssueStatus.QualityControlFlagged :
        x.TestIssue == AnalyticalRunIssue.Warning ? IssueStatus.FailedTests :
        x.RunStatus == RunStatus.Ready
            && x.ApprovalStatus == ResultApprovalStatus.NotApproved
            && x.TestResults.OfType<SampleTestResult>().Count(r => r.Status == TestResultStatus.PendingApproval || r.Status == TestResultStatus.ApprovedPendingReflex) > 0
            ? IssueStatus.PendingApproval :
        x.TestResults.OfType<SampleTestResult>().Any(r => r.Status == TestResultStatus.ApprovedPendingReflex) ? IssueStatus.ApprovedPendingReflex :
        IssueStatus.NoIssue;

    // If you change this expression, please make sure it stays in sync with AnalyticalRunStatusHelper.GetStateStatus.
    private static readonly Expression<Func<AnalyticalRun, StateStatus>> _stateStatusKeySelector = x =>
        x.ApprovalStatus == ResultApprovalStatus.Rejected ? StateStatus.Rejected :
        x.ApprovalStatus == ResultApprovalStatus.Approved ? StateStatus.Approved :
        x.RunStatus == RunStatus.Processing ? StateStatus.Processing :
        StateStatus.Ready;

    private Dictionary<string, int?> _methodsSortOrder = new();

    public async Task<PartialQueryResult<AnalyticalRunDto>> Handle(GetAnalyticalRunsQuery request, CancellationToken cancellationToken)
    {
        if (request.SortByProperty == nameof(AnalyticalRunDto.MethodName))
        {
            _methodsSortOrder = await methodsService.GetMethodsSortOrder();
        }

        var query = context.AnalyticalRuns.AsQueryable();
        var result = await GetPartialQueryResultAsync(query, request, cancellationToken);
        foreach (var item in result.Items)
        {
            item.InstrumentName = instrumentManager.FindInstrument(item.InstrumentName)?.DisplayName ?? item.InstrumentName;
        }
        return result;
    }

    private static Expression<Func<AnalyticalRun, int>> BuildMethodSortExpression(Dictionary<string, int?> methodsSortOrder)
    {
        var param = Expression.Parameter(typeof(AnalyticalRun), "x");
        var methodNameProp = Expression.Property(param, nameof(AnalyticalRun.MethodName));

        Expression body = Expression.Constant(int.MaxValue);

        foreach (var kvp in methodsSortOrder.Where(k => k.Value.HasValue).OrderByDescending(k => k.Value!.Value))
        {
            var condition = Expression.Equal(methodNameProp, Expression.Constant(kvp.Key));
            body = Expression.Condition(condition, Expression.Constant(kvp.Value!.Value), body);
        }

        return Expression.Lambda<Func<AnalyticalRun, int>>(body, param);
    }

    protected override IQueryable<AnalyticalRun> AddFiltering(IQueryable<AnalyticalRun> query, GetAnalyticalRunsQuery request)
    {
        if (request.Status is { Count: > 0 })
        {
            // Reverse-map each requested StateStatus to its underlying (RunStatus, ApprovalStatus) conditions
            // following the same logic as AnalyticalRunStatusHelper.GetStateStatus.
            var requiredApprovalStatuses = new List<ResultApprovalStatus>();
            var requiredRunStatuses = new List<RunStatus>();

            foreach (var status in request.Status)
            {
                switch (status)
                {
                    case StateStatus.Approved:
                        requiredApprovalStatuses.Add(ResultApprovalStatus.Approved);
                        break;
                    case StateStatus.Rejected:
                        requiredApprovalStatuses.Add(ResultApprovalStatus.Rejected);
                        break;
                    case StateStatus.Processing:
                        requiredRunStatuses.Add(RunStatus.Processing);
                        break;
                    case StateStatus.Ready:
                        requiredRunStatuses.Add(RunStatus.Ready);
                        requiredRunStatuses.Add(RunStatus.Aborted);
                        requiredRunStatuses.Add(RunStatus.ReadyWithoutTests);
                        break;
                }
            }

            if (requiredApprovalStatuses.Count > 0 && requiredRunStatuses.Count > 0)
            {
                query = query.Where(x => requiredApprovalStatuses.Contains(x.ApprovalStatus) ||
                                         (x.ApprovalStatus != ResultApprovalStatus.Rejected && x.ApprovalStatus != ResultApprovalStatus.Approved && requiredRunStatuses.Contains(x.RunStatus)));
            }
            else if (requiredApprovalStatuses.Count > 0)
            {
                query = query.Where(x => requiredApprovalStatuses.Contains(x.ApprovalStatus));
            }
            else
            {
                query = query.Where(x => x.ApprovalStatus != ResultApprovalStatus.Rejected && x.ApprovalStatus != ResultApprovalStatus.Approved && requiredRunStatuses.Contains(x.RunStatus));
            }
        }

        if (request.Issues is { Count: > 0 })
        {
            // if changing this logic, please keep it in sync with _issueStatusKeySelector
            query = query.Where(x => request.Issues.Contains(
                x.CalibrationCurveIssue == AnalyticalRunIssue.Error ? IssueStatus.CalibrationFailed :
                x.CurveControlIssue == AnalyticalRunIssue.Error ? IssueStatus.CurveControlFailed :
                x.CalibrationCurveIssue == AnalyticalRunIssue.Warning ? IssueStatus.CalibrationFlagged :
                x.CurveControlIssue == AnalyticalRunIssue.Warning ? IssueStatus.CurveControlFlagged :
                x.QualityControlIssue == AnalyticalRunIssue.Error ? IssueStatus.QualityControlFailed :
                x.QualityControlIssue == AnalyticalRunIssue.Warning ? IssueStatus.QualityControlFlagged :
                x.TestIssue == AnalyticalRunIssue.Warning ? IssueStatus.FailedTests :
                x.RunStatus == RunStatus.Ready
                    && x.ApprovalStatus == ResultApprovalStatus.NotApproved
                    && x.TestResults.OfType<SampleTestResult>().Count(r => r.Status == TestResultStatus.PendingApproval || r.Status == TestResultStatus.ApprovedPendingReflex) > 0
                    ? IssueStatus.PendingApproval :
                x.TestResults.OfType<SampleTestResult>().Any(r => r.Status == TestResultStatus.ApprovedPendingReflex) ? IssueStatus.ApprovedPendingReflex :
                IssueStatus.NoIssue));
        }

        if (request.InstrumentNumber is { Count: > 0 })
        {
            query = query.Where(x => request.InstrumentNumber.Contains(x.InstrumentNumber));
        }

        if (request.FromStartDate.HasValue)
        {
            query = query.Where(x => x.StartDateTime.Date >= request.FromStartDate.Value.Date);
        }

        if (request.ToStartDate.HasValue)
        {
            query = query.Where(x => x.StartDateTime.Date < request.ToStartDate.Value.Date.AddDays(1));
        }

        return query;
    }

    protected override IQueryable<AnalyticalRun> AddSorting(IQueryable<AnalyticalRun> query, string? sortProperty, bool ascending)
    {
        query = sortProperty switch
        {
            nameof(AnalyticalRunDto.MethodName) => ascending ? query.OrderBy(BuildMethodSortExpression(_methodsSortOrder)).ThenBy(x => x.MethodName).ThenBy(x => x.StartDateTime) : query.OrderByDescending(BuildMethodSortExpression(_methodsSortOrder)).ThenByDescending(x => x.MethodName).ThenByDescending(x => x.StartDateTime),
            nameof(AnalyticalRunDto.StartDateTime) => ascending ? query.OrderBy(x => x.StartDateTime) : query.OrderByDescending(x => x.StartDateTime),
            nameof(AnalyticalRunDto.EndDateTime) => ascending ? query.OrderBy(x => x.EndDateTime) : query.OrderByDescending(x => x.EndDateTime),
            nameof(AnalyticalRunDto.IssueStatus) or nameof(AnalyticalRunDto.Issues) => ascending ? query.OrderBy(_issueStatusKeySelector) : query.OrderByDescending(_issueStatusKeySelector),
            nameof(AnalyticalRunDto.StateStatus) => ascending ? query.OrderBy(_stateStatusKeySelector) : query.OrderByDescending(_stateStatusKeySelector),
            nameof(AnalyticalRunDto.CustomRunId) => ascending ? query.OrderBy(x => x.CmrRunId) : query.OrderByDescending(x => x.CmrRunId),
            _ => query.OrderByDescending(x => x.StartDateTime)
        };
        return query;
    }

    protected override IQueryable<AnalyticalRunDto> ApplyProjection(IQueryable<AnalyticalRun> query)
    {
        return query.Select(x => new AnalyticalRunDto
        {
            Id = x.Id,
            MethodName = x.MethodName,
            StartDateTime = x.StartDateTime,
            EndDateTime = x.EndDateTime,
            InstrumentName = x.InstrumentNumber,
            IssueStatus = AnalyticalRunStatusHelper.GetIssueStatus(
                x.CalibrationCurveIssue,
                x.CurveControlIssue,
                x.TestIssue,
                x.QualityControlIssue,
                x.RunStatus,
                x.ApprovalStatus,
                x.TestResults.OfType<SampleTestResult>().Count(r => r.Status == TestResultStatus.PendingApproval || r.Status == TestResultStatus.ApprovedPendingReflex),
                hasApprovedPendingReflex: x.TestResults.OfType<SampleTestResult>().Any(r => r.Status == TestResultStatus.ApprovedPendingReflex)),
            Issues = AnalyticalRunStatusHelper.GetIssueStatuses(
                x.CalibrationCurveIssue,
                x.CurveControlIssue,
                x.TestIssue,
                x.QualityControlIssue,
                x.RunStatus,
                x.ApprovalStatus,
                x.TestResults.OfType<SampleTestResult>().Count(r => r.Status == TestResultStatus.PendingApproval || r.Status == TestResultStatus.ApprovedPendingReflex),
                hasApprovedPendingReflex: x.TestResults.OfType<SampleTestResult>().Any(r => r.Status == TestResultStatus.ApprovedPendingReflex)),
            StateStatus = AnalyticalRunStatusHelper.GetStateStatus(x.RunStatus, x.ApprovalStatus),
            CustomRunId = x.CmrRunId ?? string.Empty,
            NumberOfTests = x.TestResults.OfType<SampleTestResult>().Count()
        });
    }

    protected override Task AttachExtraInformation(List<AnalyticalRunDto> items, CancellationToken cancellationToken)
    {
        if (!authorizationService.RndMode)
        {
            items.ForEach(x => x.CustomRunId = string.Empty);
        }

        return Task.CompletedTask;
    }
}
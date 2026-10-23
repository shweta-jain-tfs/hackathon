using MediatR;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services;

internal sealed class ResultHelper
{
    /// <summary>
    /// Applies consistent end-of-run cleanup to an analytical run:
    /// sets EndDateTime (if not already set), RunStatus, and invalidates
    /// stale CurveState/CurveSource fields.
    /// </summary>
    public static async Task CleanupAnalyticalRun(AnalyticalRun analyticalRun, IResultDbContext dbContext, IDateTime dateTime, CancellationToken cancellationToken)
    {
        analyticalRun.EndDateTime ??= dateTime.Now;

        var hasTests = await dbContext.SampleTestResults
            .AnyAsync(x => x.AnalyticalRunId == analyticalRun.Id, cancellationToken)
            .ConfigureAwait(false);

        analyticalRun.RunStatus = hasTests ? RunStatus.Ready : RunStatus.ReadyWithoutTests;

        if (analyticalRun.CurveState is CurveState.WaitingForNewCurve or CurveState.WaitingForCurveControl)
        {
            analyticalRun.CurveState = CurveState.Invalid;

            if (analyticalRun.CurveSource == CurveSource.CurveFromPreviousAssay)
            {
                analyticalRun.CurveId = null;
                analyticalRun.CurveSource = CurveSource.Missing;
            }
        }
    }

    public static async Task UpdateApprovalStatusForAnalyticalRun(IResultDbContext dbContext, int analyticalRunId, CancellationToken cancellationToken)
    {
        var analyticalRun = await dbContext.AnalyticalRuns.FirstAsync(ar => ar.Id == analyticalRunId, cancellationToken);
        if (analyticalRun.RunStatus == RunStatus.Processing)
        {
            return;
        }

        var oldApprovalStatus = analyticalRun.ApprovalStatus;
        analyticalRun.ApprovalStatus = ResultApprovalStatus.NotApproved;

        var numberOfResultsInRun = await dbContext.SampleTestResults.Where(r => r.AnalyticalRunId == analyticalRunId).CountAsync(cancellationToken);
        if (numberOfResultsInRun > 0)
        {
            var numberOfApprovedResults = await dbContext.SampleTestResults
                .Where(r => r.AnalyticalRunId == analyticalRunId && r.ApprovalStatus == ResultApprovalStatus.Approved)
                .CountAsync(cancellationToken);
            var numberOfRejectedResults = await dbContext.SampleTestResults
                .Where(r => r.AnalyticalRunId == analyticalRunId && r.ApprovalStatus == ResultApprovalStatus.Rejected)
                .CountAsync(cancellationToken);

            if (numberOfApprovedResults == numberOfResultsInRun ||
                (numberOfApprovedResults > 0 && numberOfApprovedResults + numberOfRejectedResults == numberOfResultsInRun))
            {
                analyticalRun.ApprovalStatus = ResultApprovalStatus.Approved;
            }
            else if (numberOfRejectedResults == numberOfResultsInRun)
            {
                analyticalRun.ApprovalStatus = ResultApprovalStatus.Rejected;
            }
        }

        if (analyticalRun.ApprovalStatus != oldApprovalStatus)
        {
            if (analyticalRun.ApprovalStatus is ResultApprovalStatus.Approved or ResultApprovalStatus.Rejected)
            {
                analyticalRun.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, CurveIssue: AnalyticalRunIssue.NoIssue, CurveControlIssue: AnalyticalRunIssue.NoIssue, QualityControlIssue: AnalyticalRunIssue.NoIssue, TestIssue: AnalyticalRunIssue.NoIssue));
            }

            analyticalRun.AddDomainEvent(new AnalyticalRunUpdatedEvent(analyticalRun));
            analyticalRun.AddDomainEvent(new AnalyticalRunApprovalStatusChangedEvent(analyticalRun.Id, analyticalRun.ApprovalStatus));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public static async Task<bool> AnyResultIssuesInOtherAnalyticalRuns(IResultDbContext dbContext, int analyticalRunId, string instrumentNumber, CancellationToken cancellationToken)
    {
        return await dbContext.AnalyticalRuns.AnyAsync(ar =>
            ar.Id != analyticalRunId && ar.InstrumentNumber == instrumentNumber &&
            (ar.CalibrationCurveIssue != AnalyticalRunIssue.NoIssue || ar.CurveControlIssue != AnalyticalRunIssue.NoIssue), cancellationToken);
    }

    public static async Task UpdateTaskCardState(
        IMediator mediator,
        IResultDbContext dbContext,
        string serialNumber,
        IReadOnlyCollection<TaskCardType> cardTypes,
        CancellationToken cancellationToken)
    {
        var shouldUpdateReviewPendingTaskCard = cardTypes.Contains(TaskCardType.ResultsIssue);

        var includeCalibration = cardTypes.Contains(TaskCardType.CalibrationIssue);
        var includeQc = cardTypes.Contains(TaskCardType.QCIssue);
        var includeResults = cardTypes.Contains(TaskCardType.ResultsIssue);

        var runs = await GetCurrentAnalyticalRuns(dbContext, serialNumber, cancellationToken);

        var topCalibrationIssue = AnalyticalRunIssue.NoIssue;
        var topQcIssue = AnalyticalRunIssue.NoIssue;
        var topResultsIssue = AnalyticalRunIssue.NoIssue;

        foreach (var run in runs)
        {
            if (run.CurveState != CurveState.Frozen)
            {
                if (includeCalibration)
                {
                    topCalibrationIssue = GetHighestIssue(topCalibrationIssue, run.CalibrationCurveIssue);
                    topCalibrationIssue = GetHighestIssue(topCalibrationIssue, run.CurveControlIssue);
                }

                if (includeQc)
                {
                    topQcIssue = GetHighestIssue(topQcIssue, run.QualityControlIssue);
                }
            }

            if (includeResults)
            {
                topResultsIssue = GetHighestIssue(topResultsIssue, run.TestIssue);
            }

            if ((!includeCalibration || topCalibrationIssue == AnalyticalRunIssue.Error)
                && (!includeQc || topQcIssue == AnalyticalRunIssue.Error)
                && (!includeResults || topResultsIssue == AnalyticalRunIssue.Error))
            {
                break;
            }
        }

        var calibrationState = GetTaskCardState(topCalibrationIssue);
        var qcState = GetTaskCardState(topQcIssue);
        var resultsState = GetTaskCardState(topResultsIssue);

        foreach (var cardType in cardTypes)
        {
            var state = cardType switch
            {
                TaskCardType.CalibrationIssue => calibrationState,
                TaskCardType.QCIssue => qcState,
                TaskCardType.ResultsIssue => resultsState,
                _ => null
            };

            if (state.HasValue)
            {
                await mediator.Send(new TaskCardCommand(cardType, state.Value, serialNumber), cancellationToken);
            }
            else
            {
                await mediator.Send(new RemoveTaskCardCommand(cardType, serialNumber), cancellationToken);
            }
        }

        if (!shouldUpdateReviewPendingTaskCard) { return; }

        //Update review-pending task card
        if (await ShouldShowResultReviewPendingTaskCard(dbContext, serialNumber, cancellationToken))
        {
            await mediator.Send(new TaskCardCommand(TaskCardType.ResultReviewPending, TaskCardState.Information, serialNumber), cancellationToken);
            return;
        }
        else
        {

            await mediator.Send(new RemoveTaskCardCommand(TaskCardType.ResultReviewPending, serialNumber), cancellationToken);
        }
    }

    /// <summary>
    /// Evaluates if a review reminder card should be shown for the instrument.
    /// The card is shown when any test result in a ready analytical run requires review action.
    /// </summary>
    private static async Task<bool> ShouldShowResultReviewPendingTaskCard(IResultDbContext dbContext, string instrumentNumber, CancellationToken cancellationToken)
    {
        return await dbContext.SampleTestResults.AnyAsync(result =>
            result.AnalyticalRun != null
            && result.AnalyticalRun.InstrumentNumber == instrumentNumber
            && result.AnalyticalRun.RunStatus == RunStatus.Ready
            && (result.Status == TestResultStatus.PendingApproval
                || result.Status == TestResultStatus.ApprovedPendingReflex), cancellationToken);
    }

    private static async Task<List<AnalyticalRun>> GetCurrentAnalyticalRuns(IResultDbContext dbContext, string instrumentNumber, CancellationToken cancellationToken)
    {
        // Get all non-approved/non-rejected analytical runs for the instrument.
        return await dbContext.AnalyticalRuns
            .Where(ar =>
                ar.InstrumentNumber == instrumentNumber &&
                ar.ApprovalStatus != ResultApprovalStatus.Approved &&
                ar.ApprovalStatus != ResultApprovalStatus.Rejected)
            .ToListAsync(cancellationToken);
    }

    private static AnalyticalRunIssue GetHighestIssue(AnalyticalRunIssue currentTopIssue, AnalyticalRunIssue candidateIssue)
    {
        if (currentTopIssue == AnalyticalRunIssue.Error || candidateIssue == AnalyticalRunIssue.NoIssue)
        {
            return currentTopIssue;
        }

        if (candidateIssue == AnalyticalRunIssue.Error)
        {
            return AnalyticalRunIssue.Error;
        }

        return AnalyticalRunIssue.Warning;
    }

    private static TaskCardState? GetTaskCardState(AnalyticalRunIssue topIssue)
    {
        if (topIssue == AnalyticalRunIssue.Error)
        {
            return TaskCardState.Error;
        }

        if (topIssue == AnalyticalRunIssue.Warning)
        {
            return TaskCardState.Warning;
        }

        return null;
    }
}

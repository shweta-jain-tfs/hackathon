using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events.TestResultReady;

internal record CurveControlResultGroupReadyEvent(string InstrumentNumber, Guid GroupId, string MethodName) : INotification;

internal sealed class CurveControlResultGroupReadyEventHandler(
    IResultDbContext dbContext,
    IMethodsService methodsService,
    IResultCalculationService resultCalculationService,
    IMediator mediator,
    IDateTime dateTime,
    ILogger<CurveControlResultGroupReadyEventHandler> logger) : TestResultReadyEventHandlerBase<CurveControlTestResult>(dbContext), INotificationHandler<CurveControlResultGroupReadyEvent>
{
    public async Task Handle(CurveControlResultGroupReadyEvent notification, CancellationToken cancellationToken)
    {
        var testResults = await GetListOfCurveControlsInGroup(notification.InstrumentNumber, notification.GroupId, cancellationToken);
        var analyticalRunId = testResults.FirstOrDefault()?.AnalyticalRunId;
        var (conjugateLotNumber, immunoCapLotNumber) = await GetLotNumbers(testResults, cancellationToken);

        var runInfo = analyticalRunId.HasValue
            ? await DbContext.AnalyticalRuns
                .Where(ar => ar.Id == analyticalRunId.Value)
                .Select(ar => new { ar.CurveId, ar.CurveState, ar.CurveSource })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var calculationCurveId = runInfo?.CurveId;
        var isCommitted = runInfo?.CurveState == CurveState.Frozen;
        var suppressCurveControlChartUpdates = runInfo?.CurveSource == CurveSource.CurveFromCurrentAssay;

        // Concentrations can only be calculated when:
        // 1. A curve has been assigned to the run (CurveId not null)
        // 2. The curve source is not Missing (defensive check)
        // 3. The run is not waiting for a brand-new curve and is not committed
        // This allows CC evaluation for previous-assay/invalid runs (which can recover on OK CC)
        // while blocking instrument-driven recalculation for frozen runs.
        var canCalculateConcentrations = runInfo != null
            && runInfo.CurveId.HasValue
            && runInfo.CurveSource is not CurveSource.Missing
            && runInfo.CurveState is not (CurveState.WaitingForNewCurve or CurveState.Frozen);

        if (canCalculateConcentrations)
        {
            await HandleCurveControlTestOrders(testResults, notification.MethodName, calculationCurveId, isCommitted, suppressCurveControlChartUpdates: suppressCurveControlChartUpdates, cancellationToken);
        }

        await EvaluateCurveControls(testResults, notification.MethodName, notification.GroupId, notification.InstrumentNumber, analyticalRunId, conjugateLotNumber, immunoCapLotNumber, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        foreach (var testResult in testResults)
        {
            await mediator.Send(SendResultToLabCommunityCommandFactory.Create(testResult), cancellationToken);
        }
    }

    private async Task HandleCurveControlTestOrders(List<CurveControlTestResult> testResults, string methodName, int? calculationCurveId, bool isCommitted, bool suppressCurveControlChartUpdates, CancellationToken cancellationToken)
    {
        foreach (var testResult in testResults)
        {
            // Calculate individual replicate concentrations from responses using the assigned curve
            // before judgment evaluation — concentrations are not provided by the instrument for CC replicates.
            if (calculationCurveId.HasValue && testResult.Replicates != null)
            {
                foreach (var replicate in testResult.Replicates)
                {
                    await resultCalculationService.CalculateReplicateConcentration(
                        replicate,
                        calculationCurveId.Value,
                        testResult.PreDilutionFactor,
                        testResult.InstrumentDilutionFactor,
                        methodName,
                        testResult.TestName,
                        TestResultType.CurveControl,
                        cancellationToken);
                }
            }

            var ccLimits = await methodsService.GetMethod(methodName,
                m => m.CurveControls.Where(cc => cc.CurveControlName == testResult.CurveControlPointName).Select(cc => new
                {
                    InnerSDLimit = cc.InnerStdDevLimit,
                    OuterSDLimit = cc.OuterStdDevLimit,
                    ExpectedSD = cc.ExpectedStdDev,
                    DefinedConcentration = cc.ExpectedConcentration,
                }).FirstOrDefault());

            if (testResult.Replicates != null && ccLimits != null)
            {
                var upperOuter = ccLimits.DefinedConcentration + (ccLimits.ExpectedSD * ccLimits.OuterSDLimit);
                var upperInner = ccLimits.DefinedConcentration + (ccLimits.ExpectedSD * ccLimits.InnerSDLimit);
                var lowerOuter = ccLimits.DefinedConcentration - (ccLimits.ExpectedSD * ccLimits.OuterSDLimit);
                var lowerInner = ccLimits.DefinedConcentration - (ccLimits.ExpectedSD * ccLimits.InnerSDLimit);

                var judgmentSummary = HandleCurveControlReplicates(testResult.Replicates, upperOuter, upperInner, lowerOuter, lowerInner);
                testResult.JudgmentSummary = judgmentSummary;

                await CalculateCurveControlMeanResultValues(testResult);

                await SetTestResultStatus(testResult, cancellationToken);
                // Do not update the CC chart for committed runs — post-commitment CCs
                // should not replace the pre-commitment anchor in the chart.
                if (!isCommitted && !suppressCurveControlChartUpdates)
                {
                    testResult.AddDomainEvent(new CurveControlChartUpdatedEvent(testResult.InstrumentNumber, testResult.MethodName));
                }
            }
        }
    }

    /// <summary>
    /// Handle the replicates of a curve control test order
    /// </summary>
    /// <param name="testReplicates">List of replicates</param>
    /// <param name="upperOuter">Upper outer limit</param>
    /// <param name="upperInner">Upper inner limit</param>
    /// <param name="lowerOuter">Lower outer limit</param>
    /// <param name="lowerInner">Lower inner limit</param>
    /// <returns>Curve control judgment summary</returns>
    private static CurveControlJudgment HandleCurveControlReplicates(
        IList<TestReplicate> testReplicates,
        double upperOuter,
        double upperInner,
        double lowerOuter,
        double lowerInner)
    {
        var judgmentSummary = CurveControlJudgment.NotDefined;
        var replicateConcentrationExists = false;

        foreach (var testReplicate in testReplicates)
        {
            if (testReplicate is CurveControlTestReplicate curveControlTestReplicate)
            {
                var judgmentReplicate = CurveControlJudgment.OK;
                if (judgmentSummary == CurveControlJudgment.NotDefined)
                {
                    judgmentSummary = CurveControlJudgment.OK;
                }

                if (curveControlTestReplicate.Concentration != null && curveControlTestReplicate.ConcentrationStatus != ConcentrationStatus.NotAvailable)
                {
                    replicateConcentrationExists = true;

                    CheckAgainstUpperLimits(upperOuter, upperInner, ref judgmentSummary, curveControlTestReplicate, ref judgmentReplicate);
                    CheckAgainstLowerLimits(lowerOuter, lowerInner, ref judgmentSummary, curveControlTestReplicate, ref judgmentReplicate);
                }
                else if (curveControlTestReplicate.IsLowResponse)
                {
                    judgmentReplicate = CurveControlJudgment.Low;
                    judgmentSummary = CurveControlJudgment.Low;
                }
                else
                {
                    judgmentReplicate = CurveControlJudgment.NotDefined;
                }
                curveControlTestReplicate.Judgment = judgmentReplicate;
            }
        }

        if (!replicateConcentrationExists)
        {
            judgmentSummary = CurveControlJudgment.NotDefined;
        }

        return judgmentSummary;
    }

    /// <summary>
    /// Check upper limits for a curve control replicate
    /// </summary>
    /// <param name="upperOuter">Upper outer limit</param>
    /// <param name="upperInner">Upper inner limit</param>
    /// <param name="judgmentSummary">Judgment summary so far</param>
    /// <param name="curveControlTestReplicate">The curve control replicate</param>
    /// <param name="judgmentReplicate">Replicate judgment so far</param>
    private static void CheckAgainstUpperLimits(double upperOuter, double upperInner, ref CurveControlJudgment judgmentSummary, CurveControlTestReplicate curveControlTestReplicate, ref CurveControlJudgment judgmentReplicate)
    {
        // Check against upper outer limit
        if (curveControlTestReplicate.Concentration > upperOuter)
        {
            judgmentReplicate = CurveControlJudgment.High;
            judgmentSummary = CurveControlJudgment.High;
        }

        // Check against upper inner limit
        else if (curveControlTestReplicate.Concentration > upperInner)
        {
            judgmentReplicate = CurveControlJudgment.FlaggedHigh;
            if (judgmentSummary == CurveControlJudgment.OK)
            {
                judgmentSummary = CurveControlJudgment.FlaggedHigh;
            }
            else
            {
                judgmentSummary = CurveControlJudgment.High;  // A bit strange, but this is how Prime does...
            }
        }
    }

    /// <summary>
    /// Check lower limits for a curve control replicate
    /// </summary>
    /// <param name="lowerOuter">Lower outer limit</param>
    /// <param name="lowerInner">Lower inner limit</param>
    /// <param name="judgmentSummary">Judgment summary so far</param>
    /// <param name="curveControlTestReplicate">The curve control replicate</param>
    /// <param name="judgmentReplicate">Replicate judgment so far</param>
    private static void CheckAgainstLowerLimits(double lowerOuter, double lowerInner, ref CurveControlJudgment judgmentSummary, CurveControlTestReplicate curveControlTestReplicate, ref CurveControlJudgment judgmentReplicate)
    {
        // Check against lower outer limit
        if (curveControlTestReplicate.Concentration < lowerOuter)
        {
            judgmentReplicate = CurveControlJudgment.Low;
            judgmentSummary = CurveControlJudgment.Low;
        }

        // Check against lower inner limit
        else if (curveControlTestReplicate.Concentration < lowerInner)
        {
            judgmentReplicate = CurveControlJudgment.FlaggedLow;
            if (judgmentSummary == CurveControlJudgment.OK)
            {
                judgmentSummary = CurveControlJudgment.FlaggedLow;
            }
            else
            {
                judgmentSummary = CurveControlJudgment.Low;  // A bit strange, but this is how Prime does...
            }
        }
    }

    /// <summary>
    /// Calculate mean concentration and response values for a curve control
    /// </summary>
    private async Task CalculateCurveControlMeanResultValues(CurveControlTestResult testResult)
    {
        if (testResult.Replicates != null)
        {
            await resultCalculationService.CalculateSummaryResult(testResult);
        }
    }

    private async Task EvaluateCurveControls(List<CurveControlTestResult> curveControlTestOrderList,
        string methodName,
        Guid groupId,
        string instrumentNumber,
        int? analyticalRunId,
        string conjugateLotNumber,
        string immunoCapLotNumber,
        CancellationToken cancellationToken)
    {
        if (!analyticalRunId.HasValue)
        {
            return;
        }

        var analyticalRun = await DbContext.AnalyticalRuns
            .Include(ar => ar.CalculationCurve)
            .Where(ar => ar.Id == analyticalRunId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (analyticalRun == null)
        {
            return;
        }

        // If a new curve has been ordered, any CC arriving against the stale curve is irrelevant.
        // Ignore CC events until the new curve arrives and replaces the stale CurveId.
        if (analyticalRun.CurveState == CurveState.WaitingForNewCurve)
        {
            return;
        }

        var calculationCurveId = analyticalRun.CurveId;
        var runId = analyticalRun.InstrumentRunId;

        // If no curve is assigned yet, save the CC result for archival purposes only.
        // Do not evaluate judgments or affect any curves/states.
        if (!calculationCurveId.HasValue)
        {
            return;
        }

        // Skip re-evaluation of stale CCs after a curve rerun: if run transitioned to NotCalculated,
        // any pre-existing CC result was evaluated before the rerun and should not affect the new curve.
        if (analyticalRun.CurveState == CurveState.Valid)
        {
            var curveCreatedAt = analyticalRun.CalculationCurve?.MeasurementDateTime;
            if (curveCreatedAt.HasValue)
            {
                // Remove stale CCs (those created before the curve) — only evaluate fresh CCs
                curveControlTestOrderList = curveControlTestOrderList
                    .Where(cc => cc.Created >= curveCreatedAt.Value)
                    .ToList();
            }
        }

        var judgmentResult = await mediator.Send(
            new EvaluateCurveControlsCommand(curveControlTestOrderList, methodName, groupId, runId, instrumentNumber, calculationCurveId, conjugateLotNumber, immunoCapLotNumber),
            cancellationToken);

        // Always clean up CurrentCurves for flagged/NotOK CCs, even for committed runs.
        // The committed run's calculation state stays locked, but the curve should not
        // propagate to the next assay if this CC shows it's no good.
        if (judgmentResult is CurveControlJudgmentResult.Flagged or CurveControlJudgmentResult.NotOK or CurveControlJudgmentResult.ReRunNotOK)
        {
            // Flagged or failed CCs stop propagation of the current curve to the next assay.
            analyticalRun.AddDomainEvent(new RemoveCurrentCurveEvent(calculationCurveId!.Value));
        }

        // If the run is already committed, it stays committed — ignore new CC events.
        // State updates and recalculation only for non-committed runs.
        // Never update analytical run states or issues for curves from current assay, as they are not subject to CC judgment.
        if (analyticalRun.CurveState == CurveState.Frozen || analyticalRun.CurveSource == CurveSource.CurveFromCurrentAssay)
        {
            return;
        }

        analyticalRun.CurveControlTotalJudgment = judgmentResult;
        await SaveQualityIssueAndHandleTaskCards(analyticalRun, judgmentResult);


        if (judgmentResult == CurveControlJudgmentResult.OK)
        {
            // OK CC received — unlock calculations for runs waiting for a CC,
            // or for runs that went Invalid due to a prior flagged CC (curve is still assigned).
            if (analyticalRun.CurveState is CurveState.WaitingForCurveControl or CurveState.Invalid
                && analyticalRun.CurveSource != CurveSource.Missing)
            {
                analyticalRun.CurveState = CurveState.Valid;
                analyticalRun.AddDomainEvent(new AnalyticalRunCalculationCurveUpdatedByCurveControl(analyticalRun.Id));
            }
        }
        else if (judgmentResult == CurveControlJudgmentResult.Flagged)
        {
            // Flagged CC received — the run is no longer waiting for a CC; the user must
            // inspect the result and decide to accept the flagged curve or rerun.
            analyticalRun.CurveState = CurveState.Invalid;

            if (analyticalRun.CalculationCurve != null)
            {
                SetCurveLastDayToUseToPreviousDay(analyticalRun.CalculationCurve);
            }
        }
        else if (judgmentResult is CurveControlJudgmentResult.NotOK or CurveControlJudgmentResult.ReRunNotOK)
        {
            // Two failed CCs → block calculations, require a new curve or manual selection.
            analyticalRun.CurveState = CurveState.Invalid;
            analyticalRun.CurveSource = CurveSource.Missing;

            if (analyticalRun.CalculationCurve != null)
            {
                SetCurveLastDayToUseToPreviousDay(analyticalRun.CalculationCurve);
            }
        }

        analyticalRun.AddDomainEvent(new CalibrationCurveChartUpdatedEvent(analyticalRun.InstrumentNumber, methodName));
        analyticalRun.AddDomainEvent(new CurveControlChartUpdatedEvent(analyticalRun.InstrumentNumber, methodName));
    }

    private void SetCurveLastDayToUseToPreviousDay(Curve curve)
    {
        logger.LogInformation("Changing curve last day to use due to flagged or faild CC CurveId: {CurveId}, CurrentLastDayToUse: {CurrentLastDayToUse}, NewLastDayToUse: {NewLastDayToUse}",
            curve.Id, curve.LastDayToUse, DateOnly.FromDateTime(dateTime.Now.Date.AddDays(-1)));
        curve.LastDayToUse = DateOnly.FromDateTime(dateTime.Now.Date.AddDays(-1));
    }

    private static async Task SaveQualityIssueAndHandleTaskCards(AnalyticalRun analyticalRun, CurveControlJudgmentResult judgmentResult)
    {
        if (analyticalRun != null)
        {
            // Gate: Do not process CC results for committed runs.
            // Post-commitment CCs should not modify a frozen run's state, issues, or triggger task card updates.
            // This ensures committed runs are truly terminal and immutable.
            if (analyticalRun.CurveState == CurveState.Frozen)
            {
                return;
            }

            var curveControlIssue = judgmentResult switch
            {
                CurveControlJudgmentResult.OK => AnalyticalRunIssue.NoIssue,
                CurveControlJudgmentResult.Flagged => AnalyticalRunIssue.Warning,
                CurveControlJudgmentResult.NotJudged => AnalyticalRunIssue.NoIssue,
                CurveControlJudgmentResult.NotOK or CurveControlJudgmentResult.ReRunNotOK => AnalyticalRunIssue.Error,
                _ => throw new ArgumentOutOfRangeException(nameof(judgmentResult), judgmentResult, null)
            };

            analyticalRun.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, CurveIssue: null, CurveControlIssue: curveControlIssue, QualityControlIssue: null, TestIssue: null));
        }
    }

    /// <summary>
    /// Get list of curve control test orders for the group from the database
    /// </summary>
    /// <param name="instrumentNumber">Instrument ID</param>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken"></param>
    private async Task<List<CurveControlTestResult>> GetListOfCurveControlsInGroup(string instrumentNumber, Guid groupId, CancellationToken cancellationToken)
    {
        var curveControlTestOrderList = await DbContext.CurveControlTestResults
            .Include(to => to.Replicates!)
            .ThenInclude(replicate => replicate.TestReplicateReagents)
            .Where(to => to.InstrumentNumber == instrumentNumber && to.GroupId == groupId)
            .ToListAsync(cancellationToken);

        return curveControlTestOrderList;
    }

    /// <summary>
    /// Get the conjugate and carrier lot numbers.
    /// </summary>
    /// <param name="testResults">List of test orders</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Conjugate and carrier lot numbers</returns>
    private async Task<(string ConjugateLotNumber, string CarrierLotNumber)> GetLotNumbers(List<CurveControlTestResult> testResults, CancellationToken cancellationToken)
    {
        var conjugateLotNumber = string.Empty;
        var carrierLotNumber = string.Empty;

        var firstReplicateId = new Guid();
        foreach (var to in testResults)
        {
            if (to.Replicates != null)
            {
                foreach (var r in to.Replicates)
                {
                    firstReplicateId = r.InstrumentReplicateId;
                    break;
                }
            }
        }

        if (firstReplicateId != Guid.Empty)
        {
            var articleList = await GetArticleList(firstReplicateId, cancellationToken);

            if (articleList.Count > 0)
            {
                foreach (var article in articleList)
                {
                    if (article.ReagentType == ArticleType.ConjugateCartridge)
                    {
                        conjugateLotNumber = article.LotNumber;
                    }

                    if (article.ReagentType is ArticleType.ImmunoCapCarrier or ArticleType.EliaCalibratorWellCarrier or ArticleType.EliaWellCarrier)
                    {
                        carrierLotNumber = article.LotNumber;
                    }
                }
            }
        }

        return (conjugateLotNumber, carrierLotNumber);
    }

    private async Task<List<ArticleData>> GetArticleList(Guid replicateId, CancellationToken cancellationToken)
    {
        var articleList = await (from r in DbContext.TestReplicates
                                 join rr in DbContext.TestReplicateReagents on r.Id equals rr.TestReplicateId
                                 where r.InstrumentReplicateId == replicateId &&
                                 (rr.ReagentType == ArticleType.ConjugateCartridge ||
                                 rr.ReagentType == ArticleType.ImmunoCapCarrier ||
                                 rr.ReagentType == ArticleType.EliaCalibratorWellCarrier ||
                                 rr.ReagentType == ArticleType.EliaWellCarrier)
                                 select new ArticleData
                                 {
                                     ReagentType = rr.ReagentType,
                                     LotNumber = rr.LotNumber,
                                 }).ToListAsync(cancellationToken);

        return articleList;
    }

    private class ArticleData
    {
        public ArticleType ReagentType { get; init; }
        public string LotNumber { get; init; } = string.Empty;
    }
}

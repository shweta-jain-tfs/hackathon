using MediatR;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

internal record AnalyticalRunCalculationCurveUpdated(int AnalyticalRunId) : INotification;

internal sealed class AnalyticalRunCalculationCurveUpdatedHandler(
    IResultDbContext dbContext,
    IResultCalculationService resultCalculationService,
    IDateTime dateTime)
    : INotificationHandler<AnalyticalRunCalculationCurveUpdated>
{
    public async Task Handle(AnalyticalRunCalculationCurveUpdated notification, CancellationToken cancellationToken)
    {
        var analyticalRun = await dbContext.AnalyticalRuns
            .Where(x => x.Id == notification.AnalyticalRunId)
            .Include(x => x.CalculationCurve)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        var calculationCurveId = analyticalRun.CurveId;
        var runInstrumentNumber = analyticalRun.InstrumentNumber;
        var runMethodName = analyticalRun.MethodName;
        var oldCalculationState = analyticalRun.CurveState;

        void AddChartUpdateEvents()
        {
            analyticalRun.AddDomainEvent(new CalibrationCurveChartUpdatedEvent(runInstrumentNumber, runMethodName));
            analyticalRun.AddDomainEvent(new CurveControlChartUpdatedEvent(runInstrumentNumber, runMethodName));
        }

        if (!calculationCurveId.HasValue)
        {
            return;
        }

        if (analyticalRun.CurveState == CurveState.Frozen)
        {
            return;
        }

        var isNotOkCurve = analyticalRun.CalculationCurve?.Judgment is not CurveJudgment.OK;
        // Only a non-OK curve blocks calculations.
        // But Flagged curves can only proceed if they've been explicitly accepted by the user
        // (state transitioned to Valid via AcceptCurveFromFlaggedCurveControlCommand).
        // A newly-arrived flagged curve in Invalid/Waiting states remains blocked.
        if (analyticalRun.CalculationCurve?.Judgment == CurveJudgment.Flagged)
        {
            if (analyticalRun.CurveState is CurveState.Valid or CurveState.Frozen)
            {
                isNotOkCurve = false;
            }
        }

        if (isNotOkCurve)
        {
            // NotOK / other failure curves block calculations
            analyticalRun.CurveState = CurveState.Invalid;
            AddChartUpdateEvents();
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var testResults = await GetTestResults(dbContext, analyticalRun, cancellationToken);

        foreach (var testResult in testResults)
        {
            var preDilutionFactor = testResult.PreDilutionFactor;
            var instrumentDilutionFactor = testResult.InstrumentDilutionFactor;
            var methodName = testResult.MethodName;
            var testName = testResult.TestName;
            var testOrderType = testResult.ResultType;

            if (testOrderType == TestResultType.Sample)
            {
                if (testResult is SampleTestResult sampleTestResult)
                {
                    if (sampleTestResult.ApprovalStatus != ResultApprovalStatus.NotApproved)
                    {
                        continue;
                    }
                }
            }

            foreach (var replicate in testResult.Replicates!)
            {
                await resultCalculationService.CalculateReplicateConcentration(
                    replicate, calculationCurveId.Value, preDilutionFactor, instrumentDilutionFactor, methodName, testName, testOrderType, cancellationToken);
            }

            if (testResult is SampleTestResult str)
            {
                testResult.AddDomainEvent(new SampleTestResultReadyEvent(str));
            }

            if (testResult is QualityControlTestResult qcr)
            {
                testResult.AddDomainEvent(new QualityControlResultGroupReadyEvent(qcr.InstrumentNumber, qcr.GroupId));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Commit the run after any successful recalculation, regardless of curve judgment.
        analyticalRun.CurveState = CurveState.Frozen;
        analyticalRun.FrozenAt = dateTime.UtcNow;

        // Clear run issues immediately so task-card evaluation uses the resolved state.
        analyticalRun.CalibrationCurveIssue = AnalyticalRunIssue.NoIssue;
        analyticalRun.CurveControlIssue = AnalyticalRunIssue.NoIssue;

        // User accepted the curve (OK or manually accepted flagged) — clear curve/CC issues so
        // task cards are removed and GUI signs reflect the resolved state.
        analyticalRun.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, CurveIssue: AnalyticalRunIssue.NoIssue, CurveControlIssue: AnalyticalRunIssue.NoIssue, QualityControlIssue: null, TestIssue: null));

        AddChartUpdateEvents();

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<TestResult>> GetTestResults(IResultDbContext dbContext, AnalyticalRun analyticalRun, CancellationToken cancellationToken)
    {
        return await dbContext.TestResults
            .Where(tr => tr.ResultType != TestResultType.Calibrator
                      && tr.ResultType != TestResultType.CurveControl
                      && tr.AnalyticalRunId == analyticalRun.Id)
            .Include(tr => tr.Replicates)
            .OrderBy(tr => tr.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

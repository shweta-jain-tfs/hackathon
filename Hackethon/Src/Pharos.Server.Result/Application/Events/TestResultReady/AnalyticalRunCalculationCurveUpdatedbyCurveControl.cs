using MediatR;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events.TestResultReady;

internal record AnalyticalRunCalculationCurveUpdatedByCurveControl(int AnalyticalRunId) : INotification;

internal sealed class AnalyticalRunCalculationCurveUpdatedByCurveControlHandler(
    IResultDbContext dbContext,
    IResultCalculationService resultCalculationService,
    IDateTime dateTime)
    : INotificationHandler<AnalyticalRunCalculationCurveUpdatedByCurveControl>
{
    public async Task Handle(AnalyticalRunCalculationCurveUpdatedByCurveControl notification, CancellationToken cancellationToken)
    {
        var analyticalRun = await dbContext.AnalyticalRuns
            .Where(x => x.Id == notification.AnalyticalRunId)
            .Include(x => x.CalculationCurve)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        // Only committed curve unlocks calculations.
        // Flagged / NotOK → need a accepted curve to calculate concentrations. If the curve is not accepted, the run is not committed to calculating and the concentrations are not calculated.
        if (analyticalRun.CurveId == null || !analyticalRun.CanCalculateConcentrations)
        {
            return;
        }

        var replicatesFromDb = await dbContext.TestResults
            .Where(x => x.AnalyticalRunId == analyticalRun.Id)
            .Include(x => x.Replicates)
            .SelectMany(x => x.Replicates!)
            .Include(x => x.TestResult)
            .ToListAsync(cancellationToken);

        var r = replicatesFromDb.Where(x => x.Concentration == null && x is { IsAborted: false, IsLowResponse: false });

        foreach (var replicate in r)
        {
            var curveId = analyticalRun.CurveId;
            var testResult = replicate.TestResult;

            //Only recalculate not-approved tests
            if (testResult.ResultType == TestResultType.Sample)
            {
                if (testResult is SampleTestResult sampleTestResult)
                {
                    if (sampleTestResult.ApprovalStatus != ResultApprovalStatus.NotApproved)
                    {
                        continue;
                    }
                }
            }

            if (curveId > 0)
            {
                await resultCalculationService.CalculateReplicateConcentration(
                    replicate,
                    curveId.Value,
                    testResult.PreDilutionFactor,
                    testResult.InstrumentDilutionFactor,
                    testResult.MethodName,
                    testResult.TestName,
                    testResult.ResultType,
                    cancellationToken);
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

        // Curve is OK — commit the run to calculating after the first real calculation.
        analyticalRun.CurveState = CurveState.Frozen;
        analyticalRun.FrozenAt = dateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

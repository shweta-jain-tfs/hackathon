using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

internal record NewValidReagentBlank(Blank Blank) : INotification;

internal sealed class NewValidReagentBlankHandler(
    IResultDbContext dbContext,
    IResponseCalculationService responseCalculationService,
    IResultCalculationService resultCalculationService,
    IMethodsService methodsService,
    IPublisher publisher)
    : INotificationHandler<NewValidReagentBlank>
{
    public async Task Handle(NewValidReagentBlank notification, CancellationToken cancellationToken)
    {

        var blank = notification.Blank;
        var methodList = await GetListOfMethods(blank.BlankType);

        var testResults = await GetTestResultsContainingReplicatesWithoutResponse(blank.InstrumentNumber, blank.DevSolutionBarcode, blank.DevSolutionCellPosition, methodList, cancellationToken);

        if (testResults.Count == 0)
        {
            return;
        }

        var calGroupIdHandled = new List<Guid>();
        var ccGroupIdHandled = new List<Guid>();

        var resultsWithPendingCalibration = new List<ITestResult>();

        foreach (var testResult in testResults.OrderBy(tr => tr.Id))
        {
            int? curveId = null;
            var canCalculate = false;

            foreach (var replicate in testResult.Replicates!)
            {

                if (replicate.FluorometerValue == null || replicate.Response != null)
                {
                    continue;
                }

                // This replicate is measured, but has no response.
                await responseCalculationService.CalculateResponseForTestReplicate(replicate, blank, testResult.ResultType, testResult.MethodName, GetCalibratorCurveControlName(testResult));

                // we skip concentration for calibration points
                if (testResult is CalibratorTestResult)
                {
                    continue;
                }

                // Try calculating the replicate concentration
                if (curveId == null)
                {
                    var calculationContext = await GetCalculationContext(testResult.AnalyticalRunId);
                    curveId = calculationContext.CurveId;
                    canCalculate = calculationContext.CanCalculate;
                }

                if (canCalculate && curveId > 0)
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
            }

            if (testResult.AllReplicatesHaveResponse)
            {
                responseCalculationService.CalculateMeanResponse(testResult);

                if (testResult is SampleTestResult str)
                {
                    testResult.Status = TestResultStatus.PendingCalibration;
                    if (canCalculate && curveId > 0)
                    {
                        testResult.AddDomainEvent(new SampleTestResultReadyEvent(str));
                    }
                    else
                    {
                        // When calculation is not committed or there is no curve, keep PendingCalibration
                        // and propagate this status to the test order.
                        resultsWithPendingCalibration.Add(str);
                    }
                }

                if (testResult is QualityControlTestResult qcr)
                {
                    testResult.Status = TestResultStatus.PendingCalibration;

                    if (canCalculate && curveId > 0)
                    {
                        testResult.AddDomainEvent(new QualityControlResultGroupReadyEvent(qcr.InstrumentNumber, qcr.GroupId));
                    }
                }

                if (testResult is CalibratorTestResult ctr)
                {
                    if (!calGroupIdHandled.Contains(ctr.GroupId)) // Only report group completed once
                    {
                        testResult.AddDomainEvent(new CalibratorResultGroupReadyEvent(ctr.InstrumentNumber, ctr.GroupId, ctr.MethodName));
                        calGroupIdHandled.Add(ctr.GroupId);
                    }
                }

                if (testResult is CurveControlTestResult ccr)
                {
                    if (!ccGroupIdHandled.Contains(ccr.GroupId)) // Only report group completed once
                    {
                        testResult.AddDomainEvent(new CurveControlResultGroupReadyEvent(ccr.InstrumentNumber, ccr.GroupId, ccr.MethodName));
                        ccGroupIdHandled.Add(ccr.GroupId);
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (resultsWithPendingCalibration.Count > 0)
        {
            await publisher.Publish(new TestResultStatusChangedEvent(resultsWithPendingCalibration), cancellationToken);
        }
    }

    private async Task<List<string>> GetListOfMethods(BlankType blankType)
    {
        return blankType switch
        {
            BlankType.ReagentBlankImmunoCap => await methodsService.GetMethods(m => m.Technology != AssayTechnology.EliA, m => m.MethodName),
            _ => await methodsService.GetMethods(m => m.Technology == AssayTechnology.EliA, m => m.MethodName),
        };
    }

    private async Task<(int CurveId, bool CanCalculate)> GetCalculationContext(int? analyticalRunId)
    {
        var run = await dbContext.AnalyticalRuns
            .FirstOrDefaultAsync(r => r.Id == analyticalRunId)
            .ConfigureAwait(false);

        if (run == null)
        {
            return (0, false);
        }

        var curveId = run.CurveId ?? 0;
        var canCalculate = run.CanCalculateConcentrations && curveId > 0;

        return (curveId, canCalculate);
    }

    private static string GetCalibratorCurveControlName(TestResult testResult)
    {
        if (testResult is CalibratorTestResult ctr)
        {
            return ctr.CalibratorPointName;
        }
        if (testResult is CurveControlTestResult ccr)
        {
            return ccr.CurveControlPointName;
        }
        return string.Empty;
    }

    private async Task<List<TestResult>> GetTestResultsContainingReplicatesWithoutResponse(string instrumentNumber, string devSolutionBarcode, int devSolutionCellPosition, List<string> methodList, CancellationToken cancellationToken)
    {
        var testResultIdList = await dbContext.TestReplicates
            .Where(r => r.Response == null && !r.IsAborted
                && r.TestReplicateReagents!.Any(rr => rr.TestReplicateId == r.Id
                    && rr.ReagentType == ArticleType.DevelopmentSolutionCartridge && rr.Barcode == devSolutionBarcode && rr.CellPosition == devSolutionCellPosition))
            .Select(r => r.TestResultId)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var testResults = await dbContext.TestResults
                .Where(r => testResultIdList.Contains(r.Id) && r.InstrumentNumber == instrumentNumber && methodList.Contains(r.MethodName))
                .Include(r => r.Replicates!)
                .ThenInclude(r => r.TestReplicateReagents)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

        return testResults;
    }
}

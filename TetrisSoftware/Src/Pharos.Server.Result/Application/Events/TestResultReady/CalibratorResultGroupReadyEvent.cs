using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events.TestResultReady;

internal record CalibratorResultGroupReadyEvent(string InstrumentNumber, Guid GroupId, string MethodName) : INotification;

internal sealed class CalibratorResultGroupReadyEventEventHandler(
    IResultDbContext dbContext,
    IResultCalculationService resultCalculationService,
    IMethodsService methodsService,
    ISender sender,
    ILogger<CalibratorResultGroupReadyEventEventHandler> logger) : TestResultReadyEventHandlerBase<CalibratorTestResult>(dbContext), INotificationHandler<CalibratorResultGroupReadyEvent>
{
    public async Task Handle(CalibratorResultGroupReadyEvent notification, CancellationToken cancellationToken)
    {
        var calibratorTestResultList = await GetListOfCalibratorsInGroup(notification.InstrumentNumber, notification.GroupId, cancellationToken);
        if (await CheckCalibratorTestResultList(notification.MethodName, calibratorTestResultList))
        {
            foreach (var testResult in calibratorTestResultList)
            {
                await SetTestResultStatus(testResult, cancellationToken);
            }
            await resultCalculationService.CheckAndFitCurve(notification.InstrumentNumber, notification.GroupId, calibratorTestResultList, cancellationToken);
        }
        else
        {
            var analyticalRunId = calibratorTestResultList.FirstOrDefault()?.AnalyticalRunId;
            await SaveQualityIssue(analyticalRunId, AnalyticalRunIssue.Error);
            await sender.Send(new TaskCardCommand(TaskCardType.CalibrationIssue, TaskCardState.Error, notification.InstrumentNumber), cancellationToken);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Get list of calibrator test orders for the group from the database
    /// </summary>
    /// <param name="instrumentNumber">Instrument ID</param>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken"></param>
    private async Task<List<CalibratorTestResult>> GetListOfCalibratorsInGroup(string instrumentNumber, Guid groupId, CancellationToken cancellationToken)
    {
        var calibratorTestResultList = await DbContext.CalibratorTestResults
            .Include(to => to.Replicates)
            .Where(to => to.InstrumentNumber == instrumentNumber && to.GroupId == groupId)
            .ToListAsync(cancellationToken);

        return calibratorTestResultList;
    }

    /// <summary>
    /// Check list of calibrator test results to see if it is OK for curve fit
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <param name="calibratorTestResultList">List of calibrator test orders</param>
    /// <returns>True if list OK</returns>
    private async Task<bool> CheckCalibratorTestResultList(string methodName, List<CalibratorTestResult> calibratorTestResultList)
    {
        if (calibratorTestResultList.Count == 0)
        {
            logger.LogWarning("No test results in the calibrator group.");
            return false;
        }

        if (calibratorTestResultList.First().GroupSize > calibratorTestResultList.Count)
        {
            // the group is not complete, the event will be re-sent later
            return false;
        }

        foreach (var testResult in calibratorTestResultList)
        {
            if (testResult.NumberOfReplicates > testResult.Replicates!.Count)
            {
                // one of the test orders in a group is not complete, the event will be re-sent later
                return false;
            }
        }

        var expectedNumberOfTestResults = await methodsService.GetMethod(methodName, m => m.CalibrationCurve.CalibrationPoints.Count);
        if (calibratorTestResultList.Count != expectedNumberOfTestResults)
        {
            logger.LogWarning("Wrong number of calibrator points.");
            return false;
        }

        // An aborted replicate is complete even though it has no response.
        // All other replicates must have a response before the curve can be checked.
        foreach (var calibratorTestOrder in calibratorTestResultList)
        {
            if (calibratorTestOrder.Replicates != null &&
                calibratorTestOrder.Replicates.Count != 0 &&
                calibratorTestOrder.Replicates.All(r => r.Response != null || r.IsAborted))
            {
                continue;
            }

            logger.LogWarning("Not all replicates are complete for {PointName}", calibratorTestOrder.CalibratorPointName);
            return false;
        }

        return true;
    }

    private async Task SaveQualityIssue(int? analyticalRunId, AnalyticalRunIssue curveIssue)
    {
        if (analyticalRunId != null)
        {
            var analyticalRun = await DbContext.AnalyticalRuns.FirstOrDefaultAsync(r => r.Id == analyticalRunId);
            analyticalRun?.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, curveIssue, CurveControlIssue: null, QualityControlIssue: null, TestIssue: null));
        }
    }
}

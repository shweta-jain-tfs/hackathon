using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;
internal record EvaluateCurveControlsCommand(
    List<CurveControlTestResult> CurveControlResults,
    string MethodName,
    Guid GroupId,
    Guid? RunId,
    string InstrumentNumber,
    int? CalculationCurveId,
    string ConjugateLotNumber,
    string ImmunoCapLotNumber) : IRequest<CurveControlJudgmentResult>;

internal class EvaluateCurveControlsCommandHandler(IResultDbContext dbContext, IMethodsService methodsService) : IRequestHandler<EvaluateCurveControlsCommand, CurveControlJudgmentResult>
{
    public async Task<CurveControlJudgmentResult> Handle(EvaluateCurveControlsCommand request, CancellationToken cancellationToken)
    {
        var nOk = 0;
        var nHighFlagged = 0;
        var nLowFlagged = 0;
        var nHigh = 0;
        var nLow = 0;
        var allAborted = true;
        var anyAborted = false;
        var oldestResultId = request.CurveControlResults.OrderBy(r => r.Id).Select(r => r.Id).First();

        var previousCurveControlsNotOk = await CheckPreviousCurveControls(request.MethodName, request.GroupId, request.RunId, request.InstrumentNumber, request.ConjugateLotNumber, request.ImmunoCapLotNumber, oldestResultId);
        foreach (var curveControlTestOrder in request.CurveControlResults)
        {
            CurveControlStatusCount(curveControlTestOrder, ref nOk, ref nHighFlagged, ref nLowFlagged, ref nHigh, ref nLow, ref allAborted, ref anyAborted);
        }

        var atLeastOneHasConcentration = request.CurveControlResults.Any(cc => cc.MeanConcentration.HasValue);
        var conclusion = GetConclusion(nHighFlagged, nLowFlagged, nHigh, nLow, allAborted, previousCurveControlsNotOk, atLeastOneHasConcentration);

        await UpdateLastCurveControlsAbortedOnCurve(request.CalculationCurveId, anyAborted && !allAborted);

        return conclusion;
    }

    private async Task<bool> CheckPreviousCurveControls(string methodName, Guid groupId, Guid? runId, string instrumentNumber, string conjugateLotNumber, string immunoCapLotNumber, int oldestResultId)
    {
        var connectImmunoCapAndConjugate = await ConnectImmunoCapAndConjugate(methodName);
        var previousCurveControls = await ReadPreviousCurveControlsInSameAssayRun(groupId, runId, instrumentNumber, methodName, connectImmunoCapAndConjugate, conjugateLotNumber, immunoCapLotNumber, oldestResultId);
        var previousCurveControlsNotOk = false;

        var nTotalReplicates = 0;
        var nTotalAbortedReplicates = 0;

        foreach (var previousCurveControl in previousCurveControls)
        {
            if (previousCurveControl.JudgmentSummary != CurveControlJudgment.NotDefined &&
                previousCurveControl.JudgmentSummary != CurveControlJudgment.OK)
            {
                previousCurveControlsNotOk = true;
                break;
            }

            var replicateCount = await CountReplicatesAborted(previousCurveControl.Id);
            nTotalReplicates += replicateCount.Replicates;
            nTotalAbortedReplicates += replicateCount.AbortedReplicates;
        }

        if (nTotalReplicates > 0 && nTotalReplicates == nTotalAbortedReplicates)
        {
            previousCurveControlsNotOk = true;
        }

        return previousCurveControlsNotOk;
    }

    private async Task<(int Replicates, int AbortedReplicates)> CountReplicatesAborted(int testResultId)
    {
        (int Replicates, int AbortedReplicates) result = (0, 0);

        var countData = await dbContext
            .CurveControlTestResults
            .Where(tr => tr.Id == testResultId && tr.Replicates != null)
            .Select(tr => new
            {
                ReplicateCount = tr.Replicates!.Count(),
                AbortedReplicateCount = tr.Replicates!.Count(r => r.IsAborted)

            }).FirstOrDefaultAsync();
        if (countData != null)
        {
            result.Replicates = countData.ReplicateCount;
            result.AbortedReplicates = countData.AbortedReplicateCount;
        }

        return result;
    }

    private Task<bool> ConnectImmunoCapAndConjugate(string methodName)
    {
        return methodsService.GetMethod(methodName, m => m.AnalyticalRun.RunCalibrationOnCarrierLotChange);
    }

    private async Task<List<CurveControlTestResult>> ReadPreviousCurveControlsInSameAssayRun(Guid groupId, Guid? runId, string instrumentNumber, string methodName, bool connectImmunoCapAndConjugate, string conjugateLotNumber, string carrierLotNumber, int oldestResultId)
    {

        List<int> curveControlTestResultIdList;

        if (connectImmunoCapAndConjugate)
        {
            // CC results with matching run ID, instrument ID, method name, conjugate lot number and carrier lot number, but skipping results with the supplied CC group ID
            curveControlTestResultIdList = await (from ccTestResult in dbContext.CurveControlTestResults
                                                  let ar = ccTestResult.AnalyticalRun
                                                  where ccTestResult.InstrumentNumber == instrumentNumber &&
                                                        ccTestResult.GroupId != groupId &&
                                                        ccTestResult.Id < oldestResultId &&
                                                        ar.InstrumentRunId == runId &&
                                                        ar.MethodName == methodName &&
                                                        ar.ConjugateLotNumber == conjugateLotNumber &&
                                                        ar.CarrierLotNumber == carrierLotNumber
                                                  select ccTestResult.Id).ToListAsync();
        }
        else
        {
            // CC results with matching run ID, instrument ID, method name and conjugate lot number, but skipping results with the supplied CC group ID
            curveControlTestResultIdList = await (from ccTestResult in dbContext.CurveControlTestResults
                                                  let ar = ccTestResult.AnalyticalRun
                                                  where ccTestResult.InstrumentNumber == instrumentNumber &&
                                                        ccTestResult.GroupId != groupId &&
                                                        ccTestResult.Id < oldestResultId &&
                                                        ar.InstrumentRunId == runId &&
                                                        ar.MethodName == methodName &&
                                                        ar.ConjugateLotNumber == conjugateLotNumber
                                                  select ccTestResult.Id).ToListAsync();
        }

        // Use the ID list to get the whole result objects
        var curveControlTestResultList = await (from ccRes in dbContext.CurveControlTestResults
                                                where curveControlTestResultIdList.Contains(ccRes.Id)
                                                select ccRes).ToListAsync();
        return curveControlTestResultList;
    }

    private async Task UpdateLastCurveControlsAbortedOnCurve(int? calculationCurveId, bool someButNotAllAborted)
    {
        if (calculationCurveId.HasValue)
        {
            var curve = await dbContext.Curves.FirstOrDefaultAsync(c => c.Id == calculationCurveId.Value);
            if (curve != null)
            {
                curve.LastCurveControlsAborted = someButNotAllAborted;
            }
        }
    }

    private static CurveControlJudgmentResult GetConclusion(int nHighFlagged, int nLowFlagged, int nHigh, int nLow, bool allAborted, bool previousCurveControlsNotOk, bool atLeastOneHasConcentration)
    {
        var conclusion = CurveControlJudgmentResult.OK;

        // If any previous CC is other than OK, all in this case must be OK too
        if (previousCurveControlsNotOk)
        {
            if (nHighFlagged + nLowFlagged + nHigh + nLow > 0)
            {
                conclusion = CurveControlJudgmentResult.ReRunNotOK;
            }
            if (allAborted)
            {
                conclusion = CurveControlJudgmentResult.ReRunNotOK;
            }
        }
        else
        {
            if (nHighFlagged > 0 || nLowFlagged > 0)
            {
                conclusion = CurveControlJudgmentResult.Flagged;
            }
            if (allAborted)
            {
                conclusion = CurveControlJudgmentResult.Flagged;
            }
            if (nHigh > 0 || nLow > 0 || (nHighFlagged > 0 && nLowFlagged > 0))
            {
                conclusion = CurveControlJudgmentResult.NotOK;
            }
        }

        if (!atLeastOneHasConcentration && conclusion == CurveControlJudgmentResult.OK)
        {
            return CurveControlJudgmentResult.NotJudged;
        }

        return conclusion;
    }

    private static void CurveControlStatusCount(CurveControlTestResult curveControlTestOrder, ref int nOK, ref int nHighFlagged, ref int nLowFlagged, ref int nHigh, ref int nLow, ref bool allAborted, ref bool anyAborted)
    {
        if (curveControlTestOrder.Replicates != null)
        {
            var curveControlReplicates = curveControlTestOrder.Replicates.Cast<CurveControlTestReplicate>().ToList();

            nOK += curveControlReplicates.Count(r => r.Judgment == CurveControlJudgment.OK);
            nHighFlagged += curveControlReplicates.Count(r => r.Judgment == CurveControlJudgment.FlaggedHigh);
            nLowFlagged += curveControlReplicates.Count(r => r.Judgment == CurveControlJudgment.FlaggedLow);
            nHigh += curveControlReplicates.Count(r => r.Judgment == CurveControlJudgment.High);
            nLow += curveControlReplicates.Count(r => r.Judgment == CurveControlJudgment.Low);

            var abortedInfoList = (from r in curveControlTestOrder.Replicates
                                   group r by r.IsAborted into abortedGroup
                                   select abortedGroup).ToList();
            foreach (var abortedInfo in abortedInfoList)
            {
                if (abortedInfo.Key && abortedInfo.Any())
                {
                    anyAborted = true;
                }
                if (!abortedInfo.Key && abortedInfo.Any())
                {
                    allAborted = false;
                }
            }
        }
    }
}
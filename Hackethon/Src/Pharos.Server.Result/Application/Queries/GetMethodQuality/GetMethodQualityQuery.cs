using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetMethodQuality;

internal sealed class GetMethodQualityQueryHandler(IResultDbContext context, IMethodsService methodsService, IInstrumentManager instrumentManager, IDateTime dateTime)
    : IRequestHandler<GetMethodQualityQuery, List<MethodQualityDto>>
{
    public async Task<List<MethodQualityDto>> Handle(GetMethodQualityQuery request, CancellationToken cancellationToken)
    {
        var list = await methodsService.GetMethods(x => true, x => new MethodQualityDto { Id = x.Id, MethodName = x.MethodName });

        if (request.MethodNameList is not { Count: 0 })
        {
            list = list.Where(x => request.MethodNameList.Contains(x.MethodName)).ToList();
        }

        var methodList = list.Select(x => x.MethodName).ToList();
        var activeCurves = await context.CurrentCurves.Where(x => methodList.Contains(x.MethodName)).Include(x => x.Curve).ToListAsync(cancellationToken);
        await InsertActiveCurvesInList(list, activeCurves, request.InstrumentNumberList);

        return list;
    }

    private async Task InsertActiveCurvesInList(List<MethodQualityDto> list, List<CurrentCurve> activeCurves, List<string> instrumentNumberList)
    {
        var originalList = list.ToList();
        list.Clear();
        var instrumentInfo = instrumentManager.GetInstruments();

        foreach (var listItem in originalList)
        {
            var maxNumberOfDays = await methodsService.GetCalibrationCurveDaysValidity(listItem.MethodName);

            if (instrumentNumberList.Count > 0)
            {
                foreach (var instrumentNumber in instrumentNumberList)
                {
                    await InsertActiveCurvesInListForInstrument(list, listItem, activeCurves, instrumentNumber, instrumentInfo, maxNumberOfDays);
                }
            }
            else
            {
                foreach (var instrumentNumber in instrumentInfo.Select(x => x.SerialNumber).ToList())
                {
                    await InsertActiveCurvesInListForInstrument(list, listItem, activeCurves, instrumentNumber, instrumentInfo, maxNumberOfDays);
                }
            }
        }
    }

    private async Task InsertActiveCurvesInListForInstrument(List<MethodQualityDto> list, MethodQualityDto listItem, List<CurrentCurve> activeCurves, string instrumentNumber,
        ICollection<IInstrument> instrumentInfo, int maxNumberOfDays)
    {
        var curves = activeCurves
            .Where(x => x.MethodName == listItem.MethodName && x.Curve.InstrumentNumber == instrumentNumber)
            .ToList();
        var instrumentDisplayName = instrumentInfo
            .Where(x => x.SerialNumber == instrumentNumber)
            .Select(x => x.DisplayName).FirstOrDefault() ?? "";

        var curveByConjugateLot =
            (from c in curves
             group c by c.ConjugateLotNumber
            into lotGroup
             select lotGroup).ToList();

        if (curves.Count > 0)
        {
            foreach (var curveGroup in curveByConjugateLot)
            {
                foreach (var curve in curveGroup)
                {
                    list.Add(new MethodQualityDto()
                    {
                        Id = listItem.Id,
                        MethodName = listItem.MethodName,
                        InstrumentNumber = instrumentNumber,
                        InstrumentName = instrumentDisplayName,
                        MaxNumberOfDays = maxNumberOfDays,
                        ConjugateLotNumber = curve.ConjugateLotNumber,
                        CurveControlsFailed = curve.Curve.CurveControlDisabledFlag == true,
                        MeasurementDateTime = curve.Curve.MeasurementDateTime,
                        CurrentIssue = await GetCurrentIssue(instrumentNumber, curve),
                    });
                }
            }
        }
        else
        {
            list.Add(new MethodQualityDto
            {
                Id = listItem.Id,
                MethodName = listItem.MethodName,
                InstrumentNumber = instrumentNumber,
                InstrumentName = instrumentDisplayName,
            });
        }
    }

    private async Task<MethodQualityIssue> GetCurrentIssue(string instrumentNumber, CurrentCurve curve)
    {
        var issue = MethodQualityIssue.NoIssues;

        if (curve.Curve.CurveControlDisabledFlag == true)
        {
            issue = MethodQualityIssue.LastCcFailed;
        }

        else if (await LastCurveControlFlagged(instrumentNumber, curve))
        {
            issue = MethodQualityIssue.LastCcFlagged;
        }

        else if (await LastQualityControlFailed(instrumentNumber, curve))
        {
            issue = MethodQualityIssue.LastQcFailed;
        }

        // TODO: Expired curve

        return issue;
    }

    private async Task<bool> LastCurveControlFlagged(string instrumentNumber, CurrentCurve curve)
    {
        var timeLimit = dateTime.UtcNow.Date.AddDays(-30);
        var methodName = curve.MethodName;
        var conjugateLotNumber = curve.ConjugateLotNumber;

        var lastCcResults = await (from qc in context.CurveControlTestResults
                                   where qc.InstrumentNumber == instrumentNumber &&
                                         qc.MethodName == methodName &&
                                         qc.MeasurementDateTime >= timeLimit &&
                                         qc.Replicates!.Any(x => x.TestReplicateReagents!.Any(y => y.ReagentType == ArticleType.ConjugateCartridge && y.LotNumber == conjugateLotNumber))
                                   group qc by new
                                   {
                                       qc.CurveControlPointName,
                                   }
            into qcGroup
                                   select qcGroup.OrderByDescending(x => x.MeasurementDateTime).Select(x => x.JudgmentSummary).First()).ToListAsync();

        return lastCcResults.Any(x => x == CurveControlJudgment.FlaggedLow || x == CurveControlJudgment.FlaggedHigh);
    }

    private async Task<bool> LastQualityControlFailed(string instrumentNumber, CurrentCurve curve)
    {
        var timeLimit = dateTime.UtcNow.Date.AddDays(-30);
        var methodName = curve.MethodName;
        var conjugateLotNumber = curve.ConjugateLotNumber;

        var lastQcResult = await (from qc in context.QualityControlTestResults
                                  where qc.InstrumentNumber == instrumentNumber &&
                                        qc.MethodName == methodName &&
                                        qc.MeasurementDateTime >= timeLimit &&
                                        qc.Replicates!.Any(x => x.TestReplicateReagents!.Any(y => y.ReagentType == ArticleType.ConjugateCartridge && y.LotNumber == conjugateLotNumber))
                                  group qc by new
                                  {
                                      qc.QualityControlName,
                                      qc.TestName
                                  }
            into qcGroup
                                  select qcGroup.OrderByDescending(x => x.MeasurementDateTime).Select(x => x.Judgment).First()).ToListAsync();

        return lastQcResult.Any(x => x != QualityControlJudgement.Ok);
    }
}

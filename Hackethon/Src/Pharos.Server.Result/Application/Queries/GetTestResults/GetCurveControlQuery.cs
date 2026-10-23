using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

internal record GetCurveControlQuery(int AnalyticalRunId) : IRequest<List<CurveControlTestResultDto>>;

internal sealed class GetCurveControlQueryHandler(IResultDbContext context, IMethodsService methodsService) : IRequestHandler<GetCurveControlQuery, List<CurveControlTestResultDto>>
{
    public async Task<List<CurveControlTestResultDto>> Handle(GetCurveControlQuery request, CancellationToken cancellationToken)
    {
        // Fetch only what's needed from AnalyticalRun — no CC rows yet
        var analyticalRun = await context.CurveControlTestResults
            .Where(x => x.AnalyticalRunId == request.AnalyticalRunId)
            .Select(x => x.AnalyticalRun)
            .FirstOrDefaultAsync(cancellationToken);

        // Early exit: no run, no usable curve, or manually selected — nothing to return
        if (analyticalRun == null
            || analyticalRun.CurveId == null
            || analyticalRun.CurveState == CurveState.WaitingForNewCurve
            || analyticalRun.CurveState == CurveState.WaitingForCurveControl
            || analyticalRun.CurveSource == CurveSource.ManuallySelected
            || analyticalRun.CurveSource == CurveSource.CurveFromCurrentAssay) //Todo sak niklas
        {
            return [];
        }

        // Resolve the curve cutoff date once — push filter to the DB
        var curveMeasurementDateTime = await context.Curves
            .Where(x => x.Id == analyticalRun.CurveId)
            .Select(x => (DateTimeOffset?)x.MeasurementDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        // If the curve row is missing we cannot validate CC points
        if (curveMeasurementDateTime == null)
        {
            return [];
        }

        // Build the base server-side filtered query — Replicates excluded (unused in projection)
        var query = context.CurveControlTestResults
            .Where(x => x.AnalyticalRunId == request.AnalyticalRunId
                     && x.MeasurementDateTime >= curveMeasurementDateTime);

        // For committed runs, also cap at the commitment timestamp (server-side) accepted or manually selected curve
        if (analyticalRun.CurveState == CurveState.Frozen
            && analyticalRun.FrozenAt != null)
        {
            var committedAt = analyticalRun.FrozenAt;
            query = query.Where(x => x.MeasurementDateTime <= committedAt);
        }

        var items = await query
            .Select(result => new CurveControlTestResultDto
            {
                Id = result.Id,
                CurveControlPointName = result.CurveControlPointName,
                JudgmentSummary = result.JudgmentSummary,
                TestName = result.TestName,
                MethodName = result.MethodName,
                MeanResponseAsText = result.MeanResponseAsText,
                MeanConcentrationAsText = result.MeanConcentrationAsText,
                ExpectedConcentration = result.ExpectedConcentration,
                MeasurementDateTime = result.MeasurementDateTime,
                Comment = result.OperatorComment,
                Replicates = result.Replicates!.OfType<CurveControlTestReplicate>().Select(r => new CurveControlTestReplicateDto
                {
                    Id = r.Id,
                    Concentration = r.Concentration,
                    MeasurementDateTime = r.MeasurementDateTime,
                    CurveControlJudgment = r.Judgment,
                    ReplicateNumber = r.ReplicateNumber,
                    ResponseAsText = r.ResponseAsText,
                    ConcentrationAsText = r.ConcentrationAsText,
                    HasError = r.TestReplicateErrors != null && r.TestReplicateErrors.Any(),
                    IsAborted = r.IsAborted
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            //get concentration unit
            item.Unit = await methodsService.GetMethodTestConcentrationUnit(item.MethodName, item.TestName) ?? string.Empty;
        }

        return items;
    }
}
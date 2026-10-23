using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

internal record GetQualityControlQuery(int AnalyticalRunId) : IRequest<List<QualityControlTestResultDto>>;

internal sealed class GetQualityControlQueryHandler(IResultDbContext context, IMethodsService methodsService) : IRequestHandler<GetQualityControlQuery, List<QualityControlTestResultDto>>
{
    public async Task<List<QualityControlTestResultDto>> Handle(GetQualityControlQuery request, CancellationToken cancellationToken)
    {
        var cc = await context.QualityControlTestResults
            .Where(x => x.AnalyticalRunId == request.AnalyticalRunId)
            .Include(x => x.Replicates)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = cc.Select(result => new QualityControlTestResultDto
        {
            Id = result.Id,
            Identity = result.QualityControlName,
            Judgment = result.Judgment,
            TestName = result.TestName,
            MethodName = result.MethodName,
            MeanResponseAsText = result.MeanResponseAsText,
            Concentration = result.MeanConcentration,
            MeanConcentrationAsText = result.MeanConcentrationAsText,
            MinValue = result.MinValue,
            MaxValue = result.MaxValue,
            ExpectedConcentration = result.ExpectedConcentration,
            ExpectedStdDev = result.ExpectedStdDev,
            StdDevLimit = result.StdDevLimit,
            Replicates = result.Replicates!.Select(r => new QualityControlTestReplicateDto
            {
                Id = r.Id,
                MeasurementDateTime = r.MeasurementDateTime,
                ResponseAsText = r.ResponseAsText,
                ConcentrationAsText = r.ConcentrationAsText,
                HasError = r.TestReplicateErrors?.Any() ?? false,
                ReplicateNumber = r.ReplicateNumber,
                IsAborted = r.IsAborted
            }).ToList(),
            MeasurementDateTime = result.MeasurementDateTime,
            Comment = result.OperatorComment,
            InstrumentDilution = result.InstrumentDilutionFactor,
            PreDilution = result.PreDilutionFactor,
            TotalDilution = result.InstrumentDilutionFactor * result.PreDilutionFactor
        }).ToList();

        foreach (var item in items)
        {
            //get concentration unit
            item.Unit = await methodsService.GetMethodTestConcentrationUnit(item.MethodName, item.TestName) ?? string.Empty;
        }

        return items;
    }
}
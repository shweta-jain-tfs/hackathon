using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

internal record GetQualityControlTestResultQuery(int TestResultId) : IRequest<QualityControlTestResultDto>;

internal sealed class GetQualityControlTestResultQueryHandler(IResultDbContext context, IMethodsService methodsService) : IRequestHandler<GetQualityControlTestResultQuery, QualityControlTestResultDto>
{
    public async Task<QualityControlTestResultDto> Handle(GetQualityControlTestResultQuery request, CancellationToken cancellationToken)
    {
        var result = await context.QualityControlTestResults
            .Where(x => x.Id == request.TestResultId)
            .Include(x => x.Replicates)
            .Select(qc => new QualityControlTestResultDto
            {
                Id = qc.Id,
                Identity = qc.QualityControlName,
                Judgment = qc.Judgment,
                TestName = qc.TestName,
                MethodName = qc.MethodName,
                MeanResponseAsText = qc.MeanResponseAsText,
                Concentration = qc.MeanConcentration,
                MeanConcentrationAsText = qc.MeanConcentrationAsText,
                MinValue = qc.MinValue,
                MaxValue = qc.MaxValue,
                ExpectedConcentration = qc.ExpectedConcentration,
                ExpectedStdDev = qc.ExpectedStdDev,
                StdDevLimit = qc.StdDevLimit,
                Replicates = qc.Replicates!.Select(r => new QualityControlTestReplicateDto
                {
                    Id = r.Id,
                    MeasurementDateTime = r.MeasurementDateTime,
                    ResponseAsText = r.ResponseAsText,
                    ConcentrationAsText = r.ConcentrationAsText,
                    HasError = r.TestReplicateErrors!.Any(),
                    ReplicateNumber = r.ReplicateNumber,
                    IsAborted = r.IsAborted
                }).ToList(),
                MeasurementDateTime = qc.MeasurementDateTime,
                Comment = qc.OperatorComment,
                InstrumentDilution = qc.InstrumentDilutionFactor,
                PreDilution = qc.PreDilutionFactor,
                TotalDilution = qc.InstrumentDilutionFactor * qc.PreDilutionFactor
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (result == null)
        {
            throw new KeyNotFoundException($"Quality control test result with ID {request.TestResultId} not found.");
        }

        result.Unit = await methodsService.GetMethodTestConcentrationUnit(result.MethodName, result.TestName) ?? string.Empty;

        return result;
    }
}
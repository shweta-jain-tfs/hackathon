using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public record GetSampleTestResultQuery(int TestResultId) : IRequest<SampleTestResultDto>;

internal sealed class GetSampleTestResultQueryHandler(IResultDbContext context, ISampleIdFormatter sampleIdFormatter, IMethodsService methodsService)
    : IRequestHandler<GetSampleTestResultQuery, SampleTestResultDto>
{
    public async Task<SampleTestResultDto> Handle(GetSampleTestResultQuery request, CancellationToken cancellationToken)
    {
        var result = await context.SampleTestResults
            .Where(r => r.Id == request.TestResultId)
            .Select(r => new SampleTestResultDto
            {
                Id = r.Id,
                TestName = r.TestName,
                MethodName = r.MethodName,
                ApprovalStatus = r.ApprovalStatus,
                TotalDilution = r.PreDilutionFactor * r.InstrumentDilutionFactor,
                SampleIdDisplay = sampleIdFormatter.Format(r.RequestUid),
                Comment = r.OperatorComment,
                InstrumentDilution = r.InstrumentDilutionFactor,
                PreDilution = r.PreDilutionFactor,
                Replicates = r.Replicates!.Select(x => new SampleTestReplicateDto
                {
                    Id = x.Id,
                    ReplicateNumber = x.ReplicateNumber,
                    ResponseAsText = x.ResponseAsText,
                    ConcentrationAsText = x.ConcentrationAsText,
                    MeasurementDateTime = x.MeasurementDateTime,
                    IsAborted = x.IsAborted,
                    Active = x.Active,
                    HasError = x.TestReplicateErrors!.Any()
                }).ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            throw new KeyNotFoundException($"Sample test result with ID {request.TestResultId} not found.");
        }

        result.Unit = await methodsService.GetMethodTestConcentrationUnit(result.MethodName, result.TestName) ?? string.Empty;

        return result;
    }
}
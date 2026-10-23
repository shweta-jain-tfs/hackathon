using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries;

internal class GetSampleTestResultsQueryHandler(IResultDbContext dbContext, IMethodsService methodsService) : IRequestHandler<GetSampleTestResultsQuery, List<SampleTestResultDto>>
{
    public async Task<List<SampleTestResultDto>> Handle(GetSampleTestResultsQuery request, CancellationToken cancellationToken)
    {
        var uid = request.RequestUid;

        var items = await dbContext.SampleTestResults
            .Where(r => r.RequestUid.SampleId == uid.SampleId && r.RequestUid.RequestCreatedDate == uid.RequestCreatedDate)
            .Select(x => new SampleTestResultDto
            {
                TestOrderId = x.TestOrderId,
                MethodName = x.MethodName,
                TestName = x.TestName,
                MeanClass = x.MeanClass,
                AnalyticalRunId = x.AnalyticalRunId,
                MeanConcentrationsStatusOk = x.MeanConcentrationStatus == ConcentrationStatus.Ok,
                MeanConcentrationAsText = x.MeanConcentrationAsText,
                Created = x.Created,
                MeanQuotientAsText = x.MeanQuotientAsText,
                MeanCutOff = x.MeanCutOff,
                InstrumentNumber = x.InstrumentNumber
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        await AttachExtraInformation(items);

        return items;
    }

    private async Task AttachExtraInformation(List<SampleTestResultDto> items)
    {
        foreach (var result in items)
        {
            //Get Concentration unit 
            var unit = await methodsService.GetMethodTestConcentrationUnit(result.MethodName, result.TestName);

            //If ConcentrationStatus is OK, then add unit to MeanConcentrationAsText
            if (result.MeanConcentrationsStatusOk && unit is not null)
            {
                result.MeanConcentrationAsText = $"{result.MeanConcentrationAsText} {unit}";
            }
        }
    }
}
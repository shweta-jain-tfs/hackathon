using MediatR;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Result;

public record GetSampleTestResultsQuery(RequestUid RequestUid) : IRequest<List<SampleTestResultDto>>;

public class SampleTestResultDto
{
    public Guid TestOrderId { get; set; }
    public string MethodName { get; set; } = default!;
    public string TestName { get; set; } = default!;
    public string? MeanClass { get; set; }
    public string? MeanConcentrationAsText { get; set; }
    public bool MeanConcentrationsStatusOk { get; set; }
    public DateTimeOffset Created { get; set; }
    public int? AnalyticalRunId { get; set; }
    public string? MeanQuotientAsText { get; set; }
    public string? MeanCutOff { get; set; }
    public string InstrumentNumber { get; set; } = default!;
}
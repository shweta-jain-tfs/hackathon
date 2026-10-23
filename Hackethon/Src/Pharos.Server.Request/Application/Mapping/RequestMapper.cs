using Pharos.Server.Application.Instrument;
using Pharos.Server.Request.Application.Queries.GetRequestDetails;
using Pharos.Server.Request.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Request.Application.Mapping;

[Mapper]
internal partial class RequestMapper(IInstrumentManager instrumentManager)
{

    [MapProperty(nameof(Domain.Request.PatientAge), nameof(RequestDetailsDto.PatientAgeAtSampling))]
    [MapProperty(nameof(Domain.Request.TestOrders), nameof(RequestDetailsDto.TestOrders), Use = nameof(MapTestOrders))]
    [MapperIgnoreTarget(nameof(RequestDetailsDto.QueriedAt))]
    [MapperIgnoreTarget(nameof(RequestDetailsDto.SampleIdDisplay))]
    public partial RequestDetailsDto ToRequestDetailsDto(Domain.Request source);

    private List<RequestTestDto> MapTestOrders(IList<TestOrder> source)
    {
        return source.OrderBy(o => o.TestSortOrder).ThenBy(o => o.TestName).Select(testOrder =>
        {
            var result = TestOrderToRequestTestDto(testOrder);
            result.InstrumentName = instrumentManager.FindInstrument(testOrder.InstrumentNumber)?.DisplayName ?? testOrder.InstrumentNumber;
            return result;
        }).ToList();
    }

    [MapProperty(nameof(TestOrder.TestName), nameof(RequestTestDto.FullTestName))]
    [MapProperty(nameof(TestOrder.TestName), nameof(RequestTestDto.TestName))]
    [MapperIgnoreTarget(nameof(RequestTestDto.FullMethodName))]
    [MapperIgnoreTarget(nameof(RequestTestDto.Concentration))]
    [MapperIgnoreTarget(nameof(RequestTestDto.MeasurementDateTime))]
    [MapperIgnoreTarget(nameof(RequestTestDto.Class))]
    [MapperIgnoreTarget(nameof(RequestTestDto.AnalyticalRunId))]
    [MapperIgnoreTarget(nameof(RequestTestDto.MeanQuotientAsText))]
    [MapperIgnoreTarget(nameof(RequestTestDto.MeanCutOff))]
    [MapperIgnoreTarget(nameof(RequestTestDto.InstrumentName))]
    [MapperIgnoreTarget(nameof(RequestTestDto.IsReset))]
    [MapperIgnoreTarget(nameof(RequestTestDto.ReportClass))]
    [MapperIgnoreTarget(nameof(RequestTestDto.ReportConcentration))]
    [MapperIgnoreTarget(nameof(RequestTestDto.ReportCutOff))]
    [MapperIgnoreTarget(nameof(RequestTestDto.ReportQuotient))]
    private partial RequestTestDto TestOrderToRequestTestDto(TestOrder source);
}

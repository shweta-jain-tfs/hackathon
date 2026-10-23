using MediatR;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public record GetQualityControlInformationQuery(string InstrumentNumber, string MethodName, int LastDays) : IRequest<List<QualityControlInformationDto>>;

internal sealed class GetQualityControlInformationQueryHandler(IResultDbContext context, IDateTime dateTime) : IRequestHandler<GetQualityControlInformationQuery, List<QualityControlInformationDto>>
{
    public async Task<List<QualityControlInformationDto>> Handle(GetQualityControlInformationQuery request, CancellationToken cancellationToken)
    {
        var methodName = request.MethodName;
        var instrumentNumber = request.InstrumentNumber;
        var lastDays = request.LastDays;
        var measurementDate = dateTime.Now.Date.AddDays(-lastDays);

        var queryResult = await
            (from qc in context.QualityControlTestResults
            where qc.MethodName == methodName && qc.InstrumentNumber == instrumentNumber && qc.MeasurementDateTime >= measurementDate
            group qc by new
            {
                qc.QualityControlName,
                qc.TestName
            }
            into qcTypes
            select new QualityControlInformationDto
            {
                QualityControlName = qcTypes.Key.QualityControlName,
                TestName = qcTypes.Key.TestName
            }).ToListAsync(cancellationToken);

        return queryResult;
    }
}
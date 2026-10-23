using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;

internal record GetMethodAndInstrumentNameByAnalyticalRunQuery(int Id) : IRequest<MethodAndInstrumentNameDto>;

internal sealed class GetMethodAndInstrumentNameByAnalyticalRunQueryHandler(IResultDbContext context) : IRequestHandler<GetMethodAndInstrumentNameByAnalyticalRunQuery, MethodAndInstrumentNameDto>
{
    public async Task<MethodAndInstrumentNameDto> Handle(GetMethodAndInstrumentNameByAnalyticalRunQuery request, CancellationToken cancellationToken)
    {
        var ar = await context.AnalyticalRuns
            .Where(x => x.Id == request.Id)
            .Select(x => new { x.MethodName, x.InstrumentNumber })
            .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException();

        return new MethodAndInstrumentNameDto
        {
            MethodName = ar.MethodName,
            InstrumentNumber = ar.InstrumentNumber
        };
    }
}

//Helper class
public class MethodAndInstrumentNameDto
{
    public string MethodName { get; set; } = string.Empty;
    public string InstrumentNumber { get; set; } = string.Empty;
}
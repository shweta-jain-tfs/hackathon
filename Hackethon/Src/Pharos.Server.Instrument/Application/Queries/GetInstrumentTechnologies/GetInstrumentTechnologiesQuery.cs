using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentTechnologies;

public class InstrumentTechnologyDto
{
    public AssayTechnology Technology { get; set; }
    public bool IsSelected { get; set; }
}

internal record GetInstrumentTechnologiesQuery(int InstrumentId) : IRequest<List<InstrumentTechnologyDto>>;

internal sealed class GetInstrumentTechnologiesQueryHandler(
    IInstrumentDbContext dbContext,
    IMethodsService methodsService) : IRequestHandler<GetInstrumentTechnologiesQuery, List<InstrumentTechnologyDto>>
{
    public async Task<List<InstrumentTechnologyDto>> Handle(GetInstrumentTechnologiesQuery request, CancellationToken cancellationToken)
    {
        var instrumentExists = await dbContext.Instruments
            .AnyAsync(x => x.Id == request.InstrumentId, cancellationToken)
            .ConfigureAwait(false);

        if (!instrumentExists)
        {
            throw new NotFoundException();
        }

        var availableTechnologies = (await methodsService.GetMethods(
                m => m.Enabled,
                x => x.Technology))
            .ToHashSet();

        return await dbContext.InstrumentTechnologies
            .Where(x => x.InstrumentId == request.InstrumentId && availableTechnologies.Contains(x.Technology))
            .OrderBy(x => x.Technology)
            .Select(x => new InstrumentTechnologyDto
            {
                Technology = x.Technology,
                IsSelected = x.IsActive
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

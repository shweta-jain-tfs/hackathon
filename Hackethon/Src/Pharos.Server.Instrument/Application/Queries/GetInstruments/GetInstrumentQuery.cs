using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Mapping;

namespace Pharos.Server.Instrument.Application.Queries.GetInstruments;

internal record GetInstrumentQuery(int Id) : IRequest<InstrumentDto>;

internal class GetInstrumentQueryHandler(
    IInstrumentManager stateManager,
    IInstrumentDbContext dbContext,
    IMethodsService methodsService) : IRequestHandler<GetInstrumentQuery, InstrumentDto>
{
    public async Task<InstrumentDto> Handle(GetInstrumentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var instrument = (Domain.Instrument)stateManager.GetInstrument(request.Id);
            var dto = instrument.ToInstrumentDto();

            var availableTechnologies = (await methodsService.GetMethods(
                    m => m.Enabled,
                    x => x.Technology))
                .ToHashSet();

            dto.SelectedTechnologies = await dbContext.InstrumentTechnologies
                .Where(x => x.InstrumentId == request.Id && x.IsActive && availableTechnologies.Contains(x.Technology))
                .OrderBy(x => x.Technology)
                .Select(x => x.Technology)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto;
        }
        catch (KeyNotFoundException)
        {
            throw new NotFoundException();
        }
    }
}

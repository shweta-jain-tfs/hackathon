using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentMethods;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Commands.InitializeInstrumentTechnologies;

/// <summary>
/// Initializes the assay technologies that are available for an instrument.
/// </summary>
internal record InitializeInstrumentTechnologiesCommand(int InstrumentId) : IRequest;

internal sealed class InitializeInstrumentTechnologiesCommandHandler(
    IInstrumentDbContext dbContext,
    IMethodsService methodsService,
    ISender sender) : IRequestHandler<InitializeInstrumentTechnologiesCommand>
{
    public async Task Handle(InitializeInstrumentTechnologiesCommand request, CancellationToken cancellationToken)
    {
        var availableMethodTechnologies = await methodsService.GetMethods(
            m => true,
            x => x.Technology);

        var availableTechnologies = availableMethodTechnologies
            .Distinct()
            .ToHashSet();

        await InitializeTechnologies(request.InstrumentId, availableTechnologies, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await sender.Send(new InitializeInstrumentMethodsCommand(request.InstrumentId), cancellationToken);
    }

    private async Task InitializeTechnologies(int instrumentId, HashSet<AssayTechnology> availableTechnologies, CancellationToken cancellationToken)
    {
        var existingTechnologies = await dbContext.InstrumentTechnologies
            .Where(x => x.InstrumentId == instrumentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingTechnologySet = existingTechnologies
            .Select(x => x.Technology)
            .ToHashSet();

        var technologiesToAdd = availableTechnologies
            .Where(x => !existingTechnologySet.Contains(x))
            .Select(x => new InstrumentTechnology
            {
                InstrumentId = instrumentId,
                Technology = x,
                IsActive = true
            })
            .ToList();

        if (technologiesToAdd.Count > 0)
        {
            await dbContext.InstrumentTechnologies.AddRangeAsync(technologiesToAdd, cancellationToken);
        }
    }
}

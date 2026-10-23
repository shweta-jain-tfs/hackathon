using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentMethods;
using Pharos.Server.Instrument.Application.Commands.SendActiveMethods;
using Pharos.Server.Instrument.Application.Commands.UpdateMethods;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Localization;

namespace Pharos.Server.Instrument.Application.Commands.UpdateInstrumentTechnologies;

public class UpdateInstrumentTechnologyDto
{
    public AssayTechnology Technology { get; set; }
    public bool IsSelected { get; set; }
}

public class UpdateInstrumentTechnologiesCommand : IRequest
{
    [JsonIgnore]
    public int InstrumentId { get; set; }
    public List<UpdateInstrumentTechnologyDto> Technologies { get; set; } = [];
}

internal sealed class UpdateInstrumentTechnologiesCommandHandler(
    IInstrumentDbContext dbContext,
    ISender sender,
    ICurrentInstrumentServerHub currentInstrumentHub,
    IInstrumentHub instrumentHub,
    ILogger<UpdateInstrumentTechnologiesCommandHandler> logger) : IRequestHandler<UpdateInstrumentTechnologiesCommand>
{
    public async Task Handle(UpdateInstrumentTechnologiesCommand request, CancellationToken cancellationToken)
    {
        var instrument = await dbContext.Instruments
            .Where(x => x.Id == request.InstrumentId)
            .Select(x => new { x.Id, x.SerialNumber })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (instrument == null)
        {
            throw new ObjectGoneException(Translations.InstrumentDeleted);
        }

        var submittedTechnologies = request.Technologies
            .Select(x => x.Technology)
            .ToList();

        if (submittedTechnologies.Count != submittedTechnologies.Distinct().Count())
        {
            throw new ValidationException("Duplicate technologies are not allowed.");
        }

        var existingTechnologies = await dbContext.InstrumentTechnologies
            .Where(x => x.InstrumentId == request.InstrumentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingTechnologiesByTechnology = existingTechnologies.ToDictionary(x => x.Technology);
        var invalidTechnologies = submittedTechnologies
            .Where(x => !existingTechnologiesByTechnology.ContainsKey(x))
            .ToList();

        if (invalidTechnologies.Count > 0)
        {
            throw new ValidationException("Only available technologies can be updated.");
        }

        foreach (var technology in existingTechnologies)
        {
            technology.IsActive = request.Technologies
                .FirstOrDefault(x => x.Technology == technology.Technology)
                ?.IsSelected ?? false;
        }

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await sender.Send(new InitializeInstrumentMethodsCommand(request.InstrumentId), cancellationToken);

        try
        {
            await sender.Send(new UpdateMethodsCommand(instrument.SerialNumber), cancellationToken);
            await sender.Send(new SendActiveMethodsCommand(instrument.SerialNumber), cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating technologies on instrument {Instrument}", instrument.SerialNumber);
        }

        await currentInstrumentHub.InstrumentMethodsChanged(request.InstrumentId);
        await instrumentHub.InstrumentTechnologiesChanged(request.InstrumentId);
    }
}

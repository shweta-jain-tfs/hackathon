using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Localization;

namespace Pharos.Server.Instrument.Application.Commands.UpdateInstrument;

public class UpdateInstrumentCommand : IRequest
{
    [JsonIgnore]
    public int Id { get; set; }
    public required string DisplayName { get; set; }
    public bool Enabled { get; set; }
}

internal sealed class UpdateInstrumentCommandHandler(
    IInstrumentManager instrumentManager,
    IInstrumentDbContext dbInstrumentContext,
    ICommunicationSessionManager sessionManager) : IRequestHandler<UpdateInstrumentCommand>
{
    public async Task Handle(UpdateInstrumentCommand request, CancellationToken cancellationToken)
    {
        var instrumentInDb = await dbInstrumentContext.Instruments
            .Where(s => s.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (instrumentInDb == null)
        {
            throw new ObjectGoneException(Translations.InstrumentDeleted);
        }

        var instrumentInMemory = (Domain.Instrument)instrumentManager.GetInstrument(request.Id);

        var enabledUpdated = instrumentInDb.Enabled != request.Enabled;

        if (enabledUpdated)
        {
            // Cant disable instrument when in assay mode. (Instrument status is only saved in memory).
            if (!request.Enabled && instrumentInMemory.IsInAssayMode)
            {
                throw new ValidationException("This instrument is in assay mode and can not be disabled. Instrument name: " + request.DisplayName);
            }

            if (!request.Enabled)
            {
                // stop communication session if any first
                var session = sessionManager.GetSession(instrumentInDb.SerialNumber);
                if (session != null)
                {
                    await sessionManager.StopSessionAsync(session.SessionId);
                }
            }

            instrumentInDb.Enabled = request.Enabled;

            instrumentInDb.AddDomainEvent(new InstrumentEnabledChangedEvent(instrumentInDb.SerialNumber, instrumentInDb.Enabled));
        }

        var nameUpdated = instrumentInDb.DisplayName != request.DisplayName;
        if (nameUpdated)
        {
            // Check if the display name already exists on another instrument.
            var allOtherInstrumentNames = await dbInstrumentContext.Instruments
                .Where(x => x.DisplayName != instrumentInDb.DisplayName)
                .Select(s => s.DisplayName)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (allOtherInstrumentNames.Any(name => string.Equals(name, request.DisplayName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationException("The instrument name already exists on another instrument. Instrument name: " + request.DisplayName);
            }

            instrumentInDb.DisplayName = request.DisplayName;
        }

        if (dbInstrumentContext.HasChanges())
        {
            if (enabledUpdated || nameUpdated)
            {
                instrumentInDb.AddDomainEvent(new InstrumentUpdatedEvent(instrumentInDb, nameUpdated));
            }

            await dbInstrumentContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // update in-memory instrument instance after saving to db, but before handling events
            if (enabledUpdated)
            {
                instrumentInMemory.Enabled = request.Enabled;
            }
            if (nameUpdated)
            {
                instrumentInMemory.DisplayName = request.DisplayName;
            }
        }
    }
}
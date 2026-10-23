using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Services;
using Pharos.Server.Localization;

namespace Pharos.Server.Instrument.Application.Commands.DeleteInstrument;

internal record DeleteInstrumentCommand(int Id) : IRequest;

internal sealed class DeleteInstrumentCommandHandler(
    IInstrumentDbContext dbContext,
    ICommunicationSessionManager sessionManager,
    InstrumentManager instrumentManager) : IRequestHandler<DeleteInstrumentCommand>
{
    public async Task Handle(DeleteInstrumentCommand instrumentCommand, CancellationToken cancellationToken)
    {
        var instrument = await dbContext.Instruments
            .Where(i => i.Id == instrumentCommand.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (instrument == null)
        {
            throw new ObjectGoneException(Translations.InstrumentDeleted);
        }

        // Prevent deletion if instrument is currently connected to Pharos
        var session = sessionManager.GetSession(instrument.SerialNumber);
        if (session != null)
        {
            throw new NotAllowedException(Translations.InstrumentConnectedCannotBeDeleted);
        }

        // Remove instrument from memory, before remove it from db.
        instrumentManager.RemoveInstrument(instrument.SerialNumber);

        dbContext.Instruments.Remove(instrument);
        instrument.AddDomainEvent(new InstrumentDeletedEvent(instrument.SerialNumber, instrument.Id));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    }
}
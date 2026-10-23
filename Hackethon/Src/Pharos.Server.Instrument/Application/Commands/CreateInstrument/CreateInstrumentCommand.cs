using System.Net;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentTechnologies;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Services;

namespace Pharos.Server.Instrument.Application.Commands.CreateInstrument;

internal record CreateInstrumentCommand(IPAddress IpAddress, string InstrumentNumber, string? InstrumentSoftwareVersion = null) : IRequest<Domain.Instrument>;

internal class CreateInstrumentCommandHandler(IInstrumentDbContext dbContext, ISender sender, InstrumentManager instrumentManager) : IRequestHandler<CreateInstrumentCommand, Domain.Instrument>
{
    public async Task<Domain.Instrument> Handle(CreateInstrumentCommand request, CancellationToken cancellationToken)
    {
        var instrument = new Domain.Instrument
        {
            DisplayName = request.InstrumentNumber,
            IpAddress = request.IpAddress,
            SerialNumber = request.InstrumentNumber,
            InstrumentSoftwareVersion = request.InstrumentSoftwareVersion,
        };

        dbContext.Instruments.Add(instrument);
        instrument.AddDomainEvent(new InstrumentCreatedEvent(instrument.SerialNumber));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await sender.Send(new InitializeInstrumentTechnologiesCommand(instrument.Id), cancellationToken);
        instrumentManager.AddInstrument(instrument);
        return instrument;
    }
}

using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentTechnologies;
using Pharos.Server.Instrument.Application.Services;
using Pharos.Server.Instrument.Infrastructure.Grpc;

namespace Pharos.Server.Instrument.Infrastructure.Database;

internal class InstrumentDbContextInitializer(
    InstrumentDbContext context,
    InstrumentManager instrumentManager,
    ISender sender,
    IDbEncryptionService dbEncryptionService,
    IOptions<GrpcSettings> options) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => InstrumentDbContext.Schema;

    public override async Task SeedAsync()
    {
        var hasInstruments = await context.Instruments.AsQueryable().AnyAsync().ConfigureAwait(false);

        if (!hasInstruments && !options.Value.MainService.RequireClientCertificate)
        {
            var instrument = new Domain.Instrument
            {
                DisplayName = "Billy",
                InstrumentType = InstrumentType.Medium,
                IpAddress = IPAddress.Loopback,
                SerialNumber = "SEED_001",
            };
            context.Instruments.Add(instrument);

            // Add a domain event to notify that the SEEDED instrument has been created.
            instrument.AddDomainEvent(new Pharos.Server.Application.Instrument.InstrumentCreatedEvent(instrument.SerialNumber));

            await context.SaveChangesAsync().ConfigureAwait(false);

            instrumentManager.AddInstrument(instrument);
            await sender.Send(new InitializeInstrumentTechnologiesCommand(instrument.Id)).ConfigureAwait(false);
        }
    }
}

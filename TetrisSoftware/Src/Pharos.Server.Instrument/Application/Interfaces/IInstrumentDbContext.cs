using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Interfaces;

internal interface IInstrumentDbContext : IDbContext
{
    public DbSet<Domain.Instrument> Instruments { get; }
    public DbSet<InstrumentMethod> InstrumentMethods { get; }
    public DbSet<InstrumentTechnology> InstrumentTechnologies { get; }
    public DbSet<AssayScheduler> AssaySchedulers { get; }
    public DbSet<AssaySchedulerDay> AssaySchedulerDays { get; }
    public DbSet<AssaySchedulerMethod> AssaySchedulerMethods { get; }
}

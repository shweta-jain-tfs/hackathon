using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Infrastructure.Database;
internal class InstrumentDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor? auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<InstrumentDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), IInstrumentDbContext
{
    public const string Schema = "Instrument";

    protected override string DefaultSchema => Schema;

    public DbSet<Domain.Instrument> Instruments => Set<Domain.Instrument>();
    public DbSet<InstrumentMethod> InstrumentMethods => Set<InstrumentMethod>();
    public DbSet<InstrumentTechnology> InstrumentTechnologies => Set<InstrumentTechnology>();
    public DbSet<AssayScheduler> AssaySchedulers => Set<AssayScheduler>();
    public DbSet<AssaySchedulerDay> AssaySchedulerDays => Set<AssaySchedulerDay>();
    public DbSet<AssaySchedulerMethod> AssaySchedulerMethods => Set<AssaySchedulerMethod>();
}

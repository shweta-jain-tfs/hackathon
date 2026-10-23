using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Interfaces;

internal interface ISampleDbContext : IDbContext
{
    public DbSet<SampleTube> SampleTubes { get; }
    public DbSet<SampleTray> SampleTrays { get; }
}
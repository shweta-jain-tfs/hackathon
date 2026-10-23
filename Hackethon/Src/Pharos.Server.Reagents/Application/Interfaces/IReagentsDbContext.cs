using Pharos.Server.Reagents.Domain;

namespace Pharos.Server.Reagents.Application.Interfaces;

internal interface IReagentsDbContext : IDbContext
{
    public DbSet<Reagent> Reagents { get; }
    public DbSet<ReagentContent> ReagentContents { get; }
    public DbSet<Lot> Lots { get; }
}
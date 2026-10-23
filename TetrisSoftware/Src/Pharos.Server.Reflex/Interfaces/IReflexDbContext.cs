using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Domain;

namespace Pharos.Server.Reflex.Interfaces;

internal interface IReflexDbContext : IDbContext
{
    public DbSet<ReflexDefinition> ReflexDefinitions { get; }
    public DbSet<ReflexTest> ReflexTests { get; }
}
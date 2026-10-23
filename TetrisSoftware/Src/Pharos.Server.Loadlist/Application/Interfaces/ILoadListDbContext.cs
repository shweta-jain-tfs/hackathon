using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Interfaces;

internal interface ILoadListDbContext : IDbContext
{
    public DbSet<LoadListConfiguration> LoadListConfigurations { get; }
    public DbSet<LastLoadedReagent> LastLoadedReagents { get; }
}
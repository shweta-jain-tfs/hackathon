using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Maintenance.Domain;

namespace Pharos.Server.Maintenance.Application.Interfaces;

public interface IMaintenanceDbContext : IDbContext
{
    public DbSet<MaintenanceActivity> Activities { get; }
}
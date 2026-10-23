using Microsoft.EntityFrameworkCore;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Identity.Application.Interfaces;

internal interface IIdentityDbContext : IDbContext
{
    public DbSet<ApplicationUser> ApplicationUsers { get; }
    public DbSet<ApplicationRole> ApplicationRoles { get; }
}

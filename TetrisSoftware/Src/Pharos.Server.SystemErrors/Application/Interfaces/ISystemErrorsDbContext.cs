using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Application.Interfaces;

internal interface ISystemErrorsDbContext : IDbContext
{
    public DbSet<SystemError> SystemErrors { get; }
}
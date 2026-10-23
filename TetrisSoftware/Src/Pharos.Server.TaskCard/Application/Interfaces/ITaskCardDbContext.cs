using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.TaskCard.Application.Interfaces;

internal interface ITaskCardDbContext : IDbContext
{
    DbSet<Domain.TaskCard> TaskCards { get; }
}
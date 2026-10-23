using Microsoft.EntityFrameworkCore.Storage;

namespace Pharos.Server.Infrastructure.Database;

public interface IDbContext
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    bool HasChanges();
}
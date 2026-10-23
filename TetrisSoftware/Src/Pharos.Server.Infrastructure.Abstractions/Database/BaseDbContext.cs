using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Infrastructure.Database;

public abstract class BaseDbContext<TDbContext> : DbContext where TDbContext : DbContext, IDbContext
{
    private readonly IAuditableEntitySaveChangesInterceptor? _auditableEntitySaveChangesInterceptor;
    private readonly string? _connectionString;

    protected BaseDbContext(IConfiguration configuration,
        IAuditableEntitySaveChangesInterceptor? auditableEntitySaveChangesInterceptor,
        IMediator? mediator)
    {
        Mediator = mediator;
        _auditableEntitySaveChangesInterceptor = auditableEntitySaveChangesInterceptor;
        _connectionString = configuration.GetConnectionString();
    }

    public IMediator? Mediator { get; set; }

    protected abstract string DefaultSchema { get; }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.CommitTransactionAsync(cancellationToken);
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.RollbackTransactionAsync(cancellationToken);
    }

    public bool HasChanges()
    {
        return ChangeTracker.HasChanges();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            optionsBuilder.UseInMemoryDatabase(nameof(TDbContext));
        }
        else
        {
            optionsBuilder.UseSqlServer(
                _connectionString,
                o =>
                {
                    o.MigrationsAssembly(GetType().Assembly.FullName);
                    o.MigrationsHistoryTable(
                        tableName: HistoryRepository.DefaultTableName,
                        schema: DefaultSchema);
                });
        }

        if (_auditableEntitySaveChangesInterceptor != null)
        {
            optionsBuilder.AddInterceptors(_auditableEntitySaveChangesInterceptor);
        }

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (!string.IsNullOrEmpty(_connectionString))
        {
            modelBuilder.HasDefaultSchema(DefaultSchema);
        }
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = GetDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        await DispatchDomainEvents(domainEvents);

        return result;
    }

    private List<INotification> GetDomainEvents()
    {
        var entities = ChangeTracker
                       .Entries<BaseEntityWithoutId>()
                       .Where(e => e.Entity.DomainEvents.Count > 0)
                       .Select(e => e.Entity)
                       .ToArray();

        var domainEvents = entities
                           .SelectMany(e => e.DomainEvents)
                           .ToList();

        entities.ToList().ForEach(e => e.ClearDomainEvents());

        return domainEvents;
    }

    private async Task DispatchDomainEvents(List<INotification> domainEvents)
    {
        if (Mediator == null)
        {
            return;
        }

        foreach (var domainEvent in domainEvents)
        {
            await Mediator.Publish(domainEvent);
        }
    }
}
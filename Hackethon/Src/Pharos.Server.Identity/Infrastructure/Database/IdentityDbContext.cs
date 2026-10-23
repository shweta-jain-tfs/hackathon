using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Server.Identity.Application.Interfaces;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Identity.Infrastructure.Database;

internal sealed class IdentityDbContext(
    IHostEnvironment environment,
    ILoggerFactory loggerFactory,
    DbContextOptions<IdentityDbContext> options,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IConfiguration configuration)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IIdentityDbContext
{
    public const string Schema = "Identity";

    private readonly string? _connectionString = configuration.GetConnectionString();

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<ApplicationRole> ApplicationRoles => Set<ApplicationRole>();

    /// <summary>
    /// Configures the schema needed for the identity framework.
    /// </summary>
    /// <param name="builder">
    /// The builder being used to construct the model for this context.
    /// </param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        if (!string.IsNullOrEmpty(_connectionString))
        {
            builder.HasDefaultSchema(Schema);
        }

        base.OnModelCreating(builder);

        builder.Ignore<IdentityUserLogin<Guid>>();
        builder.Ignore<IdentityUserToken<Guid>>();

        builder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!string.IsNullOrEmpty(_connectionString))
        {
            optionsBuilder.UseSqlServer(
                _connectionString,
                o =>
                {
                    o.MigrationsAssembly(GetType().Assembly.FullName);
                    o.MigrationsHistoryTable(
                        tableName: HistoryRepository.DefaultTableName,
                        schema: Schema);
                });
        }

        optionsBuilder.ConfigureWarnings(warnings =>
        {
            warnings.Ignore(CoreEventId.MappedEntityTypeIgnoredWarning);
            warnings.Ignore(CoreEventId.MappedPropertyIgnoredWarning);
        });

        optionsBuilder.UseLoggerFactory(loggerFactory);

        if (environment.IsDevelopment())
        {
            // enable logging of SQL and other sensitive information
            // https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/extensions-logging
            optionsBuilder.EnableSensitiveDataLogging();
        }

        optionsBuilder.AddInterceptors(auditableEntitySaveChangesInterceptor);

        base.OnConfiguring(optionsBuilder);
    }

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
}
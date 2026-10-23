using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Infrastructure.Database;

internal class SettingsDbContext : BaseDbContext<SettingsDbContext>, ISettingsDbContext
{
    public const string Schema = "Settings";
    public SettingsDbContext(
        IConfiguration configuration) : base(configuration, null, null)
    {
    }

    public SettingsDbContext(
        IConfiguration configuration,
        IAuditableEntitySaveChangesInterceptor? auditableEntitySaveChangesInterceptor,
        IMediator? mediator) : base(configuration, auditableEntitySaveChangesInterceptor, mediator)
    {
    }

    protected override string DefaultSchema => Schema;

    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        var settingsSavedInterceptor = EntityConfigurationProvider.Instance;
        if (settingsSavedInterceptor != null)
        {
            optionsBuilder.AddInterceptors(settingsSavedInterceptor);
        }
    }
}

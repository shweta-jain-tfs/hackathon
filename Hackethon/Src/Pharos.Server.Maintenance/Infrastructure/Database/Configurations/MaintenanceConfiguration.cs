using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Maintenance.Domain;

namespace Pharos.Server.Maintenance.Infrastructure.Database.Configurations;

internal class MaintenanceConfiguration : BaseEntityConfiguration<MaintenanceActivity>
{
    public override void Configure(EntityTypeBuilder<MaintenanceActivity> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => x.MaintenanceId).IsUnique();
    }
}
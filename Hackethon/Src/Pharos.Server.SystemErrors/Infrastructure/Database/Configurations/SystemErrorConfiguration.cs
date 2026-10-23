using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Infrastructure.Database.Configurations;

internal sealed class SystemErrorConfiguration : BaseEntityConfiguration<SystemError>
{
    public override void Configure(EntityTypeBuilder<SystemError> builder)
    {
        base.Configure(builder);

        builder.HasIndex(i => i.ErrorId).IsUnique();
        builder.HasIndex(i => i.SerialNumber);
    }
}
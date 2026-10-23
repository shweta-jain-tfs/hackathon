using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Infrastructure.Database;

public abstract class BaseAuditableEntityConfiguration<TEntity> : BaseEntityConfiguration<TEntity> where TEntity : BaseAuditableEntity
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Created)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasConversion<string>();

        builder.Property(x => x.LastModified);

        builder.Property(x => x.LastModifiedBy)
            .HasConversion<string>();
    }
}
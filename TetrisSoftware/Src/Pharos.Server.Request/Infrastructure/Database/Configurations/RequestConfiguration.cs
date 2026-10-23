using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Request.Infrastructure.Database.Configurations;

internal sealed class RequestConfiguration : BaseAuditableEntityConfiguration<Domain.Request>
{
    public override void Configure(EntityTypeBuilder<Domain.Request> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.SampleId)
               .IsRequired();

        // Foreign keys
        builder.HasOne(x => x.Patient)
               .WithMany(y => y.Requests);

        // Indexes
        builder.HasIndex(x => x.SampleId);
        builder.HasIndex(x => x.RequestId);
    }
}
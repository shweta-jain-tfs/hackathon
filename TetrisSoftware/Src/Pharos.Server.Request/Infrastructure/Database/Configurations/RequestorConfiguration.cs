using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Infrastructure.Database.Configurations;

internal sealed class RequestorConfiguration : BaseAuditableEntityConfiguration<Requestor>
{
    public override void Configure(EntityTypeBuilder<Requestor> builder)
    {
        base.Configure(builder);

        builder.Property(p => p.Name).HasMaxLength(75);

        builder.Property(p => p.RequestorId)
            .IsRequired();

        builder.Property(p => p.NumberOfReportedSamples)
            .IsRequired();

        builder.Property(p => p.NumberOfReportedTests)
            .IsRequired();

        // Foreign keys
        builder.HasMany(x => x.Requests)
            .WithOne(x => x.Requestor);
    }
}
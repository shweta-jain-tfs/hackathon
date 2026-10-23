using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Infrastructure.Database.Configurations;

internal sealed class TestOrderConfiguration : BaseAuditableEntityConfiguration<TestOrder>
{
    public override void Configure(EntityTypeBuilder<TestOrder> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => x.Uid).IsUnique();

        builder.HasOne(o => o.Request)
               .WithMany(s => s.TestOrders);

        builder.HasMany(o => o.ReflexDefinitions)
            .WithOne()
            .HasPrincipalKey(x => x.Uid)
            .HasForeignKey(x => x.TestOrderId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasIndex(x => x.State);
        builder.HasIndex(x => new { x.RequestId, x.TestSortOrder });
        builder.HasIndex(x => x.InstrumentNumber);
    }
}
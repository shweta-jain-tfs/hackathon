using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;
internal sealed class TestReplicateReagentConfiguration : IEntityTypeConfiguration<TestReplicateReagent>
{
    public void Configure(EntityTypeBuilder<TestReplicateReagent> builder)
    {
        builder.HasKey(x => new { x.TestReplicateId, x.Barcode, x.SerialNumber, x.CellPosition, x.LotNumber, x.Expired, x.ReagentType })
            .IsClustered();

        builder.HasIndex(x => new { x.Barcode, x.SerialNumber, x.CellPosition });

        // Foreign keys
        builder.HasOne<TestReplicate>()
            .WithMany(y => y.TestReplicateReagents)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

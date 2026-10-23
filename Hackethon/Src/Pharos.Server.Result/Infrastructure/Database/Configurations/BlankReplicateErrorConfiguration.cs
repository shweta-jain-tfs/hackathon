using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;
internal sealed class BlankReplicateErrorConfiguration : IEntityTypeConfiguration<BlankReplicateError>
{
    public void Configure(EntityTypeBuilder<BlankReplicateError> builder)
    {
        // Foreign keys
        builder.HasOne<BlankReplicate>()
            .WithMany(y => y.BlankReplicateErrors)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasKey(x => new { x.BlankReplicateId, x.ErrorCode }).IsClustered();
    }
}

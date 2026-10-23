using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;
internal sealed class TestReplicateErrorConfiguration : IEntityTypeConfiguration<TestReplicateError>
{
    public void Configure(EntityTypeBuilder<TestReplicateError> builder)
    {
        builder.HasKey(x => new
        {
            x.TestReplicateId,
            x.ErrorCode
        });

        // Foreign keys
        builder.HasOne<TestReplicate>()
            .WithMany(y => y.TestReplicateErrors)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

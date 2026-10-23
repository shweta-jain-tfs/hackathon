using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Infrastructure.Database.Configurations;

internal sealed class RequestIssueConfiguration : BaseEntityConfiguration<RequestIssue>
{
    public override void Configure(EntityTypeBuilder<RequestIssue> builder)
    {
        base.Configure(builder);

        // Foreign keys
        builder.HasOne(x => x.Request)
            .WithMany(y => y.Issues)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodConfiguration : BaseAuditableEntityConfiguration<Method>
{
    public override void Configure(EntityTypeBuilder<Method> builder)
    {
        base.Configure(builder);

        builder.OwnsOne(x => x.AnalyticalRun);
        builder.OwnsOne(x => x.Calculation);
        builder.OwnsOne(x => x.Presentation);
        builder.OwnsOne(x => x.Reporting);

        // Indexes
        builder.HasIndex(x => x.MethodName)
               .IsUnique();

        builder.HasIndex(x => x.MethodNameLis)
               .IsUnique();

        builder.HasIndex(x => x.SortOrder);
    }
}
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodTestConfiguration : BaseAuditableEntityConfiguration<MethodTest>
{
    public override void Configure(EntityTypeBuilder<MethodTest> builder)
    {
        base.Configure(builder);

        builder.OwnsOne(x => x.Priority);

        // Foreign keys
        builder.HasOne(x => x.Method)
               .WithMany(y => y.Tests)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DefaultConcentrationUnit)
               .WithMany(y => y.DefaultMethodTests)
               .HasForeignKey(x => new { x.MethodId, x.DefaultMethodConcentrationUnitId })
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.UserConcentrationUnit)
               .WithMany(y => y.UserMethodTests)
               .HasForeignKey(x => new { x.MethodId, x.UserMethodConcentrationUnitId })
               .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(x => new
        {
            x.MethodId,
            x.TestName
        }).IsUnique();

        builder.HasIndex(x => x.TestName);

        builder.Property(x => x.TestNameNaturalSortKey)
            .HasComputedColumnSql($"dbo.NaturalSortKey({nameof(MethodTest.TestName)})");

        // Disable SQL OUTPUT for this table because TestNameNaturalSortKey is a computed column
        // using dbo.NaturalSortKey(...), which SQL Server cannot project in OUTPUT INSERTED.
        builder.ToTable(t => t.UseSqlOutputClause(false));
    }
}
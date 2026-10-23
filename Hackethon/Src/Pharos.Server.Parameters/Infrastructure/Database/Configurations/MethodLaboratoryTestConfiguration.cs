using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodLaboratoryTestConfiguration : BaseAuditableEntityConfiguration<MethodTestLabName>
{
    public override void Configure(EntityTypeBuilder<MethodTestLabName> builder)
    {
        base.Configure(builder);

        // Foreign keys
        builder.HasOne(x => x.MethodTest)
               .WithMany(y => y.LabTestNames)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => new
        {
            x.MethodTestId,
            x.TestName
        }).IsUnique();

        builder.Property(x => x.TestNameNaturalSortKey)
            .HasComputedColumnSql($"dbo.NaturalSortKey({nameof(MethodTestLabName.TestName)})");

        // Disable SQL OUTPUT for this table because TestNameNaturalSortKey is a computed column
        // using dbo.NaturalSortKey(...), which SQL Server cannot project in OUTPUT INSERTED.
        builder.ToTable(t => t.UseSqlOutputClause(false));
    }
}
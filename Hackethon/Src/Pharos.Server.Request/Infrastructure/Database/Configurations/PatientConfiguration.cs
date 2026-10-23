using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Infrastructure.Database.Configurations;

internal sealed class PatientConfiguration : BaseAuditableEntityConfiguration<Patient>
{
    public override void Configure(EntityTypeBuilder<Patient> builder)
    {
        base.Configure(builder);

        builder.Property(p => p.Gender)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(p => p.Name).HasMaxLength(75);

        builder.Property(p => p.BirthDate);

        builder.Property(p => p.PatientNumber);

        builder.Property(p => p.MedicalRecordNumber);

        builder.Property(p => p.PatientId).HasMaxLength(75);

        builder.Property(p => p.Diagnosis);

        builder.Property(p => p.Address);

        builder.Property(p => p.ZipCode);

        builder.Property(p => p.Comment);
    }
}
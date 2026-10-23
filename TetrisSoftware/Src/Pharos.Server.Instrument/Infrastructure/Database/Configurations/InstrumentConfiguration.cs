using System.Net;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Instrument.Infrastructure.Database.Configurations;

internal sealed class InstrumentConfiguration : BaseAuditableEntityConfiguration<Instrument.Domain.Instrument>
{
    public override void Configure(EntityTypeBuilder<Instrument.Domain.Instrument> builder)
    {
        base.Configure(builder);

        builder.Property(i => i.InstrumentType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(i => i.SerialNumber)
            .IsRequired();

        builder.Property(i => i.DisplayName);

        builder.Property(i => i.IpAddress)
            .HasConversion(
                v => v.ToString(),
                v => IPAddress.Parse(v))
            .HasMaxLength(45); // the maximum value for IPv6 address string

        builder.HasIndex(i => i.SerialNumber).IsUnique();
    }
}
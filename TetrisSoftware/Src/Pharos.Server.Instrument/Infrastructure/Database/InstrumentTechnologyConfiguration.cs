using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Infrastructure.Database;

internal class InstrumentTechnologyConfiguration : IEntityTypeConfiguration<InstrumentTechnology>
{
    public void Configure(EntityTypeBuilder<InstrumentTechnology> builder)
    {
        builder
            .HasIndex(x => new { x.InstrumentId, x.Technology })
            .IsUnique();
    }
}

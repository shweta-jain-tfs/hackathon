using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Domain;

namespace Pharos.Server.TaskCard.Infrastructure.Database.Configurations;

internal class TaskCardConfiguration : IEntityTypeConfiguration<Domain.TaskCard>
{
    public void Configure(EntityTypeBuilder<Domain.TaskCard> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExpiresAt)
            .IsRequired(false);

        builder.Property(x => x.InstrumentNumber)
            .HasMaxLength(Constants.InstrumentSerialNumberLength)
            .IsRequired(false);

        builder.Property(x => x.CardType)
            .IsRequired();

        builder.HasMany(x => x.Rules)
            .WithOne()
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
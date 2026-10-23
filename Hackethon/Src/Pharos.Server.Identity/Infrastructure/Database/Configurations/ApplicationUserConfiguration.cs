using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Identity.Domain;

namespace Pharos.Server.Identity.Infrastructure.Database.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Ignore(x => x.Email);
        builder.Ignore(x => x.EmailConfirmed);
        builder.Ignore(x => x.NormalizedEmail);
        builder.Ignore(x => x.PhoneNumber);
        builder.Ignore(x => x.PhoneNumberConfirmed);

        builder.Property(x => x.PasswordHash).HasMaxLength(512);
        builder.Property(x => x.SecurityStamp).HasMaxLength(512);
        builder.Property(x => x.ConcurrencyStamp).HasMaxLength(512);

        // Note: Cannot enforce uniqueness at DB level since PINs are hashed with unique salts
        // Uniqueness is enforced at application level in ApplicationUserManager
        builder.HasIndex(x => x.PinHash);
    }
}
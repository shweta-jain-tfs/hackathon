using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Identity.Domain;

namespace Pharos.Server.Identity.Infrastructure.Database.Configurations;

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(x => x.ConcurrencyStamp).HasMaxLength(512);
    }
}
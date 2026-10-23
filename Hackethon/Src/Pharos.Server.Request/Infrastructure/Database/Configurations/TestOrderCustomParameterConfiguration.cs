using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Infrastructure.Database.Configurations;

internal sealed class TestOrderCustomParameterConfiguration : IEntityTypeConfiguration<TestOrderCustomParameter>
{
    public void Configure(EntityTypeBuilder<TestOrderCustomParameter> builder)
    {
        builder.HasOne(o => o.TestOrder);
    }
}

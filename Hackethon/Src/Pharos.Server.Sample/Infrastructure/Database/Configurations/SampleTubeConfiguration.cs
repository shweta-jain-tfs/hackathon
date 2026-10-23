using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Infrastructure.Database.Configurations;
internal sealed class SampleTubeConfiguration : BaseEntityConfiguration<SampleTube>
{
    public override void Configure(EntityTypeBuilder<SampleTube> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => x.TubeId);
        builder.HasIndex(x => new { x.SampleId, x.RequestCreatedDate });
        builder.HasIndex(x => x.TrayId);
    }
}

using FluentAssertions;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.HL7.Application.Mappings;
using Xunit;

namespace Pharos.Server.Lis.HL7.Tests.Application.Mappings;

public class HL7SpecimenContainerSegmentFactoryTests
{
    [Fact]
    public void CreateFromSample_ShouldUseFirstTestOrderWithTrayOrTubePosition()
    {
        var sample = new LisSample
        {
            SampleId = "SAMPLE-001",
            TestOrders =
            [
                new LisTestOrder(),
                new LisTestOrder { TrayId = "TRAY01", TubePosition = 3 },
                new LisTestOrder { TrayId = "TRAY02", TubePosition = 4 }
            ]
        };

        var segment = HL7SpecimenContainerSegmentFactory.CreateFromSample(sample, new HL7ExportSettings());

        segment.SampleId.Should().Be("SAMPLE-001");
        segment.TrayId.Should().Be("TRAY01");
        segment.TrayPosition.Should().Be(3);
    }

    [Trait("ProductRequirement", "PR_576")]
    [Fact]
    public void CreateFromSample_WhenLocalSampleAndUseLocalSampleIdAsLisSampleIdIsFalse_ShouldUseEmptyContainerIdentifier()
    {
        var sample = CreateSample(isSampleLocallyCreated: true);
        var exportSettings = new HL7ExportSettings
        {
            UseLocalSampleIdAsLisSampleId = false
        };

        var segment = HL7SpecimenContainerSegmentFactory.CreateFromSample(sample, exportSettings);

        segment.SampleId.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_576")]
    [Fact]
    public void CreateFromSample_WhenLocalSampleAndUseLocalSampleIdAsLisSampleIdIsTrue_ShouldUseLocalSampleIdAsContainerIdentifier()
    {
        var sample = CreateSample(isSampleLocallyCreated: true);
        var exportSettings = new HL7ExportSettings
        {
            UseLocalSampleIdAsLisSampleId = true
        };

        var segment = HL7SpecimenContainerSegmentFactory.CreateFromSample(sample, exportSettings);

        segment.SampleId.Should().Be("SAMPLE-001");
    }

    [Trait("ProductRequirement", "PR_576")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateFromSample_WhenSampleIsFromLis_ShouldUseSampleIdAsContainerIdentifier(bool useLocalSampleIdAsLisSampleId)
    {
        var sample = CreateSample(isSampleLocallyCreated: false);
        var exportSettings = new HL7ExportSettings
        {
            UseLocalSampleIdAsLisSampleId = useLocalSampleIdAsLisSampleId
        };

        var segment = HL7SpecimenContainerSegmentFactory.CreateFromSample(sample, exportSettings);

        segment.SampleId.Should().Be("SAMPLE-001");
    }

    [Trait("ProductRequirement", "PR_576")]
    [Fact]
    public void CreateFromSample_WhenLocalSampleAndUseLocalSampleIdAsLisSampleIdIsFalse_ShouldKeepTrayAndTubePosition()
    {
        var sample = CreateSample(isSampleLocallyCreated: true);
        var exportSettings = new HL7ExportSettings
        {
            UseLocalSampleIdAsLisSampleId = false
        };

        var segment = HL7SpecimenContainerSegmentFactory.CreateFromSample(sample, exportSettings);

        segment.TrayId.Should().Be("TRAY01");
        segment.TrayPosition.Should().Be(3);
    }

    private static LisSample CreateSample(bool isSampleLocallyCreated)
    {
        return new LisSample
        {
            SampleId = "SAMPLE-001",
            IsSampleLocallyCreated = isSampleLocallyCreated,
            TestOrders =
            [
                new LisTestOrder { TrayId = "TRAY01", TubePosition = 3 }
            ]
        };
    }
}

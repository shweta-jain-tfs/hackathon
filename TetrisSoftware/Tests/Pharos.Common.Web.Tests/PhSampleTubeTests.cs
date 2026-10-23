using Bunit;
using Pharos.Common.Web.Components.Drawer;

namespace Pharos.Common.Web.Tests;
public class PhSampleTubeTests : BunitContext
{
    [Fact]
    public void PhSampleTube_Should_Render_Correctly()
    {
        var cut = Render<PhSampleTube>(parameters => parameters
        .Add(p => p.Size, SampleTubeSize.Small));

        var sampleTube = cut.Find(".ph-sample-tube");

        Assert.NotNull(sampleTube);
        Assert.Contains("ph-sample-tube--small", sampleTube.ClassList);
    }

    [Fact]
    public void PhSampleTube_Should_Render_CssClass_Based_On_TubeStatus_For_EmptyTube()
    {
        var cut = Render<PhSampleTube>(parameters => parameters
        .Add(p => p.Status, TubeStatus.Empty));

        var sampleTube = cut.Find(".ph-sample-tube__primary-icon-placeholder");

        Assert.NotNull(sampleTube);
        Assert.Contains("ph-sample-tube__primary-icon-placeholder--empty", sampleTube.ClassList);
    }

    [Fact]
    public void PhSampleTube_Should_Render_CssClass_Based_On_TubeStatus_For_NotIdentifiedTube()
    {
        var cut = Render<PhSampleTube>(parameters => parameters
        .Add(p => p.Status, TubeStatus.NotIdentified));

        var sampleTube = cut.Find(".ph-sample-tube__primary-icon-placeholder");

        Assert.NotNull(sampleTube);
        Assert.Contains("ph-sample-tube__primary-icon-placeholder--not-identified", sampleTube.ClassList);
    }

    [Fact]
    public void PhSampleTube_Should_Set_CssClass_Based_On_IsHighPriority()
    {
        var cut = Render<PhSampleTube>(parameters => parameters
        .Add(p => p.IsHighPriority, true));

        var sampleTube = cut.Find(".ph-sample-tube");

        Assert.Contains("ph-sample-tube--high-priority", sampleTube.ClassList);
    }

    [Fact]
    public void PhSampleTube_Should_SetCssClass_Based_On_IsManualEntry()
    {
        var cut = Render<PhSampleTube>(parameters => parameters
        .Add(p => p.IsManualInput, true)
        .Add(p => p.Status, TubeStatus.InProgress));

        var manualEntryClass = cut.Find(".ph-sample-tube__primary-icon-placeholder--manual-entry");

        Assert.NotNull(manualEntryClass);
    }
}

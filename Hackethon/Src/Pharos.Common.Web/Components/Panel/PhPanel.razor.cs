using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Panel;
public partial class PhPanel
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool Grow { get; set; }
    [Parameter] public bool NoBackground { get; set; }

    private string ClassNames { get; set; } = "";

    protected override void OnParametersSet()
    {
        ClassNames = new CssBuilder("ph-panel")
            .AddClass("ph-panel--grow", Grow)
            .AddClass("ph-panel--no-background", NoBackground)
            .Build();
    }
}
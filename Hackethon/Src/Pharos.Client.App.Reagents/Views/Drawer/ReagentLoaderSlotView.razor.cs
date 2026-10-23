using Microsoft.AspNetCore.Components;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.Drawer;
public partial class ReagentLoaderSlotView
{
    [Parameter]
    public ReagentCompartment Compartment { get; set; } = default!;

    [Parameter]
    public ReagentPositionStatus? LegendSelectedStatus { get; set; }

    private string _compartmentCss = string.Empty;

    protected override void OnParametersSet()
    {
        _compartmentCss = new CssBuilder("reagent-loader-slot")
            .AddClass("reagent-loader-slot--filled", !Compartment.IsEmpty)
            .Build();
    }
}

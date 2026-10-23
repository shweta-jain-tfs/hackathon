using Microsoft.AspNetCore.Components;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Views.Drawer.DilutionPlate;
public partial class DilutionPlateSlotView
{
    [Parameter]
    public List<DilutionPlateStack> DilutionPlates { get; set; } = default!;

    [Parameter]
    public ReagentPositionStatus? LegendSelectedStatus { get; set; }
}
using Microsoft.AspNetCore.Components;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.Drawer.DilutionPlate;

public partial class DilutionPlateView
{
    [Parameter]
    public int Position { get; set; }

    [CascadingParameter]
    public bool IsScanningOnGoing { get; set; } = default!;


    private string _dilutionPlateCss = string.Empty;

    protected override void OnParametersSet()
    {
        SetDilutionPlateCss();
    }

    private void SetDilutionPlateCss()
    {
        _dilutionPlateCss = new CssBuilder("ph-dilution-plate")
            .AddClass("ph-dilution-plate--scan", IsScanningOnGoing)
            .Build();
    }
}

using Microsoft.AspNetCore.Components;

namespace Pharos.Client.App.Reagents.Views.Drawer.LoaderDetail;
public partial class ReagentFieldView
{
    [Parameter]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public bool IsDisabled { get; set; }

    [Parameter]
    public string Value { get; set; } = string.Empty;
}

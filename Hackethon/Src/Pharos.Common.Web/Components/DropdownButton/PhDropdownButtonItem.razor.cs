using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.DropdownButton;

public partial class PhDropdownButtonItem
{
    [Parameter, EditorRequired] public string? Text { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public DropdownButtonItemType Type { get; set; } = DropdownButtonItemType.Default;
    [Parameter] public EventCallback OnClick { get; set; }

    private string CssClasses => new CssBuilder("ph-dropdown-button-item")
        .AddClass("ph-dropdown-button-item--link", Type == DropdownButtonItemType.Link)
        .Build();
}
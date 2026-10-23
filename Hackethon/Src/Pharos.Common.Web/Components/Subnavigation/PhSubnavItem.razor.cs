using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Subnavigation;
public partial class PhSubnavItem
{
    [Parameter] public string? Uri { get; set; }
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// For testing only. Set item to active.
    /// </summary>
    [Parameter] public bool ForceActive { get; set; }

    private string LinkClassNames => new CssBuilder("ph-subnav-item__link")
        .AddClass("active", ForceActive) // normally set automatically by NavLink
        .Build();
}
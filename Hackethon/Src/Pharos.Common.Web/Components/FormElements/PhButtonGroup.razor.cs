using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// Places buttons next to eachother with standard spacing.
/// Has an option to make the buttons equal width.
/// </summary>
public partial class PhButtonGroup
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>
    /// Equal width buttons
    /// </summary>
    [Parameter] public bool Justified { get; set; }

    private string ClassNames => new CssBuilder("ph-button-group")
        .AddClass("ph-button-group--justified", Justified)
        .Build();
}
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Layout;

/// <summary>
/// General layout for spacing input fields evenly.
/// </summary>
public partial class PhInputColumn
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The spacing between the elements placed inside this component.
    /// </summary>
    [Parameter] public InputSpacing Spacing { get; set; } = InputSpacing.Default;

    private string ClassNames => new CssBuilder("ph-input-column")
        .AddClass("ph-input-column--compact", Spacing == InputSpacing.Compact)
        .AddClass("ph-input-column--loose", Spacing == InputSpacing.Loose)
        .Build();
}
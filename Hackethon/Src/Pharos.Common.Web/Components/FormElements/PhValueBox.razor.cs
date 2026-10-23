using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// A read-only value display field used for presenting non-editable text or formatted data.
/// </summary>
public partial class PhValueBox
{
    [Parameter] public bool LightMode { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public TextBoxSize Size { get; set; } = TextBoxSize.Default;
    [Parameter] public ValueBoxOverflow Overflow { get; set; } = ValueBoxOverflow.Wrap;
    [Parameter] public int? Rows { get; set; }
    /// <summary>
    /// Use a disabled text color
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string Classname => new CssBuilder("ph-valuebox")
        .AddClass("ph-valuebox--light-mode", LightMode)
        .AddClass("ph-valuebox--disabled", Disabled)
        .AddClass("ph-valuebox--ellipsis", Overflow == ValueBoxOverflow.Ellipsis)
        .AddClass("ph-valuebox--multiline", Overflow == ValueBoxOverflow.Multiline)
        .AddClass("ph-valuebox--has-rows", Rows > 0)
        .AddClass("ph-valuebox--size-xsmall", Size == TextBoxSize.ExtraSmall)
        .AddClass("ph-valuebox--size-small", Size == TextBoxSize.Small)
        .AddClass("ph-valuebox--size-medium", Size == TextBoxSize.Medium)
        .AddClass("ph-valuebox--size-large", Size == TextBoxSize.Large)
        .AddClass("ph-valuebox--size-xlarge", Size == TextBoxSize.ExtraLarge)
        .Build();
}
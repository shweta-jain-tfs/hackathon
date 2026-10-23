using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FieldOperator;

public partial class PhFieldOperator
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? OperatorText { get; set; }

    [Parameter]
    public FieldOperatorWidth? Width { get; set; }

    [Parameter]
    public bool IsMissingOperatorAddSpacing { get; set; }

    [CascadingParameter(Name = "FieldOperatorRowWidth")]
    public FieldOperatorWidth RowWidth { get; set; } = FieldOperatorWidth.Small;

    private FieldOperatorWidth EffectiveWidth => Width ?? RowWidth;

    private string GroupCssClasses => new CssBuilder("ph-field-operator-row__group")
        .AddClass("ph-field-operator-row__group--width-xsmall", EffectiveWidth == FieldOperatorWidth.XSmall)
        .AddClass("ph-field-operator-row__group--width-small", EffectiveWidth == FieldOperatorWidth.Small)
        .AddClass("ph-field-operator-row__group--width-medium", EffectiveWidth == FieldOperatorWidth.Medium)
        .AddClass("ph-field-operator-row__group--width-large", EffectiveWidth == FieldOperatorWidth.Large)
        .AddClass("ph-field-operator-row__group--width-xlarge", EffectiveWidth == FieldOperatorWidth.XLarge)
        .AddClass(
            "ph-field-operator-row__group--operator-last",
            !string.IsNullOrWhiteSpace(OperatorText))
        .Build();
}
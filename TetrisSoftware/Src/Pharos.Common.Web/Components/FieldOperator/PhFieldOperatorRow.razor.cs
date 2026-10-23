namespace Pharos.Common.Web.Components.FieldOperator;

public partial class PhFieldOperatorRow
{
    [Parameter]
    public FieldOperatorWidth Width { get; set; } = FieldOperatorWidth.Small;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
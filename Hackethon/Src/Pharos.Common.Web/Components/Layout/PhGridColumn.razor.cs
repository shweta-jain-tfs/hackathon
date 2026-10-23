namespace Pharos.Common.Web.Components.Layout;

/// <summary>
/// A column in a 12 column grid. Can span several columns in the grid.
/// Use as a child to <see cref="PhGrid"/>.
/// </summary>
public partial class PhGridColumn
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public ColumnSpan Span { get; set; }

    private string ClassNames { get; set; } = "";

    protected override void OnParametersSet()
    {
        ClassNames = Span switch
        {
            ColumnSpan.Col2 => "col-2",
            ColumnSpan.Col3 => "col-3",
            ColumnSpan.Col4 => "col-4",
            ColumnSpan.Col5 => "col-5",
            ColumnSpan.Col7 => "col-7",
            ColumnSpan.Col8 => "col-8",
            ColumnSpan.Col9 => "col-9",
            ColumnSpan.Col10 => "col-10",
            ColumnSpan.Full => "col-full",
            _ or ColumnSpan.Half => "col-half"
        };
    }
}
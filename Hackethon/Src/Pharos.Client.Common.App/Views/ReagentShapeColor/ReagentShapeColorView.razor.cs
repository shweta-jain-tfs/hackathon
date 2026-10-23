namespace Pharos.Client.Common.App.Views.ReagentShapeColor;

public partial class ReagentShapeColorView
{
    [Parameter]
    public ArticleIndicatorShape Shape { get; set; }

    [Parameter]
    public ICollection<string>? Methods { get; set; }

    [Parameter]
    public ICollection<string> Colors { get; set; } = new List<string>();

    [Parameter]
    public string Name { get; set; } = string.Empty;

    // Preserve the existing ReagentShapeColorView contract when delegating to ReagentShapeView:
    // if Methods is provided render method text, otherwise render Name.
    private bool IsCarrierTypeReagent => Methods == null;
}
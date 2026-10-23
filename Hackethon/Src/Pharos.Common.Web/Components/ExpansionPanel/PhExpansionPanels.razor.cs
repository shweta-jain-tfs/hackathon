namespace Pharos.Common.Web.Components.ExpansionPanel;

/// <summary>
/// Parent container for one or more <see cref="PhExpansionPanel"/> components.
/// Use <see cref="MultiExpansion"/> to allow multiple panels to be open at once.
/// </summary>
public partial class PhExpansionPanels
{
    /// <summary>
    /// Panels to display inside the container.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// If true, multiple panels can be expanded at the same time. Default is false.
    /// </summary>
    [Parameter] public bool MultiExpansion { get; set; } = false;

    private readonly List<PhExpansionPanel> _panels = new();

    internal void AddPanel(PhExpansionPanel panel)
    {
        _panels.Add(panel);
    }

    /// <summary>
    /// Collapses all panels except the specified one.
    /// </summary>
    public void CollapseAllExcept(PhExpansionPanel panel)
    {
        foreach (var p in _panels)
        {
            if (p == panel)
            {
                continue;
            }

            _ = p.Collapse();
        }

        StateHasChanged();
    }
}
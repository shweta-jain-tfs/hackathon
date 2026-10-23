using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.ExpansionPanel;

/// <summary>
/// An expandable panel for use inside <see cref="PhExpansionPanels"/>.
/// </summary>
public partial class PhExpansionPanel
{
    [CascadingParameter] private PhExpansionPanels? Parent { get; set; }

    /// <summary>
    /// Controls whether the panel is expanded.
    /// </summary>
    [Parameter] public bool IsExpanded { get; set; }

    /// <summary>
    /// Content displayed when the panel is expanded.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Header text for the panel.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Event callback for expansion state changes.
    /// </summary>
    [Parameter] public EventCallback<bool> IsExpandedChanged { get; set; }

    private bool _isExpanded;

    private void ToggleExpansion()
    {
        _isExpanded = !_isExpanded;
        if (Parent != null && !Parent.MultiExpansion)
        {
            Parent.CollapseAllExcept(this);
        }
    }

    protected override void OnInitialized()
    {
        _isExpanded = IsExpanded;
        Parent?.AddPanel(this);
    }

    public async Task Collapse()
    {
        _isExpanded = false;
        await IsExpandedChanged.InvokeAsync(_isExpanded);
    }

    protected string Classpanel =>
        new CssBuilder("ph-expansion-panel")
            .AddClass("ph-expansion-panel--expanded", _isExpanded)
            .Build();
}
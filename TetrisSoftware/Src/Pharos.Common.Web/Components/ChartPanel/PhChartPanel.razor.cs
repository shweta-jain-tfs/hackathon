using Pharos.Common.Web.Components.Status;

namespace Pharos.Common.Web.Components.ChartPanel;

/// <summary>
/// A panel that consists of a header, body and a footer.
/// </summary>
public partial class PhChartPanel
{
    /// <summary>
    /// Status to show in the header
    /// </summary>
    [Parameter] public StatusType Status { get; set; } = StatusType.None;

    /// <summary>
    /// In the header beside the symbol, place a title or a drop down component.
    /// </summary>
    [Parameter] public RenderFragment? Title { get; set; }

    /// <summary>
    /// Place a component in right corner.
    /// </summary>
    [Parameter] public RenderFragment? Action { get; set; }

    /// <summary>
    /// Place a chart here.
    /// </summary>
    [Parameter] public RenderFragment? Chart { get; set; }

    /// <summary>
    /// Place a data grid here.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }
}
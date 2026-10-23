using Pharos.Common.Web.Components.Status;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.ChartCard;

/// <summary>
/// A clickable card wich displays a small chart (separate component) with a title, link and a status icon.
/// </summary>
/// <remarks>
/// The height and width is controlled by the dimensions of the chart placed in <see cref="ChildContent"/>.
/// </remarks>
public partial class PhChartCard
{
    /// <summary>
    /// Place a chart thumbnail here.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public string? Title { get; set; }

    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Status to show in the header of the card
    /// </summary>
    [Parameter] public StatusType Status { get; set; } = StatusType.None;

    /// <summary>
    /// Render the card without additional padding next to the chart.
    /// </summary>
    [Parameter] public bool Narrow { get; set; }

    private string ClassNames => new CssBuilder("ph-chart-card")
        .AddClass("ph-chart-card--narrow", Narrow)
        .Build();
}
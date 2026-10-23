namespace Pharos.Common.Web.Components.ChartCard;

/// <summary>
/// Horizontally scrolling panel of <see cref="PhChartCard"/> components.
/// </summary>
public partial class PhChartCardList
{
    /// <summary>
    /// Place <see cref="PhChartCard"/> elements in here.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
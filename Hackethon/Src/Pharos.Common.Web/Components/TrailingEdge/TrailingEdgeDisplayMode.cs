namespace Pharos.Common.Web.Components.TrailingEdge;
public enum TrailingEdgeDisplayMode
{
    ShowAllEdges,
    /// <summary>
    /// Hide trailing edge at top. For use in tables and grids with a sticky header.
    /// </summary>
    HideTopEdge,
    /// <summary>
    /// Hide trailing edge at top and left. For use in tables and grids with sticky header and frozen/sticky column.
    /// </summary>
    HideTopAndLeftEdge,
    /// <summary>
    /// Only show trailing edge on vertical scroll while preventing horizontal expansion.
    /// </summary>
    VerticalOnly,
    /// <summary>
    /// Disable the trailing edge and render the child content without any trailing edge.
    /// </summary>
    Disabled,
}

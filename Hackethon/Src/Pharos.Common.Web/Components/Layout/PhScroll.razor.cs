using Pharos.Common.Web.Components.DataGrid;

namespace Pharos.Common.Web.Components.Layout;

/// <summary>
/// Make an element scrollable. Intended to use inside children of <see cref="PhStack"/> that use a flex layout.
/// Adds a scrollbar to generic content. Contains <see cref="PhDataGrid{TGridItem}"/>.
/// </summary>
public partial class PhScroll
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
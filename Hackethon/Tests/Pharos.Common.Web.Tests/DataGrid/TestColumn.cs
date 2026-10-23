using Pharos.Common.Web.Components.DataGrid.Columns;

namespace Pharos.Common.Web.Tests.DataGrid;

/// <summary>
/// A <see cref="Column{TGridItem, TProp}"/> usable outside of a render tree.
/// <para>
/// The real column derives <see cref="Column{TGridItem, TProp}.PropertyName"/> from the
/// <c>Property</c> expression during <c>OnParametersSet</c>, which never runs when a column is
/// simply <c>new</c>-ed up in a unit test. Overriding the property lets filter tests assert on a
/// stable, readable property name without rendering a full grid.
/// </para>
/// <para>
/// Component parameters are assigned inside the constructor (i.e. from within the component)
/// to avoid BL0005.
/// </para>
/// </summary>
public sealed class TestColumn<TGridItem, TProp> : Column<TGridItem, TProp>
{
    public TestColumn(
        string propertyName,
        string? title = null,
        string? filterTitle = null,
        bool quickFilter = false,
        object? quickFilterDefault = null)
    {
        TestPropertyName = propertyName;
        Title = title;
        FilterTitle = filterTitle;
        QuickFilter = quickFilter;
        QuickFilterDefault = quickFilterDefault;
    }

    private string TestPropertyName { get; }

    public override string PropertyName => TestPropertyName;
}

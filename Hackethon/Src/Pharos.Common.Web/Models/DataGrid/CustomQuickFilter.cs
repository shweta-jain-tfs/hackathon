namespace Pharos.Common.Web.Models.DataGrid;

/// <summary>
/// Represents a custom quick filter for a data grid that is not bound to a column.
/// </summary>
public sealed class CustomQuickFilter : IQuickFilterSource
{
    public CustomQuickFilter(IColumnFilter filter, Type quickFilterComponentType)
    {
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        QuickFilterComponentType = quickFilterComponentType ?? throw new ArgumentNullException(nameof(quickFilterComponentType));

        if (!typeof(IComponent).IsAssignableFrom(QuickFilterComponentType))
        {
            throw new ArgumentException($"{nameof(quickFilterComponentType)} must implement {nameof(IComponent)}.", nameof(quickFilterComponentType));
        }
    }

    public IColumnFilter Filter { get; }

    public Type QuickFilterComponentType { get; }

    public bool QuickFilter => true;

    public bool SupportsFiltering => true;

    /// <summary>
    /// Position relative to the column quick filters. Default is <see cref="CustomQuickFilterPosition.Start"/>.
    /// </summary>
    public CustomQuickFilterPosition Position { get; init; } = CustomQuickFilterPosition.Start;
}

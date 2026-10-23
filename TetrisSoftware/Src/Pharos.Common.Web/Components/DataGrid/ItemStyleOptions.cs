namespace Pharos.Common.Web.Components.DataGrid;

/// <summary>
/// Defines options for applying custom styling to grid rows.
/// </summary>
/// <remarks>
/// Use StyleSelector to choose a style for each row, or Items and Match to apply a shared Style.
///
/// If you introduce a new options class, group parameters that are conceptually
/// related and depend on each other. Avoid grouping parameters that are
/// independent toggles (e.g., simple hide/show booleans), as this adds
/// unnecessary nesting without improving clarity.
/// </remarks>
/// <typeparam name="TGridItem">Type of item in the data grid</typeparam>
public sealed record ItemStyleOptions<TGridItem>
{
    /// <summary>
    /// Optional items to match against rows when no StyleSelector is supplied.
    /// </summary>
    public IEnumerable<object> Items { get; init; } = [];

    /// <summary>
    /// Optionally compares <see cref="Items"/> with the row when no StyleSelector is supplied.
    /// Can be two different types (e.g. a view model vs an entity).
    /// </summary>
    public Func<TGridItem, object, bool>? Match { get; init; }

    /// <summary>
    /// Shared style for matching rows; defaults to no styling when omitted.
    /// </summary>
    /// <remarks>
    /// A checked items is only visually checked, it does not affect the actual selection state of the row.
    /// </remarks>
    public ItemStyleFlags Style { get; init; } = ItemStyleFlags.None;

    /// <summary>
    /// Chooses each row's style directly, ignoring Items, Match and Style; return None for normal rows.
    /// </summary>
    public Func<TGridItem, ItemStyleFlags>? StyleSelector { get; init; }

    public bool IsMatch(TGridItem item)
    {
        var match = Match;
        return match is not null && Items.Any(x => match(item, x));
    }
}

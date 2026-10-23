namespace Pharos.Common.Web.Models.DataGrid;

/// <summary>
/// Column info needed by the filter components.
/// Includes the localized column title.
/// </summary>
public interface IFilterableColumn : IQuickFilterSource
{
    /// <summary>
    /// Localized display title for the column.
    /// </summary>
    string? Title { get; set; }

    /// <summary>
    /// Optional localized title used by the filter UI for the column.
    /// </summary>
    string? FilterTitle { get; set; }

    /// <summary>
    /// Resolved title shown by the filter UI.
    /// Falls back to <see cref="Title" /> when no dedicated filter title is provided.
    /// </summary>
    string? EffectiveFilterTitle { get; }

    /// <summary>
    /// Name of the bound property.
    /// </summary>
    string PropertyName { get; }


    /// <summary>
    /// Indicates whether the column is visible in the grid.
    /// </summary>
    bool Visible { get; set; }


    /// <summary>
    /// Indicates whether the column has a quick filter.
    /// </summary>
    new bool QuickFilter { get; set; }
}

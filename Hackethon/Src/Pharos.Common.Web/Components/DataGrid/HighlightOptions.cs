namespace Pharos.Common.Web.Components.DataGrid;

/// <summary>
/// Defines options for configuring row highlight behavior in the grid.
/// </summary>
/// <remarks>
/// If you introduce a new options class, group parameters that are conceptually
/// related and depend on each other. Avoid grouping parameters that are
/// independent toggles (e.g., simple hide/show booleans), as this adds
/// unnecessary nesting without improving clarity.
/// </remarks>
/// <typeparam name="TGridItem">Type of item in the data grid</typeparam>
public sealed record HighlightOptions<TGridItem>
{
    /// <summary>
    /// Determines whether highlight state is stored internally by the grid
    /// or managed externally by the parent.
    /// </summary>
    public required HighlightMode Mode { get; init; }

    /// <summary>
    /// In <see cref="HighlightMode.ExternalState"/> mode,
    /// returns true if a given row should be highlighted.
    /// Must be provided when Mode is ExternalState.
    /// </summary>
    public Func<TGridItem, bool>? IsHighlighted { get; init; }

    /// <summary>
    /// Raised when user interaction changes the highlighted item.
    /// In ExternalState mode, handle this and update the external state accordingly.
    /// </summary>
    public Action<TGridItem?>? HighlightedItemChanged { get; init; }
}

public enum HighlightMode
{
    /// <summary>
    /// The grid manages highlight state internally.
    /// </summary>
    Internal,

    /// <summary>
    /// Highlight state is managed externally by the parent component.
    /// </summary>
    ExternalState
}

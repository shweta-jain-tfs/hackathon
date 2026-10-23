// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pharos.Common.Web.Models.DataGrid;

/// <summary>
/// Parameters for data to be supplied by an <see cref="GridItemsProvider{TGridItem}"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public record GridItemsProviderRequest<TGridItem>
{
    /// <summary>
    /// The zero-based index of the first item to be supplied.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// If set, the maximum number of items to be supplied. If not set, the maximum number is unlimited.
    /// </summary>
    public int? Count { get; }

    /// <summary>
    /// Specifies which column represents the sort order.
    /// </summary>
    public string? SortByColumn { get; }

    /// <summary>
    /// Specifies the current sort direction.
    /// </summary>
    public bool SortByAscending { get; }

    /// <summary>
    /// A collection of column filters for the request.
    /// </summary>
    public IEnumerable<IColumnFilter>? ColumnFilters { get; }

    /// <summary>
    /// A token that indicates if the request should be cancelled.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Indicates whether the request requires bypassing the row limit on the server.
    /// </summary>
    /// <remarks>
    /// Used with Select All.
    /// </remarks>
    public bool RequiresRowLimitBypass { get; }

    public GridItemsProviderRequest(
        int startIndex, int? count, string? sortByColumn, bool sortByAscending,
        IEnumerable<IColumnFilter>? columnFilters, CancellationToken cancellationToken, bool requiresRowLimitBypass = false)
    {
        StartIndex = startIndex;
        Count = count;
        SortByColumn = sortByColumn;
        SortByAscending = sortByAscending;
        CancellationToken = cancellationToken;
        ColumnFilters = columnFilters;
        RequiresRowLimitBypass = requiresRowLimitBypass;
    }
}
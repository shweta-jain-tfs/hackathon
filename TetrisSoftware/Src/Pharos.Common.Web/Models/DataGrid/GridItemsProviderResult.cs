// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Pharos.Common.Web.Models.DataGrid;

/// <summary>
/// Holds data being supplied to a data grid by a <see cref="GridItemsProvider{TGridItem}"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public record GridItemsProviderResult<TGridItem>
{
    /// <summary>
    /// The items being supplied.
    /// </summary>
    public required ICollection<TGridItem> Items { get; init; }

    /// <summary>
    /// The total number of items that may be displayed in the grid. This normally means the total number of items in the
    /// underlying data source after applying any filtering that is in effect.
    ///
    /// If the grid is paginated, this should include all pages. If the grid is virtualized, this should include the entire scroll range.
    /// </summary>
    public int TotalItemCount { get; init; }

    /// <summary>
    /// Optional logical count when the returned items are grouped (e.g. number of groups).
    /// Intended for display (e.g. title) while <see cref="TotalItemCount"/> remains the total rows.
    /// </summary>
    public int? GroupedCount { get; init; }
}

/// <summary>
/// Provides convenience methods for constructing <see cref="GridItemsProviderResult{TGridItem}"/> instances.
/// </summary>
public static class GridItemsProviderResult
{
    // This is just to provide generic type inference, so you don't have to specify TGridItem yet again.

    /// <summary>
    /// Supplies an instance of <see cref="GridItemsProviderResult{TGridItem}"/>.
    /// </summary>
    /// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
    /// <param name="items">The items being supplied.</param>
    /// <param name="totalItemCount">The total number of items that exist. See <see cref="GridItemsProviderResult{TGridItem}.TotalItemCount"/> for details.</param>
    /// <returns>An instance of <see cref="GridItemsProviderResult{TGridItem}"/>.</returns>
    public static GridItemsProviderResult<TGridItem> From<TGridItem>(ICollection<TGridItem> items, int totalItemCount)
    {
        return new GridItemsProviderResult<TGridItem>
        {
            Items = items,
            TotalItemCount = totalItemCount
        };
    }

    /// <summary>
    /// Supplies an instance of <see cref="GridItemsProviderResult{TGridItem}"/> with an optional logical group count.
    /// </summary>
    public static GridItemsProviderResult<TGridItem> From<TGridItem>(ICollection<TGridItem> items, int totalItemCount, int? groupedCount)
    {
        return new GridItemsProviderResult<TGridItem>
        {
            Items = items,
            TotalItemCount = totalItemCount,
            GroupedCount = groupedCount
        };
    }

    /// <summary>
    /// Supplies an instance of <see cref="GridItemsProviderResult{TGridItem}"/> with items filtered and paginated according to the provided <paramref name="selector"/> and <paramref name="request"/>.
    /// </summary>
    public static GridItemsProviderResult<TGridItem> From<TGridItem>(ICollection<TGridItem> allItems, Expression<Func<TGridItem, bool>> selector, GridItemsProviderRequest<TGridItem> request)
    {
        var query = allItems.AsQueryable().Where(selector);
        var totalCount = query.Count();

        var pagedItems = query.Skip(request.StartIndex);
        if (request.Count is { } count)
        {
            pagedItems = pagedItems.Take(count);
        }

        return From(pagedItems.ToList(), totalCount);
    }
}
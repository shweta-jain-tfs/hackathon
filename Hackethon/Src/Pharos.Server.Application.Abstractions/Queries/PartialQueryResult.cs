namespace Pharos.Server.Application.Queries;

public class PartialQueryResult<TDto>
{
    public PartialQueryResult(List<TDto> items, int startIndex, int totalCount, int? groupedCount = null)
    {
        Items = items;
        StartIndex = startIndex;
        TotalCount = totalCount;
        GroupedCount = groupedCount;
    }

    public List<TDto> Items { get; }

    public int StartIndex { get; }

    /// <summary>
    /// Total number of rows/items matching the query (after filtering).
    /// Used for paging/virtualization.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Optional logical count when the returned items are grouped (e.g. number of groups).
    /// Intended for display purposes (e.g. UI title), while <see cref="TotalCount"/> remains the total rows.
    /// </summary>
    public int? GroupedCount { get; }
}

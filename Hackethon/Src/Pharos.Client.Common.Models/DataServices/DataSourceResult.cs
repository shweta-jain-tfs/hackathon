namespace Pharos.Client.Common.Models.DataServices;

/// <summary>
/// Represents the result of a request from a data source.
/// </summary>
/// <typeparam name="TModel">The type of the context for each item in the result.</typeparam>
public record DataSourceResult<TModel>
{
    /// <summary>
    /// The items provided by the data source.
    /// </summary>
    public List<TModel> Items { get; }

    /// <summary>
    /// The total item count in the data source generating the items provided.
    /// </summary>
    public int TotalItemCount { get; }

    /// <summary>
    /// Optional logical count when items are grouped (e.g. number of groups).
    /// </summary>
    public int? GroupedCount { get; }

    /// <summary>
    /// Instantiates a new <see cref="DataSourceResult{TItem}"/> instance.
    /// </summary>
    /// <param name="items">The items provided by the data source.</param>
    /// <param name="totalItemCount">The total item count in the data source generating the items provided.</param>
    /// <param name="groupedCount">Optional logical group count for display.</param>
    public DataSourceResult(List<TModel> items, int totalItemCount, int? groupedCount = null)
    {
        Items = items;
        TotalItemCount = totalItemCount;
        GroupedCount = groupedCount;
    }
}
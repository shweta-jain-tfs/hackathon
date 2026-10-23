namespace Pharos.Client.Common.Models.DataServices;

/// <summary>
/// Represents a request for items from a data source.
/// </summary>
public record DataSourceRequest : DataSourceFilterRequest
{
    /// <summary>
    /// The start index of the first item requested.
    /// </summary>
    public int StartIndex { get; init; }

    /// <summary>
    /// If set, the maximum number of items to be supplied. If not set, the maximum number is unlimited.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    /// Specifies which property represents the sort order.
    /// </summary>
    public string? SortByProperty { get; init; }

    /// <summary>
    /// Specifies the current sort direction.
    /// </summary>
    public bool SortByAscending { get; init; }

    public bool RequiresRowLimitBypass { get; init; }
}
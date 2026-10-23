using System.Collections;

namespace Pharos.Client.Common.Models.DataServices;

/// <summary>
/// Represents a filter for a data source request.
/// </summary>
/// <remarks>
/// The propertyName does not have point to a real property in the model. It can be an abstract
/// term like "FullTextSearch", which is then interpreted by the data service.
/// </remarks>
public sealed record DataSourceFilter
{
    /// <summary>
    /// Represents a filter for a data source request.
    /// </summary>
    /// <param name="PropertyName">The name of the property by which items are filtered.</param>
    /// <param name="FilterValue">The filter value</param>
    /// <param name="FilterOperator">The filter operator type.</param>
    /// <remarks>
    /// The propertyName does not have point to a real property in the model. It can be an abstract
    /// term like "FullTextSearch", which is then interpreted by the data service.
    /// </remarks>
    public DataSourceFilter(string PropertyName, FilterOperator FilterOperator, object? FilterValue)
    {
        this.PropertyName = PropertyName;
        this.FilterOperator = FilterOperator;
        FilterValues = new[]
        {
            FilterValue
        };
    }

    /// <summary>
    /// Represents a filter for a data source request.
    /// </summary>
    /// <param name="PropertyName">The name of the property by which items are filtered.</param>
    /// <param name="FilterValues">The collection of filter values that are treated as alternatives (logical OR).</param>
    /// <param name="FilterOperator">The filter operator type.</param>
    /// <remarks>
    /// The propertyName does not have point to a real property in the model. It can be an abstract
    /// term like "FullTextSearch", which is then interpreted by the data service.
    /// </remarks>
    public DataSourceFilter(string PropertyName, FilterOperator FilterOperator, IList FilterValues)
    {
        if (FilterValues.Count == 0)
        {
            throw new ArgumentException("You must provide at least one value for the filter.", nameof(FilterValues));
        }

        if (FilterValues.Count > 1 && !(FilterOperator is FilterOperator.Contains or FilterOperator.EqualsTo))
        {
            throw new ArgumentException($"Filter operator {FilterOperator} cannot be used with {FilterValues.Count} values.", nameof(FilterOperator));
        }

        this.PropertyName = PropertyName;
        this.FilterOperator = FilterOperator;
        this.FilterValues = FilterValues;
    }

    /// <summary>
    /// The name of the property by which items are filtered.
    /// </summary>
    public string PropertyName { get; init; }

    /// <summary>The filter value</summary>
    public object? FilterValue => FilterValues[0];

    /// <summary>The filter value</summary>
    public IList FilterValues { get; init; }

    /// <summary>The filter operator type.</summary>
    public FilterOperator FilterOperator { get; init; }
}
namespace Pharos.Common.Web.Components.DataGrid.Filters.Constraints;

/// <summary>
/// Configures a string column filter.
/// </summary>
public sealed class StringFilterConstraints : IFilterConstraints<string>
{
    /// <summary>
    /// Creates constraints for a string column filter.
    /// </summary>
    /// <param name="maxLength">The maximum number of characters accepted by the filter.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxLength"/> is less than one.
    /// </exception>
    public StringFilterConstraints(int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);
        MaxLength = maxLength;
    }

    /// <summary>
    /// Gets the maximum number of characters accepted by the filter.
    /// </summary>
    public int MaxLength { get; }
}

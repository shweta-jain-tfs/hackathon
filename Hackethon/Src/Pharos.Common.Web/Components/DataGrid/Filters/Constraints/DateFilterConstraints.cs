using Pharos.Common.Web.Components.DateFilter;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Constraints;

/// <summary>
/// Configures a date/time column quick filter.
/// </summary>
public sealed class DateFilterConstraints :
    IFilterConstraints<DateOnly>,
    IFilterConstraints<DateOnly?>,
    IFilterConstraints<DateTimeOffset>,
    IFilterConstraints<DateTimeOffset?>
{
    /// <summary>
    /// Creates constraints for a date/time quick filter.
    /// </summary>
    /// <param name="options">Visible date filter modes.</param>
    /// <param name="minDate">Optional minimum selectable date.</param>
    /// <param name="maxDate">
    /// Optional maximum selectable date. When null, the consuming <see cref="PhDateFilter"/>
    /// treats it as today (see <see cref="PhDateFilter.MaxDate"/>).
    /// </param>
    public DateFilterConstraints(
        DateFilterOptions options = DateFilterOptions.Default,
        DateOnly? minDate = null,
        DateOnly? maxDate = null)
    {
        if (options == DateFilterOptions.None)
        {
            throw new ArgumentException(
                $"{nameof(options)} must include at least one date filter mode.",
                nameof(options));
        }

        if (minDate is not null && maxDate is not null && minDate > maxDate)
        {
            throw new ArgumentException($"{nameof(minDate)} must be earlier than or equal to {nameof(maxDate)}.");
        }

        Options = options;
        MinDate = minDate;
        MaxDate = maxDate;
    }

    public DateFilterOptions Options { get; }
    public DateOnly? MinDate { get; }
    public DateOnly? MaxDate { get; }
}

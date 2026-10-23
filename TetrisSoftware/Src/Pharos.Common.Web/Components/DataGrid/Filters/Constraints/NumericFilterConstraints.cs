using System.Numerics;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Constraints;

/// <summary>
/// Configures a numeric column filter.
/// </summary>
/// <typeparam name="TNumber">The numeric property type supported by the constraints.</typeparam>
public sealed class NumericFilterConstraints<TNumber> :
    IFilterConstraints<TNumber>,
    IFilterConstraints<TNumber?>,
    INumericFilterConstraints
    where TNumber : struct, INumber<TNumber>
{
    /// <summary>
    /// Creates constraints for a numeric column filter.
    /// </summary>
    /// <param name="allowNegativeNumbers">Whether the filter accepts negative input.</param>
    public NumericFilterConstraints(
        bool allowNegativeNumbers = false)
    {
        AllowNegativeNumbers = allowNegativeNumbers;
    }

    /// <summary>
    /// Gets whether the filter accepts negative input.
    /// </summary>
    public bool AllowNegativeNumbers { get; }
}

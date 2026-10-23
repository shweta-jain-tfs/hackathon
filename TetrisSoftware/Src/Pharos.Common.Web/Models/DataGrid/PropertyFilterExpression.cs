namespace Pharos.Common.Web.Models.DataGrid;

public enum PropertyFilterOperator
{
    EqualsTo,
    GreaterThan,
    GreaterThanOrEqualTo,
    LessThan,
    LessThanOrEqualTo,
    Contains
}

/// <summary>
/// Defines a single, non-generic property filter expression used to filter data grid rows
/// by comparing a model property against a value using a <see cref="PropertyFilterOperator"/>.
/// </summary>
/// <remarks>
/// Use the generic <see cref="PropertyFilterExpression{T}"/> to create instances with a strongly typed value.
/// This base type allows expressions with different value types to be stored and processed together.
/// </remarks>
/// <param name="PropertyName">The name of the underlying model property which is displayed in this column.</param>
/// <param name="Operator">The comparison operator applied between the property value and the filter value.</param>
public abstract record PropertyFilterExpression(string PropertyName, PropertyFilterOperator Operator)
{
    /// <summary>
    /// Gets the filter value as an <see cref="object"/>, for use when the value type is not known at compile time.
    /// </summary>
    public abstract object? UntypedValue { get; }
}

public sealed record PropertyFilterExpression<T>(string PropertyName, PropertyFilterOperator Operator, T Value)
    : PropertyFilterExpression(PropertyName, Operator)
{
    public override object? UntypedValue => Value;
}
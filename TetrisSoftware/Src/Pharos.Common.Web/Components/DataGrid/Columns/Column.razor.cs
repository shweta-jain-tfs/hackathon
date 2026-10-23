using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;
using Pharos.Common.Web.Components.DataGrid.Filters.Constraints;
using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Components.DataGrid.Filters.Quick;
using Pharos.Common.Web.Components.DataGrid.Infrastructure;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Columns;
/// <summary>
/// Represents a column in the data grid with support for filtering, rendering, and permissions.
/// </summary>
/// <typeparam name="TGridItem">The type of the grid item.</typeparam>
/// <typeparam name="TProp">The type of the property displayed in the column.</typeparam>
public partial class Column<TGridItem, TProp> : ColumnBase<TGridItem>
{
    private Func<TGridItem, TProp>? _compiledPropertyExpression;
    private string _propertyName = string.Empty;

    /// <summary>
    /// Provides the internal grid context for the column.
    /// </summary>
    [CascadingParameter, EditorRequired]
    internal InternalGridContext<TGridItem> InternalGridContext { get; set; } = default!;

    /// <summary>
    /// Specifies the property expression to bind this column to for data access, sorting, and filtering.
    /// This should be a property selector (e.g., x => x.PropertyName), not a value.
    /// </summary>
    /// <remarks>
    /// When <see cref="ChildContent" /> is not provided, the value of this property will be rendered directly in the cell using ToString().
    /// For localized display values (e.g., enum localization), either:
    /// - Use a pre-localized property on your model, or
    /// - Provide ChildContent with custom rendering that handles localization.
    /// 
    /// This parameter is optional for display-only columns that only use <see cref="ChildContent" /> (as used in the settings page).
    /// Without <see cref="Property" />, the column cannot be sorted, filtered, or participate in state management.
    /// </remarks>
    [Parameter]
    public Expression<Func<TGridItem, TProp>>? Property { get; set; }

    /// <summary>
    /// Data source to populate the filter list.
    /// </summary>
    [Parameter]
    public Dictionary<string, string>? MultiSelectFilterItems { get; set; }

    /// <summary>
    /// Maps enum values to the display text used in enum filters.
    /// </summary>
    [Parameter]
    public Func<TProp, string>? EnumDisplayNameSelector { get; set; }

    /// <summary>
    /// Configures the filter for this column.
    /// The settings property type must match the column property type.
    /// </summary>
    [Parameter]
    public IFilterConstraints<TProp>? FilterConstraints { get; set; }

    /// <summary>
    /// Template for rendering the content of the column's cells.
    /// </summary>
    [Parameter]
    public RenderFragment<TGridItem>? ChildContent { get; set; }

    private const string _expressionNotRepresentableMessage = "The column property expression can't be represented as a property name. Only simple member expressions, such as x => x.SomeProperty, can be converted to property names.";
    private bool _hasRegisteredColumnInGrid;
    private Func<TGridItem, string?>? _cellTextFunc;

    /// <summary>
    /// Gets the name of the underlying model property displayed in this column.
    /// </summary>
    public override string PropertyName => _propertyName;

    internal override object? GetValue(TGridItem item)
    {
        if (_compiledPropertyExpression is null)
        {
            throw new InvalidOperationException($"Cannot get value for column '{Title}' because the property expression is not set or could not be compiled.");
        }
        return _compiledPropertyExpression(item);
    }

    /// <summary>
    /// Retrieves the full property path from the provided expression, supporting nested properties.
    /// </summary>
    /// <returns>The full property path (e.g., "Name" for x => x.Name, "Model.QualityControlName" for x => x.Model.QualityControlName), or an empty string if not representable.</returns>
    private string GetPropertyName()
    {
        if (Property?.Body is not MemberExpression body)
        {
            return string.Empty;
        }

        return GetFullPropertyPath(body);
    }

    /// <summary>
    /// Extracts the full property path from a member expression, handling nested properties.
    /// </summary>
    /// <param name="memberExpression">The member expression to analyze.</param>
    /// <returns>The full property path from parameter to leaf property.</returns>
    private static string GetFullPropertyPath(MemberExpression memberExpression)
    {
        var parts = new List<string>();
        var current = memberExpression;

        // Build path from leaf to root
        while (current != null)
        {
            parts.Add(current.Member.Name);

            if (current.Expression is ParameterExpression)
            {
                break; // Reached the parameter - we're done
            }

            if (current.Expression is MemberExpression parentMember)
            {
                current = parentMember;
                continue;
            }

            // For anything else (method calls, etc.), return empty
            return string.Empty;
        }

        // Reverse to get root-to-leaf order and join with dots
        parts.Reverse();
        return string.Join(".", parts);
    }

    /// <summary>
    /// Gets the type of the property displayed in this column.
    /// </summary>
    public override Type? PropertyType => typeof(TProp);

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<Dictionary<string, string>?>(
                nameof(MultiSelectFilterItems),
                out var dataSource) &&
            dataSource is null)
        {
            throw new InvalidOperationException(
                $"{nameof(MultiSelectFilterItems)} must be initialized " +
                "to an empty dictionary before the first render.");
        }

        return base.SetParametersAsync(parameters);
    }

    /// <summary>
    /// Called when parameters are set to initialize or update the column's state.
    /// </summary>
    protected override void OnParametersSet()
    {
        // Only extract property name if Property is provided
        _propertyName = Property is not null ? GetPropertyName() : string.Empty;

        if (!HasPermission)
        {
            Visible = false;
            QuickFilter = false;
            return;
        }

        if (Property?.Body is not null)
        {
            _compiledPropertyExpression = Property.Compile();
            _cellTextFunc = GetCellValue;
        }

        HasCustomContent = ChildContent is not null;

        AlignRight = Align switch
        {
            ColumnAlignEnum.Right => true,
            ColumnAlignEnum.Left => false,
            ColumnAlignEnum.Auto => Property is not null && IsNumericType(typeof(TProp)),
            _ => false,
        };

        // Only initialize filtering if a property name is available
        if (!string.IsNullOrEmpty(_propertyName) && SupportsFiltering)
        {
            InitializeColumnFilter();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Validate Property expression after error boundaries are ready
            if (Property is not null && string.IsNullOrEmpty(_propertyName))
            {
                // Yield to ensure error boundary is fully initialized
                await Task.Yield();

                throw new InvalidOperationException($"{_expressionNotRepresentableMessage} Table: {typeof(TGridItem).Name}, Column: '{Title}'");
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private string? GetCellValue(TGridItem item)
    {
        var val = _compiledPropertyExpression!(item);

        if (val is null)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(Format))
        {
            return val?.ToString();
        }

        if (val is not IFormattable)
        {
            throw new InvalidOperationException($"The property {PropertyName} is not IFormattable. Please remove the format string.");
        }

        return ((IFormattable?)_compiledPropertyExpression!(item))?.ToString(Format, null);
    }

    /// <summary>
    /// Renders the content of a cell in this column.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/> used to build the cell content.</param>
    /// <param name="item">The data item for the current row.</param>
    internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        if (ChildContent is not null)
        {
            builder.OpenRegion(0);
            builder.AddContent(1, ChildContent(item));
            builder.CloseRegion();
            return;
        }

        builder.AddContent(0, _cellTextFunc!(item));
    }

    /// <summary>
    /// Assigns the filter together with both of its component types.
    /// <para>
    /// These three always belong together: a filter with no quick filter component silently renders
    /// nothing in the quick filter bar. Setting them through one call makes that pairing structural
    /// instead of something each branch has to remember.
    /// </para>
    /// </summary>
    private void UseFilter(IColumnFilter filter, Type quickFilterComponentType)
    {
        Filter = filter;
        QuickFilterComponentType = quickFilterComponentType;
    }

    /// <summary>
    /// Initializes the filter for the column based on its property type.
    /// </summary>
    private void InitializeColumnFilter()
    {
        if (Filter != null)
        {
            return;
        }

        if (ForceExactMatchFilter)
        {
            UseFilter(
                new StringColumnFilter<TGridItem, TProp>(this, ForceExactMatchFilter),
                typeof(PhQuickStringColumnFilter<TGridItem, TProp>));
        }
        else if (MultiSelectFilterItems is not null)
        {
            CreateMultiSelectFilter();
        }
        else if (IsNumericType(typeof(TProp)))
        {
            UseFilter(
                new NumericColumnFilter<TGridItem, TProp>(this),
                typeof(PhQuickNumericColumnFilter<TGridItem, TProp>));
        }
        else if (typeof(TProp) == typeof(string))
        {
            UseFilter(
                new StringColumnFilter<TGridItem, TProp>(this),
                typeof(PhQuickStringColumnFilter<TGridItem, TProp>));
        }
        else if (typeof(TProp).IsEnum)
        {
            UseFilter(
                new EnumColumnFilter<TGridItem, TProp>(this, EnumDisplayNameSelector),
                typeof(PhQuickEnumColumnFilter<TGridItem, TProp>));
        }
        else if (typeof(TProp) == typeof(bool))
        {
            UseFilter(
                new BoolColumnFilter<TGridItem, TProp>(this),
                typeof(PhQuickBoolColumnFilter<TGridItem>));
        }
        else if (typeof(TProp) == typeof(DateTimeOffset) || typeof(TProp) == typeof(DateTimeOffset?))
        {
            UseFilter(
                new DateTimeColumnFilter<TGridItem, TProp>(this),
                typeof(PhQuickDateTimeColumnFilter<TGridItem, TProp>));
        }
        else
        {
            throw new InvalidOperationException($"You cannot filter column of the {typeof(TProp).FullName} type.");
        }
    }

    /// <summary>
    /// Creates the multi-select filter once.
    /// <para>
    /// The filter is given a provider rather than a snapshot of
    /// <see cref="MultiSelectFilterItems"/>. The data source is frequently populated by an
    /// async load that completes after the first render, and pulling through the provider means the
    /// filter picks those values up on its own instead of the column having to re-push them from
    /// <c>OnParametersSet</c>.
    /// </para>
    /// </summary>
    private void CreateMultiSelectFilter()
    {
        var filter = new KeyValueListColumnFilter<TGridItem, TProp>(this, () => MultiSelectFilterItems);

        // A refresh can drop keys the user had selected, which changes the effective filtering.
        filter.SelectionInvalidated += () => _ = InternalGridContext.Grid.FilterUpdated(filter);

        UseFilter(
            filter,
            typeof(PhQuickKeyValueListCheckboxColumnFilter<TGridItem, TProp>));
    }

    private static readonly HashSet<Type> _numericTypes = new()
    {
        typeof(int),
        typeof(long),
        typeof(double),
    };

    /// <summary>
    /// Determines whether the specified type is numeric.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is numeric; otherwise, false.</returns>
    private static bool IsNumericType(Type type)
    {
        return _numericTypes.Contains(Nullable.GetUnderlyingType(type) ?? type);
    }
}

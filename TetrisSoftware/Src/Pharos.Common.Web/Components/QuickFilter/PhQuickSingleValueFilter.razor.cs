using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Common.Web.Components.QuickFilter;

public partial class PhQuickSingleValueFilter<TValue>
{
    [Parameter, EditorRequired]
    public SingleValueQuickFilter<TValue> ColumnFilter { get; set; } = default!;

    [Parameter]
    public EventCallback OnValueChanged { get; set; }

    private static readonly Type _underlyingType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private string? TextValue
    {
        get => ColumnFilter.HasValue && ColumnFilter.Value is string text ? text : null;
        set => SetStringValue(value);
    }

    private TValue? SelectedValue
    {
        get => ColumnFilter.HasValue ? ColumnFilter.Value : default;
        set => SetSelectedValue(value);
    }

    private bool HasChoiceOptions => ColumnFilter.Choices.Count > 0;

    private static bool IsStringValue => _underlyingType == typeof(string);

    private static bool IsNumericValue => IsNumericType(_underlyingType);

    private static bool IsDateValue => _underlyingType == typeof(DateOnly) || _underlyingType == typeof(DateTime) || _underlyingType == typeof(DateTimeOffset);

    private List<DropDownOption<TValue>> DropdownOptions => ColumnFilter.Choices
        .Select(choice => new DropDownOption<TValue>
        {
            Text = choice.DisplayName,
            Value = choice.Value
        })
        .ToList();

    private void SetStringValue(string? value)
    {
        var isEmpty = string.IsNullOrEmpty(value);
        if (!isEmpty && ColumnFilter.HasValue && ColumnFilter.Value is string existing && existing == value)
        {
            return;
        }

        if (isEmpty)
        {
            ColumnFilter.Clear();
        }
        else
        {
            ColumnFilter.Value = (TValue)(object)value!;
            ColumnFilter.HasValue = true;
        }

        _ = OnValueChanged.InvokeAsync();
    }

    private void SetSelectedValue(TValue? value)
    {
        if (value is null)
        {
            if (!ColumnFilter.HasValue)
            {
                return;
            }

            ColumnFilter.Clear();
            _ = OnValueChanged.InvokeAsync();
            return;
        }

        if (ColumnFilter.HasValue && EqualityComparer<TValue>.Default.Equals(ColumnFilter.Value, value))
        {
            return;
        }

        ColumnFilter.Value = value;
        ColumnFilter.HasValue = true;
        _ = OnValueChanged.InvokeAsync();
    }

    private async Task ClearFilter()
    {
        ColumnFilter.Clear();
        await OnValueChanged.InvokeAsync();
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }
}

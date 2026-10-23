using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

/// <summary>
/// Serializable state object for a single column filter.
/// <para>
/// This is a closed hierarchy: every filter type owns exactly one state type. The
/// <see cref="JsonDerivedTypeAttribute"/> list below is what allows System.Text.Json to round-trip a
/// <c>List&lt;ColumnFilterState&gt;</c> without losing the concrete type, so all state types live in
/// this file and can only be edited together with their discriminators.
/// </para>
/// <para>
/// The discriminator strings are part of the persisted format - never rename one, add a new type
/// instead.
/// </para>
/// </summary>
[JsonDerivedType(typeof(StringFilterState), "string")]
[JsonDerivedType(typeof(NumericFilterState), "numeric")]
[JsonDerivedType(typeof(EnumFilterState), "enum")]
[JsonDerivedType(typeof(BoolFilterState), "bool")]
[JsonDerivedType(typeof(DateTimeFilterState), "dateTime")]
[JsonDerivedType(typeof(KeyValueListFilterState), "keyValueList")]
[JsonDerivedType(typeof(SingleValueQuickFilterState), "quickSingleValue")]
[JsonDerivedType(typeof(MultiSelectQuickFilterState), "quickMultiSelect")]
public abstract class ColumnFilterState
{
    /// <summary>
    /// Identifies which column the state belongs to, used to match saved state back to a filter.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
}

public sealed class StringFilterState : ColumnFilterState
{
    public string? Value { get; set; }
}

public sealed class NumericFilterState : ColumnFilterState
{
    // All three comparisons are stored even when unselected, so reopening the filter still shows
    // the number the user previously typed.
    public NumericFilterValues GreaterThan { get; set; } = new();

    public NumericFilterValues LessThan { get; set; } = new();

    public NumericFilterValues EqualTo { get; set; } = new();
}

public sealed class EnumFilterState : ColumnFilterState
{
    public List<int> SelectedValues { get; set; } = [];
}

public sealed class BoolFilterState : ColumnFilterState
{
    public bool Value { get; set; }
}

public sealed class DateTimeFilterState : ColumnFilterState
{
    public bool TodaySelected { get; set; }

    // Null means the "last N days" mode is inactive, so this doubles as the selection flag.
    public int? LastDaysCount { get; set; }

    // Only ever written as a pair, and only when the "between" mode is active.
    public DateTimeOffset? BetweenStartDate { get; set; }

    public DateTimeOffset? BetweenEndDate { get; set; }

    // Presentation hint for the quick filter (month picker vs two date pickers).
    // It does not affect which rows match.
    public bool IsMonthRange { get; set; }
}

public sealed class KeyValueListFilterState : ColumnFilterState
{
    public List<string> SelectedKeys { get; set; } = [];
}

/// <summary>
/// Concrete state for a single-value quick filter.
/// </summary>
public sealed class SingleValueQuickFilterState : ColumnFilterState
{
    public bool HasValue { get; set; }

    public JsonElement? Value { get; set; }
}

/// <summary>
/// Concrete state for a multi-select quick filter.
/// </summary>
public sealed class MultiSelectQuickFilterState : ColumnFilterState
{
    public List<JsonElement> SelectedValues { get; set; } = [];
}

internal static class QuickFilterStateConverter
{
    public static JsonElement? ToJsonElement<TValue>(TValue? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToElement(value);
    }

    public static TValue? FromJsonElement<TValue>(JsonElement? value)
    {
        if (value is null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return default;
        }

        return value.Value.Deserialize<TValue>();
    }

    public static List<JsonElement> ToJsonElements<TValue>(IEnumerable<TValue> values)
    {
        return values.Select(value => JsonSerializer.SerializeToElement(value)).ToList();
    }

    public static HashSet<TValue> FromJsonElements<TValue>(IEnumerable<JsonElement> values)
    {
        var result = new HashSet<TValue>();

        foreach (var value in values)
        {
            var typedValue = value.Deserialize<TValue>();
            if (typedValue is not null)
            {
                result.Add(typedValue);
            }
        }

        return result;
    }
}
namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

/// <summary>
/// Typed choice used by reusable quick filters.
/// </summary>
public class QuickFilterChoice<TValue>(TValue value, string displayName)
{
    public TValue Value { get; } = value;
    public string DisplayName { get; } = displayName;
}

/// <summary>
/// Typed selectable choice used by reusable multiselect quick filters.
/// </summary>
public sealed class SelectableQuickFilterValue<TValue>(TValue value, string displayName) : QuickFilterChoice<TValue>(value, displayName)
{
    public bool IsSelected { get; set; }
}

namespace Pharos.Common.Web.Components.Breadcrumb;

/// <summary>
/// Displays a breadcrumb navigation bar for the current page.
/// 
/// Only the last two breadcrumb items are shown directly; earlier items are folded into a dropdown menu.
/// 
/// Recommended usage:
/// - Always set the <see cref="Items"/> parameter from the page or parent component for full control and localization.
/// - If the URL has three or more segments/items, use a placeholder for any item whose value hasn't been loaded yet
///   to avoid jerky re-renders as data becomes available.
/// </summary>
public partial class Breadcrumb
{
    /// <summary>
    /// Separator string between breadcrumb items. Default is "/".
    /// </summary>
    [Parameter] public string Separator { get; set; } = "/";

    /// <summary>
    /// List of breadcrumb items. Always set this from the page for best results and localization.
    /// For URLs with three or more segments, use placeholders for items whose values are not yet loaded.
    /// </summary>
    [Parameter, EditorRequired] public required List<BreadcrumbItem> Items { get; set; }

    private List<BreadcrumbItem> _foldedItems = [];
    private List<BreadcrumbItem> _visibleItems = [];

    protected override void OnParametersSet()
    {
        if (Items is null)
        {
            throw new InvalidOperationException($"{nameof(Items)} is required in {nameof(Breadcrumb)}.");
        }

        _visibleItems = Items.TakeLast(2).ToList();
        _foldedItems = Items.SkipLast(2).ToList();

        base.OnParametersSet();
    }
}
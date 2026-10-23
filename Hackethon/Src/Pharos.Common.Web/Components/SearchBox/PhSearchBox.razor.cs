namespace Pharos.Common.Web.Components.SearchBox;

public partial class PhSearchBox
{
    [Parameter] public EventCallback<string> OnSearch { get; set; }
    [Parameter] public string PlaceholderText { get; set; } = string.Empty;
    [Parameter] public bool Disabled { get; set; }

    private ElementReference? _searchInputRef;
    private string? SearchText { get; set; }

    private string CssClassInputWhenFilled => string.IsNullOrWhiteSpace(SearchText) ? string.Empty : "is-filled";

    private async Task TriggerSearch()
    {
        await OnSearch.InvokeAsync(SearchText ?? string.Empty);
    }

    /// <summary>
    /// Triggers search only when Enter is pressed inside the search input field.
    /// </summary>
    private async Task HandleInputKeydown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await TriggerSearch();
        }
    }

    private async Task ClearAndFocusAsync()
    {
        SearchText = string.Empty;

        if (_searchInputRef.HasValue)
        {
            await _searchInputRef.Value.FocusAsync();
        }
    }

}
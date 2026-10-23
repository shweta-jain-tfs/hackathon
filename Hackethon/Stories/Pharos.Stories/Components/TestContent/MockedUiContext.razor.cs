using Microsoft.AspNetCore.Components;
using Pharos.Common.Web.Contexts;
using Pharos.Stories.Services;

namespace Pharos.Stories.Components.TestContent;

/// <summary>
/// Mocked UI context provider for testing purposes.
/// Keeps device type in sync with the theme selector in Stories.
/// Provides hard coded component strings for testing deeply nested components that rely on them, such as data grid filters.
/// </summary>
public partial class MockedUiContext : IDisposable
{
    [Inject] private ThemeService CurrentTheme { get; set; } = default!;

    [Parameter] public required RenderFragment ChildContent { get; set; }

    private UiContext UiContext => new()
    {
        Device = CurrentTheme.Theme == Theme.Desktop ? DeviceType.Desktop : DeviceType.Instrument,
        ComponentStrings = _componentStrings
    };

    private static readonly UiComponentStrings _componentStrings = UiComponentStrings.Default with
    {
        DataGrid = DataGridStrings.Default with
        {
            ClearAllFilters = "Clear all filters",
            ClearFilter = "Clear filter",
            CustomRange = "Custom range",
            From = "From:",
            LastDaysPrefix = "Last",
            LastDaysSuffix = "days",
            Month = "Month",
            ProgressBarText = "Loading selected items...",
            ResetToDefault = "Reset to default",
            SelectedText = "selected",
            Show = "Show",
            To = "To:",
            Today = "Today"
        }
    };

    protected override void OnInitialized()
    {
        CurrentTheme.OnChange += ThemeChanged;
    }

    private void ThemeChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        CurrentTheme.OnChange -= ThemeChanged;
        GC.SuppressFinalize(this);
    }
}
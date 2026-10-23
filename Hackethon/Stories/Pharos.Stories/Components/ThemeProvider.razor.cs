using Microsoft.AspNetCore.Components;
using Pharos.Stories.Services;

namespace Pharos.Stories.Components;
public partial class ThemeProvider : IDisposable
{
    [Inject] private ThemeService CurrentTheme { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string? ThemeCss { get; set; }

    protected override void OnInitialized()
    {
        SetTheme(CurrentTheme.Theme);
        CurrentTheme.OnChange += ThemeUpdated;
    }

    private void ThemeUpdated()
    {
        SetTheme(CurrentTheme.Theme);
        InvokeAsync(StateHasChanged);
    }

    private void SetTheme(Theme currentTheme)
    {
        ThemeCss = currentTheme switch
        {
            Theme.Desktop => null,
            _ => "ph-theme-instrument",
        };
    }

    public void Dispose()
    {
        CurrentTheme.OnChange -= ThemeUpdated;
    }
}
using Microsoft.AspNetCore.Components;
using Pharos.Stories.Services;

namespace Pharos.Stories.Components;
public partial class ThemeSwitcher : IDisposable
{
    [Inject] private ThemeService CurrentTheme { get; set; } = default!;

    private Theme Value { get; set; } = Theme.Instrument;

    protected override void OnInitialized()
    {
        Value = CurrentTheme.Theme;
        CurrentTheme.OnChange += Refresh;
    }

    /// <summary>
    /// Refresh the value of the theme picker, in case there are multiple theme switchers.
    /// </summary>
    private void Refresh()
    {
        if (Value != CurrentTheme.Theme)
        {
            Value = CurrentTheme.Theme;
            InvokeAsync(StateHasChanged);
        }
    }

    private void ThemeChanged()
    {
        CurrentTheme.Theme = Value;
    }

    public void Dispose()
    {
        CurrentTheme.OnChange -= Refresh;
    }
}
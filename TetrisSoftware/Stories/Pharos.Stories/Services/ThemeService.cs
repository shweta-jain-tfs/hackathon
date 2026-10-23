namespace Pharos.Stories.Services;

public class ThemeService
{
    private Theme _theme = Theme.Instrument;

    public Theme Theme
    {
        get => _theme;
        set
        {
            if (_theme != value)
            {
                _theme = value;
                NotifyStateChanged();
            }
        }
    }

    public event Action? OnChange;

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}

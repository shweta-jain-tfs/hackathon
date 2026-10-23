namespace Pharos.Client.Common.App.State.Other;

public class DesktopState : IDesktopState
{
    public event Action? OnTaskCardChange;

    public DesktopState()
    {
        NotifyTaskCardStateChanged();
    }

    private void NotifyTaskCardStateChanged()
    {
        OnTaskCardChange?.Invoke();
    }
}

namespace Pharos.Client.App.Settings.Actions;

internal sealed class SettingsActionOperationState
{
    public bool IsRunning { get; private set; }

    public bool TryBegin()
    {
        if (IsRunning)
        {
            return false;
        }

        IsRunning = true;
        return true;
    }

    public void End()
    {
        IsRunning = false;
    }
}
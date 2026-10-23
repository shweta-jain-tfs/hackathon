namespace Pharos.Client.App.Maintenance.Models;

public abstract class NavigationItem
{
    protected NavigationItem(string navigationName)
    {
        NavigationName = navigationName;
    }

    public string NavigationName { get; }
    public bool Done { get; set; }
    public bool MenuNavigationEnabled { get; set; } = true;
}

public class StepGroup : NavigationItem
{
    public StepGroup(string navigationName, List<ActivityStep> steps) : base(navigationName)
    {
        Steps = steps;
    }

    public List<ActivityStep> Steps { get; }
}

public class ActivityStep : NavigationItem
{
    private readonly List<(string Name, string Value)> _namedMessages = [];
    private Func<Task> _activate = () => Task.CompletedTask;
    private bool _activated;

    public ActivityStep(string navigationName) : base(navigationName)
    {
    }

    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<StepAction> Actions { get; } = [];

    public string Message { get; private set; } = string.Empty;
    public IReadOnlyList<(string Name, string Value)> NamedMessages => _namedMessages.AsReadOnly();

    public async Task Activate()
    {
        Message = string.Empty;
        _namedMessages.Clear();

        if (!_activated)
        {
            await _activate();
            _activated = true;
        }
    }

    public void SetActivate(Func<Task> activate)
    {
        _activate = activate;
    }

    public void SetMessage(string message)
    {
        Message = message;
        RefreshNeeded();
    }

    public void SetNamedMessages(List<(string Name, string Value)> namedMessages)
    {
        _namedMessages.Clear();
        _namedMessages.AddRange(namedMessages);
        RefreshNeeded();
    }

    private void RefreshNeeded()
    {
        OnRefreshNeeded?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OnRefreshNeeded;
}

public class StepAction
{
    public string Text { get; set; } = string.Empty;
    public StepActionType ActionType { get; set; }
    public bool Enabled { get; set; }
    public Func<Task<bool>> Action { get; set; } = () => Task.FromResult(true);
}

public enum StepActionType
{
    Next,
    Previous,
    Start,
    Done, // tell the modal to close
    Performed,
    ApiCall
}
namespace Pharos.Client.Common.App.Services;

public class UiSettingsService(ISingletonDataSource<UiSettings> dataSource)
{
    /// <summary>
    /// Raised when settings change and dependent UI should refresh.
    /// </summary>
    public event Func<Task>? SettingsChanged;

    public async Task<UiSettings> GetSettingsAsync()
    {
        return await dataSource.GetAsync();
    }

    /// <summary>
    /// Notifies subscribers in the current client session that settings have changed.
    /// </summary>
    public async Task NotifySettingsChangedAsync()
    {
        if (SettingsChanged is not null)
        {
            await Task.WhenAll(
                SettingsChanged
                    .GetInvocationList()
                    .Cast<Func<Task>>()
                    .Select(handler => handler()));
        }
    }
}

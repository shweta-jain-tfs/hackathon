using Pharos.Client.Common.App.Services;
using System.Reflection;

namespace Pharos.Client.App.Settings.Views;

public partial class SettingsUiPage
{
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;

    private List<(string Name, object? Value, string Module)> _settings = new();

    protected override async Task OnInitializedAsync()
    {
        var uiSettings = await UiSettingsService.GetSettingsAsync();

        _settings = GetSettings(uiSettings);

        await base.OnInitializedAsync();
    }

    private static List<(string Name, object? Value, string Module)> GetSettings(UiSettings uiSettings)
    {
        var result = new List<(string Name, object? Value, string Module)>();

        // Get all public instance properties of UiSettings.
        var settingsGroups = uiSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var groupProp in settingsGroups)
        {
            var groupValue = groupProp.GetValue(uiSettings);
            if (groupValue == null)
            {
                continue;
            }

            // Get all public instance properties of the group.
            foreach (var prop in groupValue.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var name = prop.Name;
                var value = prop.GetValue(groupValue);
                result.Add((name, value, groupProp.Name.Replace("Settings", "")));
            }
        }

        // Sort by Module.
        return result.OrderBy(x => x.Module).ThenBy(x => x.Name).ToList();
    }
}
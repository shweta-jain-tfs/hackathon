namespace Pharos.Client.App.Settings.ViewModels.Converters;

/// <summary>
/// Provides helper methods for converting components.
/// </summary>
public static class ConverterHelper
{
    /// <summary>
    /// Populates dynamic dropdown choices for a given settings item.
    /// </summary>
    /// <param name="item">The settings item to populate dropdown choices for.</param>
    /// <param name="dataService">The data service used to interact with the settings data.</param>
    public static void PopulateDynamicDropdownChoices(SettingsChildItem item, ISettingsDataService<SettingItemBase> dataService)
    {
        var children = (item switch
        {
            Group group => group.Children,
            Page page => page.Children,
            _ => Enumerable.Empty<SettingsChildItem>()
        }).ToList();

        var dynamicDropdowns = children.OfType<Option>().Where(o => !string.IsNullOrEmpty(o.DynamicDropdownOrigin)).ToList();

        foreach (var dynamicDropdown in dynamicDropdowns)
        {
            var targetPageList = children.FirstOrDefault(c => c.Key == item.Key + ":" + dynamicDropdown.DynamicDropdownOrigin) as PageList;
            List<Choice> choices = [];
            if (targetPageList?.PageEntries != null)
            {
                foreach (var pageEntry in targetPageList.PageEntries)
                {
                    var targetValue = pageEntry switch
                    {
                        Page targetPage => targetPage?.Children.FirstOrDefault(c => c.Key == pageEntry.Key + ":" + dynamicDropdown.DynamicDropdownValueTarget) as Option,
                        PagePlaceholder targetPagePlaceholder => targetPagePlaceholder?.Options.FirstOrDefault(c => c.Key == pageEntry.Key + ":" + dynamicDropdown.DynamicDropdownValueTarget),
                        _ => null
                    };
                    var choice = new Choice
                    {
                        Value = targetValue?.Value ?? pageEntry.Label,
                        Label = pageEntry.Label
                    };
                    choices.Add(choice);
                }

                dynamicDropdown.Choices = choices;
                targetPageList.DynamicDropdowns.Add(dynamicDropdown);
            }

            // If value of the selected choice has been changed remove the old value from DB and reset choice
            if (dynamicDropdown.DefaultValue != dynamicDropdown.Value && choices.All(c => c.Value != dynamicDropdown.Value))
            {
                dynamicDropdown.Value = dynamicDropdown.DefaultValue;
                dataService.DeleteSettingsAsync([dynamicDropdown.Key]);
            }
        }
    }
}
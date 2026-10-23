using Pharos.Client.Common.App.PageRouting;

namespace Pharos.Client.App.Settings.Views;

public partial class SettingsRedirectPage : ComponentBase
{
    [Parameter] public KeyValuePair<string, string> FirstItem { get; set; } = new KeyValuePair<string, string>();
    [Inject] private SettingsMainViewModel ViewModelList { get; set; } = default!;
    private string? SettingsUri { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Gets Dictionary containing <Keys, Labels> of the RootSettings
        var items = await ViewModelList.GetMenuItemsAsync();
        FirstItem = items.First();
        SettingsUri = $"{PageRoute.Settings}/{FirstItem.Value}";
    }
}
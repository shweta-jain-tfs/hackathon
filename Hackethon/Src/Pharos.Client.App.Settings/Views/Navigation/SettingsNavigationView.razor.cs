using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Settings.Views.Navigation;

public partial class SettingsNavigationView
{
    [Inject] private SettingsMainViewModel ViewModelList { get; set; } = default!;

    [Inject] public IModalService Modal { get; set; } = default!;

    [Parameter] public Dictionary<string, string> Items { get; set; } = [];

    protected override async void OnAfterRender(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        // Gets Dictionary containing <Key, Label> of the RootSettings
        Items = await ViewModelList.GetMenuItemsAsync();
    }
}
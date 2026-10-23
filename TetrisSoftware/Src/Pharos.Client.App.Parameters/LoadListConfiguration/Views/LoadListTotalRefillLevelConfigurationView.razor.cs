namespace Pharos.Client.App.Parameters.LoadListConfiguration.Views;

public class LoadListTotalRefillLevelConfigurationView : LoadListConfigurationsView
{
    internal RefillLevelInfo? _refillLevelInfo;

    private string? _lastFetchedInstrument;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await UpdateTotalRefillLevels();
        }
        else if (SelectedInstrument != _lastFetchedInstrument)
        {
            await UpdateTotalRefillLevels();
        }
    }

    internal override async Task OpenEditRefillLevelsModal()
    {
        await base.OpenEditRefillLevelsModal();
        await UpdateTotalRefillLevels();
    }

    internal override async Task OpenEditRefillLevelsModal(Client.Common.Models.LoadListConfiguration context)
    {
        await base.OpenEditRefillLevelsModal(context);
        await UpdateTotalRefillLevels();
    }

    protected async Task UpdateTotalRefillLevels()
    {
        if (!string.IsNullOrEmpty(SelectedInstrument))
        {
            var loadListItemType = GetLoadListItemType();
            if (loadListItemType != null)
            {
                _lastFetchedInstrument = SelectedInstrument;
                _refillLevelInfo = await ViewModel.GetTotalRefillLevel(loadListItemType.Value, SelectedInstrument);
            }
        }

        StateHasChanged();
    }
}
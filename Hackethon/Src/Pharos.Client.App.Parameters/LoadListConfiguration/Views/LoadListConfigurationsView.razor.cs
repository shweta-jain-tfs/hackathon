using Pharos.Client.App.Parameters.LoadListConfiguration.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Parameters.LoadListConfiguration.Views;

public abstract partial class LoadListConfigurationsView
{
    [Inject] internal IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private IToasterService Toaster { get; set; } = null!;
    [Parameter] public LoadListConfigurationsViewModel ViewModel { get; set; } = default!;
    [Parameter] public string SelectedInstrument { get; set; } = default!;
    [Parameter] public string SelectedInstrumentDisplayName { get; set; } = default!;

    protected PhDataGrid<Client.Common.Models.LoadListConfiguration>? DataGrid { get; set; } = default!;

    internal HashSet<Client.Common.Models.LoadListConfiguration>? _selectedConfigurations;

    protected virtual LoadListItemType? GetLoadListItemType()
    {
        return null;
    }
    internal Task SelectedConfigurationsChanged(HashSet<Client.Common.Models.LoadListConfiguration> arg)
    {
        _selectedConfigurations = arg;
        return Task.CompletedTask;
    }

    internal virtual async Task OpenEditRefillLevelsModal()
    {
        if (_selectedConfigurations is null)
        {
            return;
        }

        await OpenEditRefillLevelsModalInternal(_selectedConfigurations);
    }

    internal virtual async Task OpenEditRefillLevelsModal(Client.Common.Models.LoadListConfiguration context)
    {
        var configurations = new HashSet<Client.Common.Models.LoadListConfiguration>
        {
            context
        };
        await OpenEditRefillLevelsModalInternal(configurations);
    }

    private async Task OpenEditRefillLevelsModalInternal(HashSet<Client.Common.Models.LoadListConfiguration> configurations)
    {
        var parameters = new ModalParameters()
        {
            { nameof(EditRefillLevelsModalView.SelectedInstrument), SelectedInstrumentDisplayName },
            { nameof(EditRefillLevelsModalView.SelectedConfigurations), configurations }
        };

        var options = new ModalOptions
        {
            Title = Loc["LoadListConfigurationsEditModal_EditLevels"],
            ModalWidth = ModalSize.Medium,
            ModalHeight = ModalSize.Large,
            ShowCloseButton = true
        };

        var modal = Modal.Show<EditRefillLevelsModalView>(options, parameters);
        var result = await modal.Result;

        if (result is { Canceled: false })
        {
            if (result.Data is ValueTuple<List<int>, int?, int?> { Item1.Count: > 0 } tuple)
            {
                //if both are null , no changes were made

                if (tuple.Item2 == null && tuple.Item3 == null)
                {
                    return;
                }

                // RefreshSelection needed to grid selected items with updated values, otherwise they will hold old values until user clicks on them and triggers a re-render for that item
                try
                {
                    await ViewModel.UpdateLevels(tuple.Item1, tuple.Item2, tuple.Item3);
                    DataGrid?.ClearSelectedItem();
                }
                catch
                {
                    // Handle failure (e.g., show error message)
                    Toaster.ShowToast(ToastType.Warning, Loc["LoadListConfigurationsEditModal_UpdateRefillLevelsFailed"]);

                }
            }
        }
    }
}
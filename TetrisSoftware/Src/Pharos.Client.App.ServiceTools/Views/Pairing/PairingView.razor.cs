using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Contexts;

namespace Pharos.Client.App.ServiceTools.Views.Pairing;

public partial class PairingView : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private PairingViewModel ViewModel { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [CascadingParameter(Name = "UiContext")] public UiContext? Context { get; set; }

    private bool _showRefreshButton;

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        _showRefreshButton = await IsInstrumentInDesktopMode();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task OpenPairingModal()
    {
        var parameters = new ModalParameters
            {
                { nameof(PairingModal.ViewModel), ViewModel }
            };
        var modal = Modal.Show<PairingModal>(
            new ModalOptions
            {
                Title = ViewModel.StageDisplayText,
                ModalHeight = ModalSize.Small,
                ModalWidth = ModalSize.Small
            }, parameters);

        var result = await modal.Result;

        if (result is { Canceled: true })
        {
            await ViewModel.HandleDialogCancelledAsync();
        }
        else
        {
            _showRefreshButton =
                result.Data is PairingModalCloseResult data &&
                data.ShowRefreshFallbackOnPage;

            ViewModel.DismissDone();
        }
    }

    private Task Refresh()
    {
        Navigation.Refresh(forceReload: true);
        return Task.CompletedTask;
    }

    private async Task<bool> IsInstrumentInDesktopMode()
    {
        if (Context?.Device != DeviceType.Desktop)
        {
            return false;
        }

        try
        {
            var currentSerial = await ViewModel.GetCurrentInstrumentSerialAsync();
            return !string.IsNullOrEmpty(currentSerial);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}

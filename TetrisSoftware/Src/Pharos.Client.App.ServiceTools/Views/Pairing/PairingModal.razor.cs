using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts;

namespace Pharos.Client.App.ServiceTools.Views.Pairing;

public sealed record PairingModalCloseResult(bool ShowRefreshFallbackOnPage);

public partial class PairingModal : ComponentBase, IDisposable
{
    [Inject] private IClientDeviceService ClientDevice { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Parameter] public required PairingViewModel ViewModel { get; set; }
    [CascadingParameter] public ModalInstance Modal { get; set; } = null!;

    private bool _isInitialized;
    private bool _showRefreshOption;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (!ViewModel.IsPairingEnabled)
            {
                await ViewModel.EnablePairing();
                Modal.UpdateTitle(ViewModel.StageDisplayText);
            }

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        finally
        {
            _isInitialized = true;
        }
    }

    private bool IsSubmittingApproval => ViewModel.Stage == PairingStage.SubmittingApproval;

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = InvokeAsync(async () =>
        {
            Modal.UpdateTitle(ViewModel.StageDisplayText);
            if (e.PropertyName == nameof(PairingViewModel.PairedInstrument) &&
                !string.IsNullOrWhiteSpace(ViewModel.PairedInstrument))
            {
                if (ClientDevice.ClientDeviceType is ClientDeviceType.Desktop or ClientDeviceType.Auto)
                {
                    var currentSerial = await ViewModel.GetCurrentInstrumentSerialAsync();
                    _showRefreshOption =
                        !string.IsNullOrWhiteSpace(currentSerial) &&
                        string.Equals(currentSerial, ViewModel.PairedInstrument, StringComparison.OrdinalIgnoreCase);
                }
            }

            StateHasChanged();
        });
    }

    private async Task Dismiss()
    {
        await Modal.CloseAsync(ModalResult.Ok(new PairingModalCloseResult(_showRefreshOption)));
    }

    private async Task Refresh()
    {
        await Modal.CloseAsync(ModalResult.Ok(new PairingModalCloseResult(false)));
        Navigation.Refresh(forceReload: true);
    }

    private async Task CancelModal()
    {
        await Modal.CloseAsync(ModalResult.Cancel());
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }
}

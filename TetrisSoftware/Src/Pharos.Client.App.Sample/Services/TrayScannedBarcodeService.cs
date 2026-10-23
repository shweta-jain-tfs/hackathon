using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Sample.Services;

internal class TrayScannedBarcodeService(
    NavigationManager navigationManager,
    ModalState modalState,
    IModalService modalService,
    ISampleTrayDataService sampleTrayDataService,
    IStringLocalizer<Translations> Loc) : IScannedBarcodeService
{
    public async Task HandleScannedBarcode(ScannedBarcode scannedBarcode, CancellationToken ct = default)
    {
        if (!modalState.HasOpenModals())
        {
            navigationManager.NavigateTo(PageRoute.Trays.SelectedTray(scannedBarcode.Barcode));
        }

        if (!scannedBarcode.IsTrayInClosedDrawer)
        {
            return;
        }

        var options = new ModalOptions
        {
            Title = Loc["TrayBarcodeScan_Dialog_Title"],
            Status = ModalStatus.Warning,
            ModalWidth = ModalSize.Small,
            ModalHeight = ModalSize.Small
        };

        var modal = modalService.ShowConfirm(
            Loc["TrayBarcodeScan_Dialog_Body"],
            Loc["TrayBarcodeScan_Dialog_ButtonSecondary"],
            Loc["TrayBarcodeScan_Dialog_ButtonPrimary"],
           options);

        var result = await modal.Result;
        if (result.Canceled)
        {
            return;
        }

        await sampleTrayDataService.SetTrayNotInInstrumentAsync(scannedBarcode.Barcode);
    }
}
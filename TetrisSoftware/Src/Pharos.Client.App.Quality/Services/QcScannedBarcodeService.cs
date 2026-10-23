using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.Views;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Quality.Services;
internal sealed class QcScannedBarcodeService(IModalService modalService, ModalState modalState, IToasterService toasterService, IStringLocalizer<Translations> Loc) : IScannedBarcodeService
{
    public Task HandleScannedBarcode(ScannedBarcode scannedBarcode, CancellationToken ct = default)
    {
        if (scannedBarcode.IsPrimaryBarcode)
        {
            toasterService.ShowToast(ToastType.Information, @Loc["Scan_UDI_Barcode"]);
            return Task.CompletedTask;
        }

        CancelBarcodeModals();

        var parameters = new ModalParameters()
            .Add(nameof(QcCartridgeDetailsView.Barcode), scannedBarcode.Barcode);

        var options = new ModalOptions
        {
            Title = @Loc["QualityControlView_Title"],
            ModalWidth = ModalSize.Small,
            ModalHeight = ModalSize.Large,
            Tag = "ScannedBarcodeDialog"
        };

        modalService.Show<QcCartridgeDetailsView>(options, parameters);
        return Task.CompletedTask;
    }

    private void CancelBarcodeModals()
    {
        modalState.CancelModalsByTag("ScannedBarcodeDialog");
    }
}
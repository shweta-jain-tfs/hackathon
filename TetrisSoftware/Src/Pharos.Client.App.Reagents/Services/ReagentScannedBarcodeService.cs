using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.Views.BarcodeDetails;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Reagents.Services;

internal sealed class ReagentScannedBarcodeService(
    IModalService modalService,
    ModalState modalState,
    IToasterService toasterService,
    IStringLocalizer<Translations> Loc) : IScannedBarcodeService
{
    public Task HandleScannedBarcode(ScannedBarcode scannedBarcode, CancellationToken ct = default)
    {
        if (scannedBarcode.IsPrimaryBarcode)
        {
            toasterService.ShowToast(ToastType.Information, @Loc["Scan_UDI_Barcode"]);
            return Task.CompletedTask;
        }

        CancelBarcodeModals();

        var options = new ModalOptions
        {
            Title = @Loc["ReagentDetailsView_Title"],
            ModalWidth = ModalSize.Small,
            ModalHeight = scannedBarcode.Type == BarcodeType.Carrier ? ModalSize.Large : ModalSize.Medium,
            Tag = "ScannedBarcodeDialog"
        };

        var parameters = new ModalParameters()
            .Add(@Loc["Scan_Dialog_Barcode"], scannedBarcode.Barcode);

        switch (scannedBarcode.Type)
        {
            case BarcodeType.Carrier:
                modalService.Show<CarrierReagentDetailsView>(options, parameters);
                break;
            case BarcodeType.Cartridge:
                modalService.Show<CartridgeReagentDetailsView>(options, parameters);
                break;
            case BarcodeType.Bottle:
                modalService.Show<BottleReagentDetailsView>(options, parameters);
                break;
            default:
                break;
        }

        return Task.CompletedTask;
    }

    private void CancelBarcodeModals()
    {
        modalState.CancelModalsByTag("ScannedBarcodeDialog");
    }
}

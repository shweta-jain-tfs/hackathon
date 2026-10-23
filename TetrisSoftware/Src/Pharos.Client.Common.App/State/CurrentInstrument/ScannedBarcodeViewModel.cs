using Microsoft.JSInterop;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.App.State.CurrentInstrument.Models;

namespace Pharos.Client.Common.App.State.CurrentInstrument;

internal class ScannedBarcodeViewModel(
    IJSRuntime jSRuntime,
    ScannedBarcodeService scannedBarcodeService,
    ICurrentInstrumentDataService currentInstrumentDataService)
{
    public async Task ProcessScannedBarcode(string barcode)
    {
        var wasInputFilled = await TryFillFocusedInputAsync(barcode);

        if (!wasInputFilled)
        {
            await HandleScannedBarcodeAsync(barcode);
        }
    }

    private async Task HandleScannedBarcodeAsync(string barcode)
    {
        var scannedBarcode = await currentInstrumentDataService.GetBarcodeDetails(barcode);
        if (scannedBarcode is not null && !scannedBarcode.IsBottleDrawerOpen)
        {
            var service = scannedBarcodeService.GetService(scannedBarcode.Type);
            await service.HandleScannedBarcode(scannedBarcode);
        }
    }

    private async Task<bool> TryFillFocusedInputAsync(string barcode)
    {
        var barcodeModule = await jSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Pharos.Common.Web/js/barcode.js");
        if (barcodeModule is null)
        {
            throw new InvalidOperationException("Failed to load barcode JS module.");
        }
        var hasInputFocused = await barcodeModule.InvokeAsync<bool>("hasInputFocused");
        if (hasInputFocused)
        {
            await barcodeModule.InvokeVoidAsync("fillActiveInput", barcode);
            return true;
        }

        return false;
    }
}

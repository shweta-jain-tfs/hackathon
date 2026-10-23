namespace Pharos.Client.Common.App.Interfaces;

public interface IScannedBarcodeService
{
    Task HandleScannedBarcode(ScannedBarcode scannedBarcode, CancellationToken cancellationToken = default);
}
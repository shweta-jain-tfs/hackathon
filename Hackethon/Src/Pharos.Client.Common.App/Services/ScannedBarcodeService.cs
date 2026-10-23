using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.Common.App.Services;
internal class ScannedBarcodeService(
     [FromKeyedServices(ScannedBarcodeKeys.ReagentScannedBarcodeService)] IScannedBarcodeService reagentScannedBarcodeService,
    [FromKeyedServices(ScannedBarcodeKeys.QcScannedBarcodeService)] IScannedBarcodeService qcScannedBarcodeService,
    [FromKeyedServices(ScannedBarcodeKeys.SampleScannedBarcodeService)] IScannedBarcodeService sampleScannedBarcodeService,
    [FromKeyedServices(ScannedBarcodeKeys.TrayScannedBarcodeService)] IScannedBarcodeService trayScannedBarcodeService,
    [FromKeyedServices(ScannedBarcodeKeys.SearchScannedBarcodeService)] IScannedBarcodeService searchScannedBarcodeService)
{
    public IScannedBarcodeService GetService(BarcodeType type)
    {
        return type switch
        {
            BarcodeType.Carrier => reagentScannedBarcodeService,
            BarcodeType.Cartridge => reagentScannedBarcodeService,
            BarcodeType.Bottle => reagentScannedBarcodeService,
            BarcodeType.QualityControl => qcScannedBarcodeService,
            BarcodeType.Sample => sampleScannedBarcodeService,
            BarcodeType.Tray => trayScannedBarcodeService,
            BarcodeType.UnknownBarcode => searchScannedBarcodeService,
            _ => throw new NotImplementedException()
        };
    }
}

public static class ScannedBarcodeKeys
{
    public const string ReagentScannedBarcodeService = nameof(ReagentScannedBarcodeService);
    public const string QcScannedBarcodeService = nameof(QcScannedBarcodeService);
    public const string SampleScannedBarcodeService = nameof(SampleScannedBarcodeService);
    public const string TrayScannedBarcodeService = nameof(TrayScannedBarcodeService);
    public const string SearchScannedBarcodeService = nameof(SearchScannedBarcodeService);
}

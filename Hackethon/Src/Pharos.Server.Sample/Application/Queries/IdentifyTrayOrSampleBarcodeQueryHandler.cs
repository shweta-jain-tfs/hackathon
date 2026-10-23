using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Queries;

internal class IdentifyTrayOrSampleBarcodeQueryHandler(
    ISampleDbContext dbContext,
    ISampleQcIdProvider sampleIdProvider,
    ISender sender,
    IInstrumentSampleStorage sampleStorage,
    IInstrumentManager instrumentManager,
    IOptionsSnapshot<TubeBarcodesSettings> tubeBarcodesSettings)
    : IRequestHandler<IdentifyTrayOrSampleBarcodeQuery, SampleBarcodeResult>
{
    public async Task<SampleBarcodeResult> Handle(IdentifyTrayOrSampleBarcodeQuery request, CancellationToken cancellationToken)
    {
        var tray = await dbContext.SampleTrays
            .Where(t => t.Id == request.Barcode)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (tray != null)
        {
            var isTrayInClosedDrawer = IsTrayInClosedDrawer(tray.Id);
            return new SampleBarcodeResult(BarcodeType.Tray, isTrayInClosedDrawer: isTrayInClosedDrawer);
        }

        var configuredPrefixes = GetConfiguredSampleBarcodePrefixes();
        var canQueryMissingRequest = configuredPrefixes.Count == 0
            || MatchesSamplePrefix(request.Barcode, configuredPrefixes);

        if (!canQueryMissingRequest)
        {
            return new SampleBarcodeResult(BarcodeType.UnknownBarcode);
        }

        var sampleKey = await sampleIdProvider.GetSampleQcTubeKey(request.Barcode);
        if (!sampleKey.UnreadableBarcode && !string.IsNullOrEmpty(sampleKey.Id))
        {
            var requestIdResponse = await sender.Send(new GetRequestIdQuery(sampleKey.Id), cancellationToken);
            if (requestIdResponse.RequestExists)
            {
                return new SampleBarcodeResult(BarcodeType.Sample, requestIdResponse.RequestId);
            }
        }

        return new SampleBarcodeResult(BarcodeType.UnknownBarcode, canQueryMissingRequest: canQueryMissingRequest);
    }

    private bool IsTrayInClosedDrawer(string trayId)
    {
        var tray = sampleStorage.GetTray(trayId);
        if (tray == null)
        {
            return false;
        }

        var drawerState = instrumentManager.GetInstrument(tray.InstrumentNumber!).DrawersLockState[tray.Drawer!.Value].LockState;
        return drawerState != LockState.Open;
    }

    private List<string> GetConfiguredSampleBarcodePrefixes()
    {
        var settings = tubeBarcodesSettings.Value;
        return new[]
        {
            settings.Prefix1,
            settings.Prefix2,
            settings.Prefix3,
            settings.Prefix4,
            settings.Prefix5
        }
        .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
        .ToList();
    }

    private static bool MatchesSamplePrefix(string barcode, IEnumerable<string> configuredPrefixes)
    {
        return configuredPrefixes.Any(prefix => barcode.StartsWith(prefix, StringComparison.Ordinal));
    }
}
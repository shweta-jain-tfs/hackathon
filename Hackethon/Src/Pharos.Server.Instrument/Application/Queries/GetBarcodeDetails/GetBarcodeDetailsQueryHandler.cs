using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Quality;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Queries.GetBarcodeDetails;

public record GetBarcodeDetailsQuery(string InstrumentNumber, string Barcode) : IRequest<ScannedBarcodeDto>;

internal class GetBarcodeDetailsQueryHandler(IReagentBarcodeService reagentBarcodeService,
IInstrumentManager instrumentManager, ICurrentInstrumentServerHub currentInstrumentHub, IMediator mediator) : IRequestHandler<GetBarcodeDetailsQuery, ScannedBarcodeDto>
{
    private static readonly HashSet<ArticleType> _carrierTypes =
    [
        ArticleType.ImmunoCapCarrier,
        ArticleType.EliaWellCarrier,
        ArticleType.EliaCalibratorWellCarrier
    ];

    private static readonly HashSet<ArticleType> _cartridgeTypes =
    [
        ArticleType.CalibratorCartridge,
        ArticleType.ConjugateCartridge,
        ArticleType.CurveControlCartridge,
        ArticleType.DevelopmentSolutionCartridge,
        ArticleType.DiluentCartridge,
        ArticleType.FluoroCVial
    ];

    private static readonly HashSet<ArticleType> _bottleTypes =
    [
        ArticleType.StopSolutionBottle,
        ArticleType.WashAdditiveBottle,
        ArticleType.WashConcentrateBottle
    ];

    public async Task<ScannedBarcodeDto> Handle(GetBarcodeDetailsQuery request, CancellationToken cancellationToken)
    {
        var (articleType, barcodeFormat) = await reagentBarcodeService.DetectArticleType(request.Barcode);
        var isPrimaryBarcode = barcodeFormat == BarcodeFormat.Primary;

        return articleType switch
        {
            _ when _carrierTypes.Contains(articleType) =>
                HandleCarrierBarcode(request.Barcode, isPrimaryBarcode),
            _ when _cartridgeTypes.Contains(articleType) =>
                HandleCartridgeBarcode(request.Barcode, isPrimaryBarcode),
            _ when IsQcDataMatrix(request.Barcode) =>
                new ScannedBarcodeDto(request.Barcode, BarcodeType.QualityControl, isPrimaryBarcode: isPrimaryBarcode),
            _ when _bottleTypes.Contains(articleType) =>
                await HandleBottleBarcodeAsync(request, articleType, isPrimaryBarcode),
            _ =>
                await HandleUnknownArticleTypeAsync(request.Barcode, cancellationToken)
        };
    }

    private static ScannedBarcodeDto HandleCarrierBarcode(string barcode, bool isPrimaryBarcode)
    {
        return new ScannedBarcodeDto(barcode, BarcodeType.Carrier, 0, isPrimaryBarcode);
    }

    private static ScannedBarcodeDto HandleCartridgeBarcode(string barcode, bool isPrimaryBarcode)
    {
        return new ScannedBarcodeDto(barcode, BarcodeType.Cartridge, 0, isPrimaryBarcode);
    }

    private async Task<ScannedBarcodeDto> HandleBottleBarcodeAsync(GetBarcodeDetailsQuery request, ArticleType articleType, bool isPrimaryBarcode)
    {
        var drawerState = GetBottleDrawerState(request.InstrumentNumber);

        if (drawerState == LockState.Open)
        {
            return await HandleBottleLoadingSequenceAsync(request, articleType, isPrimaryBarcode);
        }
        else
        {
            return new ScannedBarcodeDto(request.Barcode, BarcodeType.Bottle, isPrimaryBarcode: isPrimaryBarcode);
        }
    }

    private async Task<ScannedBarcodeDto> HandleBottleLoadingSequenceAsync(GetBarcodeDetailsQuery request, ArticleType articleType, bool isPrimaryBarcode)
    {
        var instrument = instrumentManager.FindInstrument(request.InstrumentNumber);

        if (isPrimaryBarcode)
        {
            await SendIncorrectBarcodeNotificationAsync(instrument?.Id);
            return new ScannedBarcodeDto(request.Barcode, BarcodeType.Bottle, isPrimaryBarcode: isPrimaryBarcode, isBottleDrawerOpen: true);
        }

        // Get reagent info from UDI to validate it's a valid bottle
        var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, secondaryBarcode: request.Barcode);

        if (reagentInfo == null)
        {
            if (instrument != null)
            {
                await currentInstrumentHub.IncorrectBarcodeScannedForBottles(instrument.Id);
            }
            return new ScannedBarcodeDto(request.Barcode, BarcodeType.Bottle, isBottleDrawerOpen: true);
        }

        if (instrument != null)
        {
            await currentInstrumentHub.ReagentBarcodeScanned(instrument.Id, request.Barcode);
        }

        return new ScannedBarcodeDto(request.Barcode, BarcodeType.Bottle, isBottleDrawerOpen: true);
    }

    private async Task<ScannedBarcodeDto> HandleUnknownArticleTypeAsync(string barcode, CancellationToken cancellationToken)
    {
        var sampleResult = await mediator.Send(new IdentifyTrayOrSampleBarcodeQuery(barcode), cancellationToken);

        return sampleResult.Type switch
        {
            BarcodeType.Tray => HandleTrayBarcode(barcode, sampleResult.IsTrayInClosedDrawer),
            BarcodeType.Sample => HandleSampleBarcode(barcode, sampleResult.RequestId),
            _ => HandleUnknownBarcode(barcode, sampleResult.CanQueryMissingRequest)
        };
    }

    private static ScannedBarcodeDto HandleTrayBarcode(string barcode, bool isTrayInClosedDrawer)
    {
        return new ScannedBarcodeDto(barcode, BarcodeType.Tray, isTrayInClosedDrawer: isTrayInClosedDrawer);
    }

    private static ScannedBarcodeDto HandleSampleBarcode(string barcode, int requestId)
    {
        return new ScannedBarcodeDto(barcode, BarcodeType.Sample, requestId);
    }

    private static ScannedBarcodeDto HandleUnknownBarcode(string barcode, bool canQueryMissingRequest)
    {
        return new ScannedBarcodeDto(barcode, BarcodeType.UnknownBarcode, canQueryMissingRequest: canQueryMissingRequest);
    }

    private LockState GetBottleDrawerState(string instrumentNumber)
    {
        return instrumentManager.GetInstrument(instrumentNumber).DrawersLockState[DrawerType.BottledReagent].LockState;
    }

    /// <summary>
    /// Sends incorrect barcode notification to client via SignalR.
    /// </summary>
    private async Task SendIncorrectBarcodeNotificationAsync(int? instrumentId)
    {
        if (instrumentId.HasValue)
        {
            await currentInstrumentHub.IncorrectBarcodeScannedForBottles(instrumentId.Value);
        }
    }

    private static bool IsQcDataMatrix(string barcode)
    {
        if (!QcDataMatrixBarcode.TryParse(barcode, out var qcData) || qcData == null)
        {
            return false;
        }

        return true;
    }
}
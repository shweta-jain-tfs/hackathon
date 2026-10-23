using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

internal class IsQualityControlQueryHandler : IRequestHandler<IsQualityControlQuery, QualityControlResultDto>
{
    private readonly IParametersDbContext _dbContext;
    private readonly IIsQualityControlNeededService _isNeededService;

    public IsQualityControlQueryHandler(IParametersDbContext dbContext, IIsQualityControlNeededService isNeededService)
    {
        _dbContext = dbContext;
        _isNeededService = isNeededService;
    }

    public async Task<QualityControlResultDto> Handle(IsQualityControlQuery request, CancellationToken cancellationToken)
    {
        // Check Phadia quality control (barcode length of 7)
        if (request.Barcode.Length == 7)
        {
            var result = await TryGetPhadiaQualityControl(request, cancellationToken);
            if (result.IsQualityControl)
            {
                return result;
            }
        }

        // Check quality controls without barcode format
        var resultWithoutFormat = await TryGetQualityControlWithoutBarcode(request, cancellationToken);
        if (resultWithoutFormat.IsQualityControl)
        {
            return resultWithoutFormat;
        }

        // Check quality controls with barcode formats
        return await TryGetQualityControlWithBarcode(request, cancellationToken);
    }

    private async Task<QualityControlResultDto> TryGetPhadiaQualityControl(IsQualityControlQuery request, CancellationToken cancellationToken)
    {
        var articleNumberPart = request.Barcode[..3];

        var article = await _dbContext.Articles
            .Where(x => x.ArticleNumber == articleNumberPart && x.ArticleType == ArticleType.QualityControlTube)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (article is null)
        {
            return QualityControlResultDto.NotQualityControl;
        }

        var phadiaQcTests = await _dbContext.PhadiaQualityControlTests
            .Include(x => x.QualityControl)
            .Include(x => x.Test)
            .ThenInclude(mt => mt.Method)
            .Where(x => x.QualityControl.Name == article.Identity)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var methodNames = phadiaQcTests.Select(x => x.Test.Method.MethodName).ToHashSet();
        var controlNeeded = await _isNeededService.IsQualityControlNeeded(methodNames, request.InstrumentNumber, cancellationToken);
        var lotNumberPart = request.Barcode[^2..];

        return new QualityControlResultDto(true, article.Identity!, controlNeeded.IsNeeded, lotNumberPart, controlNeeded.TubeTypeId);
    }

    private async Task<QualityControlResultDto> TryGetQualityControlWithoutBarcode(IsQualityControlQuery request, CancellationToken cancellationToken)
    {
        var labQualityControl = await _dbContext.LabQualityControls
            .Include(x => x.Tests)
            .ThenInclude(x => x.Test)
            .ThenInclude(x => x.Method)
            .Where(x => x.BarcodeFormatId == null && x.IdOnBarcode == request.Barcode)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (labQualityControl is null)
        {
            return QualityControlResultDto.NotQualityControl;
        }

        var controlNeeded = await _isNeededService.IsQualityControlNeeded(labQualityControl, request.InstrumentNumber, cancellationToken);
        return new QualityControlResultDto(true, labQualityControl.Name, controlNeeded.IsNeeded, string.Empty, controlNeeded.TubeTypeId);
    }

    private async Task<QualityControlResultDto> TryGetQualityControlWithBarcode(IsQualityControlQuery request, CancellationToken cancellationToken)
    {
        var barcodeFormats = await _dbContext.QualityControlBarcodeFormats
            .Where(x => x.TotalLength == request.Barcode.Length)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (barcodeFormats.Count == 0)
        {
            return QualityControlResultDto.NotQualityControl;
        }

        var matchingFormats = ExtractBarcodeFormatMatches(request.Barcode, barcodeFormats);
        if (matchingFormats.Count == 0)
        {
            return QualityControlResultDto.NotQualityControl;
        }

        var formatLookup = matchingFormats.ToDictionary(m => (m.BarcodeFormatId, m.QcPart), m => m.LotNumber);
        var formatIds = matchingFormats.Select(m => m.BarcodeFormatId).ToHashSet();

        var labQualityControls = await _dbContext.LabQualityControls
            .Include(x => x.Tests)
            .ThenInclude(x => x.Test)
            .ThenInclude(x => x.Method)
            .Where(x => x.BarcodeFormatId != null && formatIds.Contains(x.BarcodeFormatId.Value))
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var matchedControl = labQualityControls.FirstOrDefault(x => formatLookup.ContainsKey((x.BarcodeFormatId!.Value, x.IdOnBarcode)));

        if (matchedControl is null)
        {
            return QualityControlResultDto.NotQualityControl;
        }

        var controlNeeded = await _isNeededService.IsQualityControlNeeded(matchedControl, request.InstrumentNumber, cancellationToken);
        var lotNumber = formatLookup[(matchedControl.BarcodeFormatId!.Value, matchedControl.IdOnBarcode)];

        return new QualityControlResultDto(true, matchedControl.Name, controlNeeded.IsNeeded, lotNumber, controlNeeded.TubeTypeId);
    }

    private static List<MatchingBarcodeFormat> ExtractBarcodeFormatMatches(string barcode, List<QualityControlBarcodeFormat> formats)
    {
        return formats
            .Where(format => format.QcStartPosition - 1 + format.QcLength <= barcode.Length
                             && (!format.ContainsLotInformation || format.LotStartPosition - 1 + format.LotLength <= barcode.Length))
            .Select(format =>
            {
                var qcPart = barcode.Substring(format.QcStartPosition - 1, format.QcLength);
                var lotNumber = format.ContainsLotInformation
                    ? barcode.Substring(format.LotStartPosition - 1, format.LotLength)
                    : string.Empty;
                return new MatchingBarcodeFormat(format.Id, qcPart, lotNumber);
            })
            .ToList();
    }

    private class MatchingBarcodeFormat
    {
        public int BarcodeFormatId { get; }
        public string QcPart { get; }
        public string LotNumber { get; }

        public MatchingBarcodeFormat(int barcodeFormatId, string qcPart, string lotNumber)
        {
            BarcodeFormatId = barcodeFormatId;
            QcPart = qcPart;
            LotNumber = lotNumber;
        }
    }
}
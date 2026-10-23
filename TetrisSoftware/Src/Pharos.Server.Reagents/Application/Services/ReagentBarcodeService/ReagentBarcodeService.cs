using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Mapping;

namespace Pharos.Server.Reagents.Application.Services.ReagentBarcodeService;

internal class ReagentBarcodeService(IArticlesService articlesService,
IMemoryCache memoryCache, ILogger<ReagentBarcodeService> logger) : IReagentBarcodeService
{
    public const int EliaCarrierBarcodeLength = 9;
    public const int EliaCalibratorBarcodeLength = 14;
    public const int LotSpecificCodePosition = 5;
    public const int LotSpecificCodeLength = 7;
    public const string DefaultLotSpecificCode = "0000000";
    private const int _lotNumberLength = 2;
    private const int _minBarcodeLength = 7;
    private const int _maxBarcodeLength = 14;
    private static readonly HashSet<ArticleType> _carriers = new()
    {
     ArticleType.ImmunoCapCarrier,
     ArticleType.EliaWellCarrier,
     ArticleType.EliaCalibratorWellCarrier
    };

    private static readonly HashSet<ArticleType> _cartridges = new()
    {
     ArticleType.CalibratorCartridge,
     ArticleType.ConjugateCartridge,
     ArticleType.CurveControlCartridge,
     ArticleType.DevelopmentSolutionCartridge,
     ArticleType.DiluentCartridge,
     ArticleType.FluoroCVial,
     ArticleType.QualityControlCartridge
    };

    private static readonly HashSet<ArticleType> _bottles = new()
    {
     ArticleType.StopSolutionBottle,
     ArticleType.WashAdditiveBottle,
     ArticleType.WashConcentrateBottle
    };

    /// <summary>
    /// The starting point for relative reagent expiration dates (1980-01-01).
    /// </summary>
    public static readonly DateTimeOffset ExpiryDateStartingPoint = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public async Task<ReagentInfo?> GetReagentInfo(ArticleType articleType, string? primaryBarcode = null, string? secondaryBarcode = null)
    {
        if (articleType == ArticleType.None)
        {
            logger.LogError("Article type cannot be 'None'");
            return null;
        }

        if (string.IsNullOrWhiteSpace(primaryBarcode) && string.IsNullOrWhiteSpace(secondaryBarcode))
        {
            logger.LogError("At least one of 'primary' or 'secondary' barcode must be provided");
            return null;
        }

        if (_carriers.Contains(articleType))
        {
            return await GetCarrierInfo(primaryBarcode, secondaryBarcode).ConfigureAwait(false);
        }
        else if (_cartridges.Contains(articleType))
        {
            return await GetCartridgeInfo(primaryBarcode, secondaryBarcode).ConfigureAwait(false);
        }
        else if (_bottles.Contains(articleType))
        {
            if (string.IsNullOrEmpty(secondaryBarcode))
            {
                throw new ArgumentException("Scan the UDI code instead.");
            }
            return await GetReagentInfoFromUdi(secondaryBarcode);
        }
        else
        {
            logger.LogError("Unsupported article type: {ArticleType}", articleType);
            return null;
        }
    }

    public LotInformation? GetReagentLotInformation(ArticleType articleType, string? primaryBarcode = null, string? secondaryBarcode = null)
    {
        if (articleType == ArticleType.None)
        {
            logger.LogError("Article type cannot be 'None'");
            return null;
        }

        if (string.IsNullOrWhiteSpace(primaryBarcode) && string.IsNullOrWhiteSpace(secondaryBarcode))
        {
            logger.LogError("At least one of 'primary' or 'secondary' barcode must be provided");
            return null;
        }

        if (_carriers.Contains(articleType))
        {
            if (string.IsNullOrEmpty(primaryBarcode))
            {
                return null;
            }
            return GetReagentLotInfoFromBarcode(articleType, primaryBarcode);
        }
        if (_cartridges.Contains(articleType))
        {
            if (string.IsNullOrEmpty(primaryBarcode))
            {
                return null;
            }
            // Cartridge barcode 2D matrix (top) may have FNC1 character at the start so skip it.
            var barcode = primaryBarcode.Length > 1 && primaryBarcode.StartsWith('|') ? primaryBarcode[1..] : primaryBarcode;
            return GetReagentLotInfoFromBarcode(articleType, barcode);
        }
        else if (_bottles.Contains(articleType))
        {
            var parsedUdi = TryParseUdiWithLogging(secondaryBarcode);
            if (parsedUdi == null)
            {
                return null;
            }
            return new LotInformation { ExpiryDate = parsedUdi.ExpiryDate!.Value, LotNumber = $"{parsedUdi.ArticleId}{parsedUdi.LotNumber}" };
        }
        else
        {
            logger.LogError("Unsupported article type: {ArticleType}", articleType);
            return null;
        }
    }

    public string? GetSerialNumber(ArticleType articleType, string? primaryBarcode = null, string? secondaryBarcode = null)
    {
        if (articleType == ArticleType.None)
        {
            logger.LogError("Article type cannot be 'None'");
            return null;
        }

        if (string.IsNullOrWhiteSpace(primaryBarcode) && string.IsNullOrWhiteSpace(secondaryBarcode))
        {
            logger.LogError("At least one of 'primary' or 'secondary' barcode must be provided");
            return null;
        }

        if (_carriers.Contains(articleType) || _bottles.Contains(articleType))
        {
            if (string.IsNullOrEmpty(secondaryBarcode))
            {
                return null;
            }

            var parsedUdi = TryParseUdiWithLogging(secondaryBarcode);
            return parsedUdi?.SerialNumber;
        }
        if (_cartridges.Contains(articleType))
        {
            if (string.IsNullOrEmpty(primaryBarcode))
            {
                return null;
            }
            return GetSerialNumberFromBarcode(primaryBarcode);
        }
        else
        {
            logger.LogError("Unsupported article type: {ArticleType}", articleType);
            return null;
        }
    }

    public async Task<(ArticleType, BarcodeFormat?)> DetectArticleType(string barcode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                logger.LogError("Barcode is null or empty");
                return (ArticleType.None, null);
            }

            // Remove FNC1 character it can be present for cartridge 2D matrix barcodes
            var processedBarcode = barcode.AsSpan();
            if (processedBarcode.Length > 1 && processedBarcode[0] == '|')
            {
                processedBarcode = processedBarcode[1..];
            }

            if (processedBarcode.Length < Constants.ArticleNumberLength)
            {
                logger.LogError("Barcode too short to contain article number");
                return (ArticleType.None, null);
            }

            string articleNumber;
            BarcodeFormat barcodeFormat;

            // Primary barcode detection - check length constraints first for efficiency
            if (processedBarcode.Length is >= _minBarcodeLength and <= _maxBarcodeLength)
            {
                articleNumber = processedBarcode[..Constants.ArticleNumberLength].ToString();
                barcodeFormat = BarcodeFormat.Primary;
            }
            // Secondary barcode (UDI) detection - do not use processedBarcode here as Udi.TryParse expects the full string
            else
            {
                var parsedUdi = TryParseUdiWithLogging(barcode);
                if (parsedUdi != null)
                {
                    articleNumber = parsedUdi.ArticleId;
                    barcodeFormat = BarcodeFormat.Secondary;
                }
                else
                {
                    logger.LogError("Unable to detect article type & barcode format");
                    return (ArticleType.None, null);
                }
            }

            var article = await GetArticleWithCaching(articleNumber).ConfigureAwait(false);
            if (article == null)
            {
                logger.LogWarning("Article not found for article number: {ArticleNumber}", articleNumber);
                return (ArticleType.None, null);
            }

            return (article.ArticleType, barcodeFormat);
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Error detecting article type from barcode");
            return (ArticleType.None, null);
        }
    }

    private async Task<ReagentInfo?> GetCarrierInfo(string? primaryBarcode = null, string? secondaryBarcode = null)
    {
        var hasPrimary = !string.IsNullOrEmpty(primaryBarcode);
        var hasSecondary = !string.IsNullOrEmpty(secondaryBarcode);

        if (hasPrimary && !hasSecondary)
        {
            if (primaryBarcode!.Length is < _minBarcodeLength or > _maxBarcodeLength)
            {
                logger.LogError("Invalid Barcode length: {Length}", primaryBarcode.Length);
                return null;
            }

            var articleNumber = primaryBarcode[..Constants.ArticleNumberLength];
            var article = await GetArticleWithCaching(articleNumber).ConfigureAwait(false);
            if (article == null)
            {
                logger.LogError("Unable to fetch Article information from barcode: {ArticleNumber}", articleNumber);
                return null;
            }

            var lot = GetReagentLotInfoFromBarcode(article.ArticleType, primaryBarcode);
            if (lot == null)
            {
                logger.LogError("Unable to fetch lot information from barcode: {Barcode}", primaryBarcode);
                return null;
            }

            return new ReagentInfo
            {
                Article = article.ToArticle(),
                Lot = lot,
                SerialNumber = null
            };
        }

        if (!hasPrimary && hasSecondary)
        {
            return await GetReagentInfoFromUdi(secondaryBarcode).ConfigureAwait(false);
        }

        // Both barcodes provided - use primary for article/lot, UDI for expiry date and serial number
        if (hasPrimary && hasSecondary)
        {
            if (primaryBarcode!.Length is < _minBarcodeLength or > _maxBarcodeLength)
            {
                logger.LogError("Invalid Barcode length: {Length}", primaryBarcode.Length);
                return null;
            }

            var articleNumber = primaryBarcode[..Constants.ArticleNumberLength];
            var article = await GetArticleWithCaching(articleNumber).ConfigureAwait(false);
            if (article == null)
            {
                logger.LogError("Unable to fetch Article information from barcode: {ArticleNumber}", articleNumber);
                return null;
            }

            var lot = GetReagentLotInfoFromBarcode(article.ArticleType, primaryBarcode);
            if (lot == null)
            {
                logger.LogError("Unable to fetch lot information from barcode: {Barcode}", primaryBarcode);
                return null;
            }

            var parsedUdi = TryParseUdiWithLogging(secondaryBarcode);
            if (parsedUdi == null)
            {
                logger.LogError("Unable to fetch serial number & expiry date from secondary barcode: {SecondaryBarcode}", secondaryBarcode);
                return null;
            }

            // Override expiry date from UDI if different, and get serial number from UDI
            var updateLotInfo = parsedUdi.ExpiryDate.HasValue && lot.ExpiryDate != parsedUdi.ExpiryDate.Value
                ? lot with { ExpiryDate = parsedUdi.ExpiryDate.Value }
                : lot;

            return new ReagentInfo
            {
                Article = article.ToArticle(),
                Lot = updateLotInfo,
                SerialNumber = parsedUdi.SerialNumber
            };
        }

        // Neither barcode provided
        logger.LogError("At least one barcode must be provided");
        return null;
    }

    private async Task<ReagentInfo?> GetCartridgeInfo(string? primaryBarcode = null, string? secondaryBarcode = null)
    {
        if (!string.IsNullOrEmpty(primaryBarcode))
        {
            const int cartridgeBarcodeLength = 12;

            // Cartridge barcode 2D matrix (top) may have FNC1 character at the start so skip it.
            var barcode = primaryBarcode.Length > 1 && primaryBarcode.StartsWith('|') ? primaryBarcode[1..] : primaryBarcode;
            if (barcode.Length != cartridgeBarcodeLength)
            {
                logger.LogError("Invalid Barcode length for cartridge: {Length}, expected: {ExpectedLength}", barcode.Length, cartridgeBarcodeLength);
                return null;
            }

            var articleNumber = barcode[..Constants.ArticleNumberLength];
            var article = await GetArticleWithCaching(articleNumber).ConfigureAwait(false);
            if (article == null)
            {
                logger.LogError("Unable to fetch Article information from barcode: {ArticleNumber}", articleNumber);
                return null;
            }

            var lot = GetReagentLotInfoFromBarcode(article.ArticleType, barcode);
            if (lot == null)
            {
                logger.LogError("Unable to fetch lot information from barcode: {Barcode}", barcode);
                return null;
            }

            var serialNumber = GetSerialNumberFromBarcode(barcode);
            if (serialNumber == null)
            {
                logger.LogError("Unable to fetch serial number from barcode: {Barcode}", barcode);
                return null;
            }

            return new ReagentInfo
            {
                Article = article.ToArticle(),
                Lot = lot,
                SerialNumber = serialNumber
            };
        }
        else
        {
            return await GetReagentInfoFromUdi(secondaryBarcode).ConfigureAwait(false);
        }
    }

    private async Task<ReagentInfo?> GetReagentInfoFromUdi(string? secondaryBarcode)
    {
        var parsedUdi = TryParseUdiWithLogging(secondaryBarcode);
        if (parsedUdi == null)
        {
            logger.LogError("Failed to parse UDI: {SecondaryBarcode}", secondaryBarcode);
            return null;
        }

        var article = await GetArticleWithCaching(parsedUdi.ArticleId).ConfigureAwait(false);
        if (article == null)
        {
            logger.LogError("Unable to fetch Article information from UDI: {ArticleId}", parsedUdi.ArticleId);
            return null;
        }

        return new ReagentInfo
        {
            Article = article.ToArticle(),
            Lot = new LotInformation
            {
                ExpiryDate = parsedUdi.ExpiryDate!.Value,
                LotNumber = $"{parsedUdi.ArticleId}{parsedUdi.LotNumber}",
                DisplayLotNumber = parsedUdi.LotNumber
            },
            SerialNumber = parsedUdi.SerialNumber
        };
    }

    /// <summary>
    /// Attempts to parse UDI barcode with enhanced exception handling and logging.
    /// </summary>
    /// <param name="udi">The UDI string to parse</param>
    /// <returns>Parsed UdiBarcode if successful, null otherwise</returns>
    private UdiBarcode? TryParseUdiWithLogging(string? udi)
    {
        if (string.IsNullOrWhiteSpace(udi))
        {
            return null;
        }

        try
        {
            if (UdiBarcode.TryParse(udi, out var parsedUdi) && parsedUdi != null)
            {
                return parsedUdi;
            }

            logger.LogWarning("Failed to parse UDI: {Udi}", udi);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while parsing UDI {Udi} : {ErrorMessage}", udi, ex.Message);
        }
        return null;
    }

    private LotInformation? GetReagentLotInfoFromBarcode(ArticleType articleType, string barcode)
    {
        try
        {
            var (lotNumber, displayLotNumber) = GetLotNumberFromBarcode(articleType, barcode);
            var expiryDate = GetExpiryDateFromBarcode(articleType, barcode);

            if (articleType is ArticleType.EliaWellCarrier or ArticleType.EliaCalibratorWellCarrier)
            {
                // El-T forces this implementation...
                if (barcode.Length == EliaCarrierBarcodeLength)
                {
                    return new EliaWellLotInformation
                    {
                        LotNumber = lotNumber,
                        DisplayLotNumber = displayLotNumber,
                        ExpiryDate = expiryDate,
                        CorrectionFactor = GetEliaCarrierLotCorrectionFactor(barcode),
                        CorrectionFactorText = barcode.Substring(LotSpecificCodePosition, _lotNumberLength)
                    };
                }

                if (barcode.Length == EliaCalibratorBarcodeLength)
                {
                    const int correctionFactorLength = 2;

                    if (barcode.Length != EliaCalibratorBarcodeLength || barcode.AsSpan(LotSpecificCodePosition, LotSpecificCodeLength - 1) == DefaultLotSpecificCode)
                    {
                        return null;
                    }

                    return new EliaCalibratorWellLotInformation
                    {
                        LotNumber = lotNumber,
                        DisplayLotNumber = displayLotNumber,
                        ExpiryDate = expiryDate,
                        IsChecksumValid = IsEliaCalibratorWellChecksumValid(barcode),
                        Alpha = Base36.DecodeInt(barcode.Substring(LotSpecificCodePosition, correctionFactorLength)) / 100.0,
                        Beta1 = Base36.DecodeInt(barcode.Substring(LotSpecificCodePosition + correctionFactorLength, correctionFactorLength)) / 100.0,
                        Beta2 = Base36.DecodeInt(barcode.Substring(LotSpecificCodePosition + (correctionFactorLength * 2), correctionFactorLength)) / 100.0,
                        CorrectionFactorText = barcode.Substring(LotSpecificCodePosition, LotSpecificCodeLength - 1)
                    };
                }
                return null;
            }

            return new LotInformation
            {
                LotNumber = lotNumber,
                DisplayLotNumber = displayLotNumber,
                ExpiryDate = expiryDate
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting lot information from barcode: {Barcode}", barcode);
            return null;
        }
    }

    private static (string lotNumber, string displayLotNumber) GetLotNumberFromBarcode(ArticleType articleType, string barcode)
    {
        try
        {
            string lotNumber;
            string displayLotNumber;
            switch (articleType)
            {
                case ArticleType.ImmunoCapCarrier:
                    displayLotNumber = barcode.AsSpan(5, _lotNumberLength).ToString();
                    lotNumber = string.Concat(barcode.AsSpan(0, Constants.ArticleNumberLength), displayLotNumber);
                    break;
                case ArticleType.EliaWellCarrier:
                    {
                        var lot = barcode.AsSpan(7, _lotNumberLength);
                        var decimalValue = Base36.DecodeInt(lot.ToString());
                        lotNumber = decimalValue.ToString("0000", CultureInfo.InvariantCulture);
                        displayLotNumber = lotNumber;
                        break;
                    }
                case ArticleType.EliaCalibratorWellCarrier:
                    {
                        var lot = barcode.AsSpan(12, _lotNumberLength);
                        var decimalValue = Base36.DecodeInt(lot.ToString());
                        lotNumber = decimalValue.ToString("0000", CultureInfo.InvariantCulture);
                        displayLotNumber = lotNumber;
                        break;
                    }
                case var type when _bottles.Contains(type):
                    displayLotNumber = barcode.AsSpan(5, _lotNumberLength).ToString();
                    lotNumber = string.Concat(barcode.AsSpan(0, Constants.ArticleNumberLength), displayLotNumber);
                    break;
                case var type when _cartridges.Contains(type):
                    displayLotNumber = barcode.AsSpan(6, _lotNumberLength).ToString();
                    lotNumber = string.Concat(barcode.AsSpan(0, Constants.ArticleNumberLength), displayLotNumber);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported article type: {articleType}");
            }

            // For EliA carriers with Base 36 decoding, the decimal value will contain only digits
            if (articleType is ArticleType.EliaWellCarrier or ArticleType.EliaCalibratorWellCarrier)
            {
                if (!lotNumber.All(char.IsDigit))
                {
                    throw new ArgumentException($"Invalid lot number: '{lotNumber}'. EliA carrier lot number must contain only digits (0-9).");
                }
            }
            else
            {
                // Validate that the lot number contains only alphanumeric characters (0-9, A-Z) for non-EliA carriers
                if (!lotNumber.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'Z')))
                {
                    throw new ArgumentException($"Invalid lot number: '{lotNumber}'. Lot number must contain only alphanumeric characters (0-9, A-Z).");
                }
            }

            return (lotNumber, displayLotNumber);
        }
        catch (ArgumentOutOfRangeException e)
        {
            throw new ArgumentException("Invalid reagent barcode: failed to extract the lot number.", e);
        }
    }

    private static string? GetSerialNumberFromBarcode(string? barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }
        const int serialNumberPosition = 8;
        const int serialNumberLength = 4;

        if (barcode.Length >= serialNumberPosition + serialNumberLength)
        {
            return barcode.Substring(serialNumberPosition, serialNumberLength);
        }

        return null;
    }

    private static DateTimeOffset GetExpiryDateFromBarcode(ArticleType articleType, string barcode)
    {
        var encodedRelativeDate = barcode.Substring(Constants.ArticleNumberLength, 2);

        if (!encodedRelativeDate.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'Z')))
        {
            throw new ArgumentException($"Invalid expiry date, must contain only alphanumeric characters (0-9, A-Z).");
        }

        //Bottle reagents exclusive to Phadia 1.0 do not have lot number encoding. Bottle reagents shared with Phadia 1.0 use the proprietary barcode format.
        if (_carriers.Contains(articleType) || _bottles.Contains(articleType))
        {
            return DecodeTwoSymbolExpiryDate(encodedRelativeDate);
        }

        if (_cartridges.Contains(articleType))
        {
            var daySymbol = barcode[Constants.ArticleNumberLength + 2].ToString();
            return DecodeThreeSymbolExpiryDate(encodedRelativeDate + daySymbol);
        }

        throw new NotSupportedException($"Unsupported article type: {articleType}");
    }

    private static DateTimeOffset DecodeTwoSymbolExpiryDate(string encodedRelativeDate)
    {
        var months = Base36.DecodeInt(encodedRelativeDate);
        return ExpiryDateStartingPoint.AddMonths(months + 1).AddDays(-1);
    }

    private static DateTimeOffset DecodeThreeSymbolExpiryDate(string encodedRelativeDate)
    {
        var monthsEncoded = encodedRelativeDate[..2];
        var daySymbol = encodedRelativeDate[2];

        var months = Base36.DecodeInt(monthsEncoded);
        var dayOfMonth = Base36.DecodeInt(daySymbol);

        return ExpiryDateStartingPoint.AddMonths(months).AddDays(dayOfMonth - 1);
    }

    private static double GetEliaCarrierLotCorrectionFactor(string barcode)
    {
        if (barcode.Length != EliaCarrierBarcodeLength)
        {
            return 1.0;
        }

        const int correctionFactorPosition = 5;
        const int correctionFactorLength = 2;
        return Base36.DecodeInt(barcode.Substring(correctionFactorPosition, correctionFactorLength)) / 100.0;
    }

    private static bool IsEliaCalibratorWellChecksumValid(string barcode)
    {
        int i;

        var sum = 0;

        if (barcode.Length != EliaCalibratorBarcodeLength)
        {
            return false;
        }

        var buffer = barcode.AsSpan(LotSpecificCodePosition, LotSpecificCodeLength);

        for (i = 0; i < buffer.Length - 1; i++)
        {
            sum += Base36.DecodeInt(buffer[i]);
        }
        var base36Sum = Base36.EncodeInt(sum);
        i = base36Sum.Length;

        return base36Sum[i - 1] == buffer[LotSpecificCodeLength - 1];
    }

    private async Task<Article?> GetArticleWithCaching(string articleNumber)
    {
        // try cached article first
        memoryCache.TryGetValue(articleNumber, out Article? article);

        if (article == null)
        {
            article = await articlesService.GetArticle(articleNumber).ConfigureAwait(false);

            if (article != null)
            {
                memoryCache.Set(articleNumber, article, DateTimeOffset.Now.AddDays(1));
            }
        }

        return article;
    }

    public async Task<IReadOnlyDictionary<string, ReagentInfo>> GetCarrierContentsFromOcr(IReadOnlyList<string> ocrList)
    {
        var result = new Dictionary<string, ReagentInfo>();
        var identitiesToFetch = new List<string>();

        for (var i = 0; i < ocrList.Count; i++)
        {
            var ocr = ocrList[i];
            if (string.IsNullOrEmpty(ocr))
            {
                continue;
            }
            if (memoryCache.TryGetValue(ocr, out ReagentInfo? cachedInfo) && cachedInfo != null)
            {
                result[ocr] = cachedInfo;
            }
            else
            {
                identitiesToFetch.Add(ocr);
            }
        }

        if (identitiesToFetch.Count > 0)
        {
            var articles = await articlesService.GetCarrierArticlesFromIdentities(identitiesToFetch);
            var grouped = articles
                .Where(a => a.Identity != null)
                .GroupBy(a => a.Identity!)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var ocr in identitiesToFetch)
            {
                ReagentInfo info;
                if (!grouped.TryGetValue(ocr, out var matches) || matches.Count == 0)
                {
                    info = new ReagentInfo
                    {
                        Article = new Article
                        {
                            ArticleType = ArticleType.None,
                            ArticleNumber = string.Empty,
                            Identity = ocr,
                            Quantity = 0
                        },
                    };
                }
                else
                {
                    var types = matches.Select(a => a.ArticleType).Distinct().ToList();
                    if (types.Count == 1)
                    {
                        var minQuantity = matches.Min(a => a.Quantity);
                        var article = matches.First();
                        article.Quantity = minQuantity;
                        info = new ReagentInfo
                        {
                            Article = article
                        };
                    }
                    else
                    {
                        info = new ReagentInfo
                        {
                            Article = new Article
                            {
                                ArticleType = ArticleType.None,
                                ArticleNumber = string.Empty,
                                Identity = ocr,
                                Quantity = 0
                            },
                        };
                    }
                }
                memoryCache.Set(ocr, info, DateTimeOffset.Now.AddDays(1));
                result[ocr] = info;
            }
        }
        return result;
    }
}
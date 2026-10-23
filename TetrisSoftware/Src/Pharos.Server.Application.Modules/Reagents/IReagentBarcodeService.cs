using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Reagents;

/// <summary>
/// The service to parse reagent barcodes and extract "static" information about the reagents.
/// </summary>
public interface IReagentBarcodeService
{
    /// <summary>
    /// Get reagent info from the barcode based on the article type
    /// </summary>
    /// <param name="articleType">The article type</param>
    /// <param name="primaryBarcode">Parameter dedicated to 1D, 2D Barcode</param>
    /// <param name="secondaryBarcode">Parameter dedicated to UDI</param>
    /// <returns></returns>
    Task<ReagentInfo?> GetReagentInfo(ArticleType articleType, string? primaryBarcode = null, string? secondaryBarcode = null);

    /// <summary>
    /// Get reagent lot information from the barcode based on the article type
    /// </summary>
    /// <param name="articleType">The article type</param>
    /// <param name="primaryBarcode">Parameter dedicated to 1D, 2D Barcode</param>
    /// <param name="secondaryBarcode">Parameter dedicated to UDI</param>
    /// <returns></returns>
    LotInformation? GetReagentLotInformation(ArticleType articleType, string? primaryBarcode = null, string? secondaryBarcode = null);

    /// <summary>
    /// Get serial number from the barcode based on the article type
    /// </summary>
    /// <param name="articleType">The article type</param>
    /// <param name="primaryBarcode">Parameter dedicated to 1D, 2D Barcode</param>
    /// <param name="secondaryBarcode">Parameter dedicated to UDI</param>
    /// <returns></returns>
    string? GetSerialNumber(ArticleType articleType, string? primaryBarcode = null, string? secondaryBarcode = null);

    /// <summary>
    /// Mainly when external scanner is used to scan the barcode, detect the article type from the barcode
    /// </summary>
    /// <param name="barcode">Input barcode to identify the ArticleType and its format</param>
    /// <returns></returns>
    Task<(ArticleType, BarcodeFormat?)> DetectArticleType(string barcode);

    /// <summary>
    /// Gets carrier content information for a list of OCR test names.
    /// </summary>
    /// <param name="ocrList">List of OCR test names.</param>
    /// <returns>
    /// Dictionary mapping OCR test name to ReagentInfo (unknown if not found or ambiguous).
    /// </returns>
    Task<IReadOnlyDictionary<string, ReagentInfo>> GetCarrierContentsFromOcr(IReadOnlyList<string> ocrList);
}

public enum BarcodeFormat
{
    Primary,
    Secondary,
}
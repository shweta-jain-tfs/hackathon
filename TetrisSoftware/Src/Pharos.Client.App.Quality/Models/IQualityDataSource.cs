namespace Pharos.Client.App.Quality.Models;

/// <summary>
/// Data source for Quality operations.
/// </summary>
public interface IQualityDataSource
{
    /// <summary>
    /// Parses a QC data matrix barcode and returns cartridge information.
    /// </summary>
    /// <param name="dataMatrix">The QC data matrix barcode string</param>
    /// <returns>Parsed QC cartridge information, or null if parsing fails</returns>
    Task<QcCartridgeInformation?> GetQcCartridgeInfo(string dataMatrix);
}

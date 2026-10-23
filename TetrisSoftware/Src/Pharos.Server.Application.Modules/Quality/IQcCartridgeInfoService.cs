namespace Pharos.Server.Application.Quality;

/// <summary>
/// Service for parsing QC cartridge data matrix barcodes.
/// </summary>
public interface IQcCartridgeInfoService
{
    /// <summary>
    /// Parses a QC data matrix barcode and returns the cartridge information.
    /// Test units are automatically populated from method parameters.
    /// </summary>
    /// <param name="dataMatrix">The QC data matrix barcode string</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Parsed QC cartridge information, or null if parsing fails</returns>
    Task<QcCartridgeInformationDto?> ParseQcDataMatrixAsync(string dataMatrix, CancellationToken cancellationToken);
}

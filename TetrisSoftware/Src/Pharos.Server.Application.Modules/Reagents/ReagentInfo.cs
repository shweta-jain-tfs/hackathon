using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Application.Reagents;

/// <summary>
/// Represents information about a reagent that can be extracted from the barcode.
/// </summary>
public record ReagentInfo
{
    /// <summary>
    /// The article information.
    /// </summary>
    public Article Article { get; init; } = default!;

    /// <summary>
    /// The reagent lot information, extracted from the barcode.
    /// </summary>
    public LotInformation Lot { get; init; } = default!;

    /// <summary>
    /// The serial number of the reagent in the lot.
    /// </summary>
    public string? SerialNumber { get; init; } = default!;
}

public record LotInformation
{
    /// <summary>
    /// The string that uniquely identifies the lot (5-characters) but in case EliA well and EliA calibrator it will be 4 characters.
    /// The lot number is a unique combination of article identifier and lot number symbols from the reagent barcode.
    /// </summary>
    public string LotNumber { get; init; } = default!;
    public string DisplayLotNumber { get; init; } = default!;

    /// <summary>
    /// The expiration date (last day of use) of this reagent lot (batch).
    /// After a reagent is open, its expiration date no longer equals to the batch expiration date.
    /// </summary>
    public DateTimeOffset ExpiryDate { get; init; }
}

public record EliaWellLotInformation : LotInformation
{
    public double CorrectionFactor { get; init; }
    public string CorrectionFactorText { get; init; } = default!;
}

public record EliaCalibratorWellLotInformation : LotInformation
{
    public bool IsChecksumValid { get; init; }
    public double Alpha { get; init; }
    public double Beta1 { get; init; }
    public double Beta2 { get; init; }
    public bool NoAdjustment => Alpha == 0 && Beta1 == 0 && Beta2 == 0;
    public string CorrectionFactorText { get; init; } = default!;

}
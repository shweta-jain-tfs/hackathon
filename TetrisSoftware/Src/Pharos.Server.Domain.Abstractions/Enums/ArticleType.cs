namespace Pharos.Server.Domain.Enums;

/// <summary>
/// This enumeration lists all article types available in the Tetris system.
/// It does not include article types used only in Prime systems (e.g. "calibrator strip").
/// </summary>
/// <remarks>
/// IMPORTANT: numbers match the articles file in the parameters package. Do NOT change them.
/// </remarks>
public enum ArticleType
{
    None = 0,

    /// <summary>
    /// A cartridge with the calibration liquid.
    /// </summary>
    CalibratorCartridge = 31,

    /// <summary>
    /// A cartridge with the conjugate liquid.
    /// </summary>
    ConjugateCartridge = 33,

    /// <summary>
    /// A cartridge with the curve control liquid.
    /// </summary>
    CurveControlCartridge = 32,

    /// <summary>
    /// A cartridge with the development solution.
    /// </summary>
    DevelopmentSolutionCartridge = 34,

    /// <summary>
    /// A cartridge with the diluent.
    /// </summary>
    DiluentCartridge = 35,

    /// <summary>
    /// A dilution plate.
    /// </summary>
    DilutionPlate = 41,

    /// <summary>
    /// A dilution wells carrier.
    /// </summary>
    DilutionWellCarrier = 19,

    /// <summary>
    /// A carrier with EliA calibration wells.
    /// </summary>
    EliaCalibratorWellCarrier = 17,

    /// <summary>
    /// A carrier with EliA Wells.
    /// </summary>
    EliaWellCarrier = 15,

    ///// <summary>
    ///// The fluorescence calibrator vial.
    ///// </summary>
    FluoroCVial = 10,

    /// <summary>
    /// An ImmunoCap carrier.
    /// </summary>
    ImmunoCapCarrier = 1,

    ///// <summary>
    ///// The maintenance concentrate.
    ///// </summary>
    MaintenanceConcentrateBottle = 40,

    /// <summary>
    /// A cartridge with the quality control liquid.
    /// </summary>
    QualityControlCartridge = 30,

    /// <summary>
    /// A quality control sample tube (e.g. Phadia freeze dried QC).
    /// </summary>
    QualityControlTube = 5,

    /// <summary>
    /// The stop solution.
    /// </summary>
    StopSolutionBottle = 7,

    /// <summary>
    /// A wash additive.
    /// </summary>
    WashAdditiveBottle = 21,

    /// <summary>
    /// A wash concentrate.
    /// </summary>
    WashConcentrateBottle = 20
}
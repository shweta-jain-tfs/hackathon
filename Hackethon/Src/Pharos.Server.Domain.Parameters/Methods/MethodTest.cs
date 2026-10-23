using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// A method may have several tests that can be processed in order
/// to diagnose allergy or autoimmune diseases.
/// </summary>
public class MethodTest : BaseAuditableEntity
{
    public const int DevelopmentSolutionIdentityLength = 8;
    public const int DiluentIdentityLength = 16;
    public const int FullTestNameLength = 30;
    public const int TextQuantitativeResultLength = 7;
    public const string TextDilutedQuantitativeDefault = "---";

    /// <summary>
    /// The ID for the method this test belongs to
    /// </summary>
    public int MethodId { get; set; }

    /// <summary>
    /// The short name of a test, for example "c1"
    /// </summary>
    [MaxLength(Constants.TestNameLength)]
    public string TestName { get; set; } = default!;

    /// <summary>
    /// The long name of the test, for example "Cat dander"
    /// </summary>
    [MaxLength(FullTestNameLength)]
    public string FullTestName { get; set; } = default!;

    /// <summary>
    /// The value indicating whether this test is activated (enabled for use) in the lab or not.
    /// </summary>
    /// <remarks>
    /// The value is not provided in the method update file.
    /// </remarks>
    public bool Active { get; set; } = true;

    #region Replicates

    /// <summary>
    /// When testing this one, repeat it a number of times
    /// </summary>
    public int Replicates => UserReplicates ?? DefaultReplicates;

    /// <summary>
    /// The default value for the <see cref="Replicates"/> property provided in the method update file.
    /// </summary>
    public int DefaultReplicates { get; set; } = 1;

    /// <summary>
    /// The user-provided value for the <see cref="Replicates"/> property.
    /// </summary>
    public int? UserReplicates { get; set; }

    #endregion

    #region ReportConcentration

    /// <summary>
    /// This test should report concentration results.
    /// The different result reporting types, this and the ones below, can be enabled/disabled by the operator
    /// </summary>
    public bool ReportConcentration => UserReportConcentration ?? DefaultReportConcentration;

    /// <summary>
    /// The default value for the <see cref="ReportConcentration"/> property provided in the method update file.
    /// </summary>
    public bool DefaultReportConcentration { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="ReportConcentration"/> property.
    /// </summary>
    public bool? UserReportConcentration { get; set; }

    #endregion

    #region ReportClass

    /// <summary>
    /// The value indicating whether result classes should be reported for the test or not.
    /// </summary>
    public bool ReportClass => (UserReportClassId ?? DefaultReportClassId) != 0;

    /// <summary>
    /// The result classes ID.
    /// </summary>
    public int ReportClassId => UserReportClassId ?? DefaultReportClassId;

    /// <summary>
    /// The default value for the "report class" property provided in the method update file.
    /// 0 = No class reporting
    /// 1..20 the class index in the table.
    /// </summary>
    public int DefaultReportClassId { get; set; }

    /// <summary>
    /// The user-provided value for the "report class" property.
    /// 0 = No class reporting
    /// 1..20 the class index in the method class table.
    /// </summary>
    public int? UserReportClassId { get; set; }

    #endregion

    #region ReportCutOff

    /// <summary>
    /// This test should report cut-off results
    /// Using a cut-off concentration see cref="MethodTest.CutOffConcentration", if the concentration is below the cut-off
    /// the result is one text see cref="MethodReportType.TextBelowCutOff"
    /// and another text if above see cref="MethodReportType.TextAboveCutOff"
    /// </summary>
    public bool ReportCutOff => UserReportCutOff ?? DefaultReportCutOff;

    /// <summary>
    /// The default value for the <see cref="ReportCutOff"/> property provided in the method update file.
    /// </summary>
    public bool DefaultReportCutOff { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="ReportCutOff"/> property.
    /// </summary>
    public bool? UserReportCutOff { get; set; }

    #endregion

    #region ReportQuotient

    /// <summary>
    /// This test should report quotient results, i.e. the quotient of the test's RU divided by the RU of the lowest calibration point
    /// (not always the "lowest").
    /// The quotient can also be expressed in percent <see cref="QuotientReportMode"/>
    ///
    /// Example:
    /// - MethodCalibrators.QuotientIndex for sIgE is 2 => use RU for point CAL-0.35 (not using CAL-0.00), which is 187 RU for the used curve
    /// - Test has Enums.ReportAsFraction.ReportAsPercent
    /// - Test has RU 358
    /// - Test gets quotient result = "191 %"
    /// </summary>
    public QuotientReportMode ReportQuotient => UserReportQuotient ?? DefaultReportQuotient;

    /// <summary>
    /// The default value for the <see cref="ReportQuotient"/> property provided in the method update file.
    /// </summary>
    public QuotientReportMode DefaultReportQuotient { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="ReportQuotient"/> property.
    /// </summary>
    public QuotientReportMode? UserReportQuotient { get; set; }

    #endregion

    #region MethodConcentrationUnit

    /// <summary>
    /// The ID of the concentration unit this test uses.
    /// </summary>
    public int MethodConcentrationUnitId => UserMethodConcentrationUnitId ?? DefaultMethodConcentrationUnitId;

    /// <summary>
    /// The default concentration unit ID provided in the method update file.
    /// </summary>
    public int DefaultMethodConcentrationUnitId { get; set; }

    /// <summary>
    /// The user-provided concentration unit ID.
    /// </summary>
    public int? UserMethodConcentrationUnitId { get; set; }

    #endregion

    /// <summary>
    /// If defined, this is the highest concentration to report.
    /// If the concentration is above this value, the see cref="MethodReportType.TextQuantitativeAbove"
    /// text will be reported instead of a numerical value
    /// </summary>
    public double? HighestConcentrationToReport { get; set; }

    /// <summary>
    /// Number of decimals for presenting a numerical concentration below 10
    /// </summary>
    public int NumberOfDecimalsForConcentrationBelow10 { get; set; }

    /// <summary>
    /// If true, carriers for this test may include a concentration factor, per lot number, that should be
    /// used in calculation of "final" concentration
    /// <seealso cref="ConcentrationFactor"/>
    /// </summary>
    public bool UseLotSpecificCode { get; set; }

    #region DilutionFactor

    /// <summary>
    /// The dilution factor for the test.
    /// </summary>
    public int DilutionFactor => UserDilutionFactor ?? DefaultDilutionFactor;

    /// <summary>
    /// The instrument dilution factor for the test.
    /// </summary>
    public int InstrumentDilutionFactor => DilutionMethod == DilutionMethod.InstrumentDilution ? DilutionFactor : 1;

    /// <summary>
    /// The default value for the <see cref="DilutionFactor"/> property provided in the method update file.
    /// </summary>
    public int DefaultDilutionFactor { get; set; } = 1;

    /// <summary>
    /// The user-provided value for the <see cref="DilutionFactor"/> property.
    /// </summary>
    public int? UserDilutionFactor { get; set; }

    #endregion

    #region CutOffConcentration

    /// <summary>
    /// The cut-off concentration for reporting cut-off results <seealso cref="ReportCutOff"/>
    /// </summary>
    public double? CutOffConcentration => UserCutOffConcentration ?? DefaultCutOffConcentration;

    /// <summary>
    /// The default value for the <see cref="CutOffConcentration"/> property provided in the method update file.
    /// </summary>
    public double? DefaultCutOffConcentration { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="CutOffConcentration"/> property.
    /// </summary>
    public double? UserCutOffConcentration { get; set; }

    #endregion

    /// <summary>
    /// The default value for laboratory-provided test name.
    /// </summary>
    [MaxLength(Constants.LabTestNameLength)]
    public string DefaultLabTestName { get; set; } = default!;

    #region DilutionMethod

    /// <summary>
    /// The dilution method to use for the test.
    /// </summary>
    public DilutionMethod DilutionMethod => UserDilutionMethod ?? DefaultDilutionMethod;

    /// <summary>
    /// The default value for the <see cref="DilutionMethod"/> property provided in the method update file.
    /// </summary>
    public DilutionMethod DefaultDilutionMethod { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="DilutionMethod"/> property.
    /// </summary>
    public DilutionMethod? UserDilutionMethod { get; set; }

    #endregion

    /// <summary>
    /// Identity of the diluent to use for this test.
    /// </summary>
    [MaxLength(DiluentIdentityLength)]
    public string DiluentIdentity { get; set; } = default!;

    #region EmphasizeOnReports

    /// <summary>
    /// "Promote" this test on reports
    /// The test with results will be presented almost as a method in itself on laboratory reports
    /// </summary>
    public bool EmphasizeOnReports => UserEmphasizeOnReports ?? DefaultEmphasizeOnReports;

    /// <summary>
    /// The default value for the <see cref="EmphasizeOnReports"/> property provided in the method update file.
    /// </summary>
    public bool DefaultEmphasizeOnReports { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="EmphasizeOnReports"/> property.
    /// </summary>
    public bool? UserEmphasizeOnReports { get; set; }

    #endregion

    /// <summary>
    /// Text to use for concentrations above the calibration curve
    /// </summary>
    /// <remarks>
    /// The value is not provided in the method update file.
    /// </remarks>
    [MaxLength(TextQuantitativeResultLength)]
    public string? TextQuantitativeAbove { get; set; }

    /// <summary>
    /// Text to use for concentrations below the calibration curve
    /// </summary>
    /// <remarks>
    /// The value is not provided in the method update file.
    /// </remarks>
    [MaxLength(TextQuantitativeResultLength)]
    public string? TextQuantitativeBelow { get; set; }

    #region TextQuantitativeAboveForDilutedTest

    /// <summary>
    /// Text to use for concentrations above the calibration curve, when the sample has been diluted
    /// </summary>
    public string TextQuantitativeAboveForDilutedTest => UserTextQuantitativeAboveForDilutedTest ?? DefaultTextQuantitativeAboveForDilutedTest ?? TextDilutedQuantitativeDefault;

    /// <summary>
    /// The default value for the <see cref="TextQuantitativeAboveForDilutedTest"/> property provided in the method update file.
    /// </summary>
    [MaxLength(TextQuantitativeResultLength)]
    public string? DefaultTextQuantitativeAboveForDilutedTest { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="TextQuantitativeAboveForDilutedTest"/> property.
    /// </summary>
    [MaxLength(TextQuantitativeResultLength)]
    public string? UserTextQuantitativeAboveForDilutedTest { get; set; }

    #endregion

    #region TextQuantitativeBelowForDilutedTest

    /// <summary>
    /// Text to use for concentrations below the calibration curve, when the sample has been diluted
    /// </summary>
    public string TextQuantitativeBelowForDilutedTest => UserTextQuantitativeBelowForDilutedTest ?? DefaultTextQuantitativeBelowForDilutedTest ?? TextDilutedQuantitativeDefault;

    /// <summary>
    /// The default value for the <see cref="TextQuantitativeBelowForDilutedTest"/> property provided in the method update file.
    /// </summary>
    [MaxLength(TextQuantitativeResultLength)]
    public string? DefaultTextQuantitativeBelowForDilutedTest { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="TextQuantitativeBelowForDilutedTest"/> property.
    /// </summary>
    [MaxLength(TextQuantitativeResultLength)]
    public string? UserTextQuantitativeBelowForDilutedTest { get; set; }

    #endregion

    /// <summary>
    /// If set, the calculated concentration is reported even though the concentration is above the curve
    /// (which otherwise would be reported with a text instead, for example "> 100" see cref="MethodReportType.TextQuantitativeAbove")
    /// </summary>
    public bool ReportConcentrationAboveCurveFlag { get; set; }

    /// <summary>
    /// If set, the calculated concentration is reported even though the concentration is below the curve
    /// (which otherwise would be reported with a text instead, for example "below 2.00" see cref="MethodReportType.TextQuantitativeBelow")
    /// </summary>
    public bool ReportConcentrationBelowCurveFlag { get; set; }

    /// <summary>
    /// Number of decimals for presenting a numerical concentration between 10 and 100
    /// </summary>
    public int NumberOfDecimalsForConcentrationBetween10And100 { get; set; }

    /// <summary>
    /// Number of decimals for presenting a numerical concentration above 100
    /// </summary>
    public int NumberOfDecimalsForConcentrationAbove100 { get; set; }

    /// <summary>
    /// Indicated if this test is a mix, i.e. consists of multiple types of allergens
    /// For example
    /// - test "fx73" includes allergens for tests "f26", "f27" and "f83"
    /// </summary>
    public bool MixOfAllergens { get; set; }

    /// <summary>
    /// The test priority parameters.
    /// </summary>
    public MethodTestPriority? Priority { get; set; }

    /// <summary>
    /// See "Phadia Laboratory Systems Data Management Specification (DMS), 303781" in Docan
    /// </summary>
    public bool IterativeAlfaBeta1Beta2Flag { get; set; }

    public double AdjustmentForAlfaBeta1Beta2 { get; set; }

    /// <summary>
    /// Special method name for this test
    /// If the method has <see cref="Method.IsSpecialMethod"/> set, then tests may have a special method name defined, for example "CEGA"
    /// This means that when special method "CEGA" is enabled by the operator, all tests with that special name will be visible and possible to run
    /// </summary>
    [MaxLength(Constants.MethodNameLength)]
    public string? SpecialMethodName { get; set; }

    /// <summary>
    /// The value indicating whether this test was disabled by Thermo Fisher
    /// and cannot be used in the lab.
    /// </summary>
    /// <remarks>
    /// Usually tests are disabled for certain countries.
    /// </remarks>
    public bool Disabled { get; set; }

    /// <summary>
    /// Concentration factor to multiply the concentration value from the calibration curve when the test is processed in a Tetris instrument
    /// (this is to hopefully produce the same results from same sample on different instrument types...)
    /// </summary>
    public double ConcentrationFactor { get; set; }

    /// <summary>
    /// Lower detection limit for Tetris instruments.
    /// This is the lowest possible concentration for this test
    /// </summary>
    public double LowerDetectionLimit { get; set; }

    /// <summary>
    /// Identity of the diluent to use for this test.
    /// </summary>
    [MaxLength(DevelopmentSolutionIdentityLength)]
    public string? DevelopmentSolutionIdentity { get; set; }

    /// <summary>
    /// Indicates if this is a research-use only (RUO) test. 
    /// </summary>
    public bool IsRuo { get; set; }

    /// <summary>
    /// Indicates whether the Specialized test has been activated and is visible and possible to run.
    /// </summary>
    public bool IsSpecializedTestActivated { get; set; }

    /// <summary>
    /// Mark this test as low or high frequency
    /// </summary>
    public UsageFrequency UsageFrequency { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the method this test belongs to
    /// </summary>
    public virtual Method Method { get; set; } = default!;

    /// <summary>
    /// The navigation property for the laboratory's names for this test
    /// </summary>
    public virtual IList<MethodTestLabName> LabTestNames { get; set; } = default!;

    /// <summary>
    /// The navigation property for the calibrators using this test
    /// </summary>
    public virtual IList<MethodCalibrationCurve> CalibrationCurves { get; set; } = default!;

    /// <summary>
    /// The navigation property for the curve controls using this test
    /// </summary>
    public virtual IList<MethodCurveControl> CurveControls { get; set; } = default!;

    /// <summary>
    /// The navigation property for the default concentration unit this test uses.
    /// </summary>
    public virtual MethodConcentrationUnit DefaultConcentrationUnit { get; set; } = default!;

    /// <summary>
    /// The navigation property for the user-provided concentration unit this test uses.
    /// </summary>
    public virtual MethodConcentrationUnit? UserConcentrationUnit { get; set; }

    #endregion

    /// <summary>
    /// Gets the natural sort key for the test, used to enable human-friendly sorting of tests.
    /// </summary>
    [MaxLength(4000)] //Length is set by the NaturalSort function
    public string? TestNameNaturalSortKey { get; }
}

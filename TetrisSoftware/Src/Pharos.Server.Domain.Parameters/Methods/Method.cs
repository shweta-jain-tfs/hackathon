using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

#pragma warning disable CS8618

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// A method is like a recipe for all ingredients, process steps and more needed to
/// produce results for diagnosing allergy or autoimmune diseases
/// </summary>
public class Method : BaseAuditableEntity
{
    public const int FullMethodNameLength = 30;
    public const int LisMethodNameLength = 20;

    /// <summary>
    /// The short name of a method, for example sIgE
    /// </summary>
    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; }

    /// <summary>
    /// Method name used by LIS, default same as MethodName
    /// </summary>
    /// <remarks>
    /// The value is not provided in the method update file.
    /// </remarks>
    [MaxLength(LisMethodNameLength)]
    public string? MethodNameLis { get; set; }

    /// <summary>
    /// Default LIS channel id for this method.
    /// </summary>
    /// <remarks>
    /// The value is not provided in the method update file.
    /// </remarks>
    public int? DefaultLisChannelId { get; set; }

    /// <summary>
    /// The long name of the method, for example ImmunoCap Specific IgE 0-100
    /// </summary>
    [MaxLength(FullMethodNameLength)]
    public string FullMethodName { get; set; }

    /// <summary>
    /// Version of this set of method parameters
    /// The version information is included in MethodUpdate zip files.
    /// Do not "update" a method to a lower version!!!
    /// </summary>
    public int MethodVersion { get; set; }

    /// <summary>
    /// The value indicating whether this method is enabled for use in the lab or not.
    /// </summary>
    /// <remarks>
    /// The value is not provided in the method update file.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Indicates if this is a research-use only (RUO) method. 
    /// </summary>
    public bool IsRuo { get; set; }

    /// <summary>
    /// Indicates whether the Specialized method has been activated and is visible and possible to use.
    /// </summary>
    public bool IsSpecializedMethodActivated { get; set; }

    /// <summary>
    /// Indicates that this is a special method that needs "extra activation" in order to be visible and possible to use.
    /// </summary>
    /// <example>
    /// - Method "View" is a special method is only used in Japan
    /// - Method "IS-E" is a special method that only is used together with Phadia MIA software for ISAC test
    /// </example>
    public bool IsSpecialMethod { get; set; }

    /// <summary>
    /// The value indicating whether this method is a template.
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// The value indicating whether this method is defined by user or shipped by Thermo Fisher.
    /// </summary>
    public bool IsUserDefined { get; set; }

    /// <summary>
    /// The type of technology to use for this method.
    /// This will control the necessary process steps, number of washes, ...
    /// </summary>
    public AssayTechnology Technology { get; set; }

    /// <summary>
    /// The value indicating whether it is possible to enter instrument dilution
    /// for samples, test panels, or reflex tests below values defined in method parameters
    /// </summary>
    public bool PreventDilutionFactorsBelowDefault { get; set; }


    /// <summary>
    /// The order of foil piercing for calibrator and curve control cartridge cells (to avoid carryover).
    /// </summary>
    public CalibratorOrder CalibratorPiercingOrder { get; set; }

    /// <summary>
    /// The pipetting order in which calibrators and curve control cartridge cells are processed (to avoid carryover).
    /// </summary>
    public CalibratorOrder CalibratorPipettingOrder { get; set; }


    /// <summary>
    /// Sort order of this method in a list of methods (when they are displayed in a list).
    /// This value should be used for sorting methods in a list.
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    ///  If not null, this concentration factor will be used as the default value when manually
    ///  adding a <see cref="MethodTest"/> (<seealso cref="MethodTest.ConcentrationFactor"/>
    /// </summary>
    public double DefaultConcentrationFactor { get; set; }


    /// <summary>
    /// If set to true, it is relevant to show a plot of RU values for this method for each Analytical Run
    /// (probably still need to be enabled for make it visible in GUI).
    /// RU plots are for methods that may have a defined baseline, i.e. it is expected to have most RU values
    /// in a certain range, otherwise this may be an indication about something is wrong
    /// (perhaps an elevation of results due to "something", for example bad wash / rinse).
    /// Today I think this only applies for sIgE where "expected many" RU values should be in the interval [0, 30] RU
    /// </summary>
    public bool ShowRuPlot { get; set; }

    /// <summary>
    /// Indicates if it is allowed to use 50% wash concentrate with this method.
    /// </summary>
    public bool Allow50PercentWash { get; set; }

    /// <summary>
    /// Indicates if it is allowed to dilute sample in the pipette.
    /// </summary>
    public bool AllowDilutionInPipette { get; set; }

    /// <summary>
    /// The value indicating whether the blank response must be subtracted from the sample responses.
    /// </summary>
    public bool SubtractBlank { get; set; }

    /// <summary>
    /// The method's calculation parameters.
    /// </summary>
    public virtual MethodAnalyticalRun AnalyticalRun { get; set; }

    /// <summary>
    /// The method's calculation parameters.
    /// </summary>
    public virtual MethodCalculation Calculation { get; set; }

    /// <summary>
    /// The method's presentation parameters.
    /// </summary>
    public virtual MethodPresentation Presentation { get; set; }

    /// <summary>
    /// The method's reporting parameters.
    /// </summary>
    public virtual MethodReporting Reporting { get; set; }

    /// <summary>
    /// Please note that this is not a navigation property.
    /// This is only used for parsing method update files, and then the parsed data will be used for updating the real PhadiaQualityControl / PhadiaQualityControlTest entities in database.
    /// </summary>
    [NotMapped] public List<PhadiaQualityControlTestForParser> PhadiaQualityControlTestsForParser { get; set; } = [];

    #region Navigation properties

    /// <summary>
    /// The navigation property for the method's tests
    /// </summary>
    public virtual IList<MethodTest> Tests { get; set; }

    /// <summary>
    /// The navigation property for the method's reagents
    /// </summary>
    public virtual IList<MethodReagent> Reagents { get; set; }

    /// <summary>
    /// The navigation property for calibration curve
    /// </summary>
    public virtual MethodCalibrationCurve CalibrationCurve { get; set; }

    /// <summary>
    /// The navigation property for the method's reagents
    /// </summary>
    public virtual IList<MethodCurveControl> CurveControls { get; set; }

    /// <summary>
    /// The navigation property for the method's concentration units
    /// For example: kUA/l, PAU/l, ...
    /// </summary>
    public virtual IList<MethodConcentrationUnit> ConcentrationUnits { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public virtual IList<MethodClassTableBelow> ClassTableBelows { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public virtual IList<MethodClassTable> ClassTables { get; set; }

    #endregion
}

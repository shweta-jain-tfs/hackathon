using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Domain;

internal class InstrumentMethod : BaseEntity
{
    public int InstrumentId { get; set; }

    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; } = default!;
    public bool IsActive { get; set; }
    public InstrumentMethodWhenToRun WhenToRun { get; set; }

    #region Curve Control Properties

    /// <summary>
    /// Indicates if a calibrator needs to be run before the next curve controls.
    /// </summary>
    public bool Calibrator { get; set; }

    /// <summary>
    /// Only used when Calibrator is false. 
    /// Determines which option is selected by default: true = "New calibration curve", false = "Curve controls".
    /// </summary>
    public bool ForceCalibrator { get; set; }

    /// <summary>
    /// Conjugate lot number for curve control runs.
    /// </summary>
    [MaxLength(Constants.LotNumberLength)]
    public string ConjugateLotNumber { get; set; } = string.Empty;

    /// <summary>
    /// Carrier (ImmunoCAP) lot number for curve control runs.
    /// </summary>
    [MaxLength(Constants.LotNumberLength)]
    public string CarrierLotNumber { get; set; } = string.Empty;

    #endregion Curve Control Properties

    #region Navigation properties

    public virtual Instrument Instrument { get; set; } = default!;

    #endregion
}
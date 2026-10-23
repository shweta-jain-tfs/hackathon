namespace Pharos.Server.Result.Domain;

/// <summary>
/// Describes how the curve was assigned to an analytical run.
/// </summary>
public enum CurveSource
{
    /// <summary>
    /// No curve is available for this assay run and none has been ordered.
    /// </summary>
    Missing = 0,

    /// <summary>
    /// Curve was automatically propagated from a previous assay's OK curve via CurrentCurve.
    /// Calculations are blocked until a valid curve control is received in this assay run.
    /// </summary>
    CurveFromPreviousAssay = 1,

    /// <summary>
    /// Curve was calibrated within this assay run (same InstrumentRunId).
    /// Calculations can proceed as soon as the curve judgment is OK.
    /// </summary>
    CurveFromCurrentAssay = 2,

    /// <summary>
    /// The user made a deliberate manual choice to trust this curve — either by selecting it
    /// from the curve list, or by explicitly accepting a curve with judgment <see cref="CurveJudgment.Flagged"/>.
    /// Curve controls are not evaluated for this run when selected from the list (no flagged CC involved).
    /// When the user accepted a flagged curve control, the CC chart is still shown with the accepted judgment.
    /// </summary>
    ManuallySelected = 3,
}

namespace Pharos.Server.Result.Domain;

/// <summary>
/// Tracks the calculation readiness and state of an analytical run.
/// Once <see cref="Frozen"/> is reached the state is terminal — it never resets.
/// </summary>
public enum CurveState
{
    /// <summary>
    /// No calculations have been performed yet.
    /// </summary>
    Valid = 0,

    /// <summary>
    /// A curve from a previous assay is assigned but no valid curve control result exists
    /// yet in this run. Cannot calculate concentrations until an OK curve control is received.
    /// </summary>
    WaitingForCurveControl = 1,

    /// <summary>
    /// A new curve has been ordered from the instrument and is pending arrival.
    /// Expected transient state (recoverable automatically when curve arrives or is fitted)
    /// Cannot calculate concentrations until the curve arrives.
    /// </summary>
    WaitingForNewCurve = 2,

    /// <summary>
    /// No valid curve is available (missing or invalid after two flagged curve controls).
    /// Reqiers manual intervention to resolve (new curve assignment, curve control override, or freeze).
    /// Cannot calculate concentrations without manual intervention.
    /// </summary>
    Invalid = 3,

    /// <summary>
    /// The run is committed to calculating. This state is terminal: no new curve or
    /// curve control event can alter the calculation path. The run will keep calculating
    /// all subsequent results automatically.
    /// </summary>
    Frozen = 4
}
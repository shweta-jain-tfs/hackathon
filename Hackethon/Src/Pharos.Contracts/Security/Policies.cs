namespace Pharos.Contracts.Security;

/// <summary>
/// Lists all authorization policies available in the application.
/// </summary>
public static class Policies
{
    /// <summary>
    /// The request marked with policy can be only called from the instrument computer.
    /// </summary>
    public const string InstrumentOnly = "instrument_only";

    /// <summary>
    /// The request marked with policy can be only called from the instrument computer
    /// but requires no authenticated user.
    /// For example get status of the current instrument from the "lock screen".
    /// </summary>
    public const string InstrumentOnlyAllowAnonymous = "instrument_only.anonymous";

    /// <summary>
    /// Functionality marked with this policy is available only when RnD mode is enabled in Pharos.
    /// </summary>
    public const string RnD = "rnd";

    /// <summary>
    /// Functionality marked with this policy is available only when RUO mode is enabled in Pharos.
    /// </summary>
    public const string Ruo = "ruo";

    /// <summary>
    /// Functionality marked with this policy is available only to service engineers or when RnD mode is enabled in Pharos.
    /// </summary>
    public const string ServiceEngineerOrRndMode = "service_engineer_or_rnd";
}

namespace Pharos.Server.Application.AuditTrail.Enums;
public enum Category
{
    /// <summary>
    /// Authentication and Authorization events
    /// </summary>
    AuthenticationAndAuthorization = 0,

    /// <summary>
    /// Patient's personal and health information events
    /// </summary>
    PatientInformation = 1,

    /// <summary>
    /// System configuration and settings events
    /// </summary>
    SystemConfiguration = 2,

    /// <summary>
    /// Sample management events
    /// </summary>
    Sample = 3,

    /// <summary>
    /// Result management events
    /// </summary>
    Result = 4,

    /// <summary>
    /// Instrument events
    /// </summary>
    Instrument = 5,

    /// <summary>
    /// System errors
    /// </summary>
    SystemError = 6

    // TODO: More categories to be added as needed
}

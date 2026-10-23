namespace Pharos.Server.Request.Domain;

/// <summary>
/// Defines the source from which a request was created.
/// </summary>
public enum RequestSource
{
    /// <summary>
    /// The request was manually created by the user through the UI.
    /// </summary>
    Manual = 1,

    /// <summary>
    /// The request was received from the Laboratory Information System (LIS).
    /// </summary>
    Lis = 2,

    /// <summary>
    /// The request was imported by the user from a file through the UI.
    /// </summary>
    Import = 3,

    /// <summary>
    /// The request was created by the Request Generator workflow.
    /// </summary>
    Generator = 4
}

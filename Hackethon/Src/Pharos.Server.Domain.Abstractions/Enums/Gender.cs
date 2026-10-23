namespace Pharos.Server.Domain.Enums;

/// <summary>
/// Lists available gender options in patient information.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum Gender
{
    /// <summary>
    /// The patient sex is not set.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// Male patient.
    /// </summary>
    Male = 1,

    /// <summary>
    /// Female patient.
    /// </summary>
    Female = 2
}
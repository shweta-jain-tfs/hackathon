namespace Pharos.Server.Domain.Enums;

/// <summary>
/// Specifies instrument models supported by Pharos.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum InstrumentType
{
    Unknown = 0,

    /// <summary>
    /// Phadia 2.0 Small instrument.
    /// </summary>
    Small = 1,

    /// <summary>
    /// Phadia 2.0 Medium instrument.
    /// </summary>
    Medium = 2
}

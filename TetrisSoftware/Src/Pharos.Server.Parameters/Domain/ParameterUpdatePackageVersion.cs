namespace Pharos.Server.Parameters.Domain;

/// <summary>
/// Represents the last successfully imported version of the parameter update package.
/// </summary>
internal class ParameterUpdatePackageVersion
{
    public int VersionNumber { get; set; }
    public DateTimeOffset? ImportDate { get; set; }
}
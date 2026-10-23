namespace Pharos.Server.Domain.Enums;


/// <summary>
/// List assay technologies supported by the system.
/// </summary>
/// <remarks>
/// IMPORTANT: Do not change the numbers, because they are taken from the method parameters file spec.
/// </remarks>
public enum AssayTechnology
{
    ImmunoCap = 0,
    EliA = 1,
    ImmunoCapIgG = 2,
    ViewAllergy = 5,
}

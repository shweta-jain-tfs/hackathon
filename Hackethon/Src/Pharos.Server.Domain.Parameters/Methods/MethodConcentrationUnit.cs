using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// The concentration units to use for a method.
/// Each method test reference one of these units
/// A method may have up to 10 different concentration units
/// </summary>
public class MethodConcentrationUnit
{
    public const int ConcentrationUnitMaxLength = 5;

    /// <summary>
    /// The ID of the method that this concentration unit belongs to
    /// </summary>
    public int MethodId { get; set; }

    /// <summary>
    /// Concentration unit id, 1-10
    /// </summary>
    public int UnitId { get; set; }

    /// <summary>
    /// The concentration unit text, for example kUA/l
    /// </summary>
    [MaxLength(ConcentrationUnitMaxLength)]
    public string ConcentrationUnit { get; set; } = default!;

    #region Navigation properties

    /// <summary>
    /// The navigation property for the method that this concentration unit belongs to
    /// </summary>
    public virtual Method Method { get; set; } = default!;

    /// <summary>
    /// The navigation property for the tests that are using this concentration unit as their default unit.
    /// </summary>
    public virtual IList<MethodTest> DefaultMethodTests { get; set; } = default!;

    /// <summary>
    /// The navigation property for the tests that are using this concentration unit as their user-provided unit.
    /// </summary>
    public virtual IList<MethodTest> UserMethodTests { get; set; } = default!;

    #endregion
}
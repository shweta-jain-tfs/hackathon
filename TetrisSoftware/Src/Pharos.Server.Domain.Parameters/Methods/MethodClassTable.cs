#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// This class represents one row in a result class table
/// It includes a concentration value that, together with the other rows for this method + class table id,
/// gives concentration ranges. Concentration results for a patient test shall find the matching concentration range to
/// produce a result text. <seealso cref="MethodClassTableBelow"/>
///
/// Example (with fictive texts):
/// Method Class table Row Concentration Text when above
/// sIgE   1           1   0,35          "Above 0,35 but below 0,70"
/// sIgE   1           2   0,70          "Above 0,70 but below 3,50"
/// ...
/// </summary>
public class MethodClassTable : BaseAuditableEntity
{
    /// <summary>
    /// The ID of the method that this method class table row belongs to
    /// </summary>
    public int MethodId { get; set; }

    /// <summary>
    /// Class table id (1-20)
    /// </summary>
    public int ClassTableId { get; set; }

    /// <summary>
    /// Class table row id in this class table (1-10)
    /// </summary>
    public int ClassTableRowId { get; set; }

    /// <summary>
    /// Lowest concentration to be "inside" this concentration range
    /// </summary>
    public double Concentration { get; set; }

    /// <summary>
    /// Class result text to use when inside this concentration range
    /// </summary>
    public string? TextAboveConcentration => UserTextAboveConcentration ?? DefaultTextAboveConcentration;

    /// <summary>
    /// The default value for the <see cref="TextAboveConcentration"/> property provided in the method update file.
    /// </summary>
    [MaxLength(MethodReporting.ClassResultTextLength)]
    public string? DefaultTextAboveConcentration { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="TextAboveConcentration"/> property.
    /// </summary>
    [MaxLength(MethodReporting.ClassResultTextLength)]
    public string? UserTextAboveConcentration { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the method this class table belongs to
    /// </summary>
    public virtual Method Method { get; set; }

    #endregion
}
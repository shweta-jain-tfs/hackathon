#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// This class is used together with <see cref="MethodClassTable"/> (which includes a number of "upper limit" concentration limits)
/// When the patient test concentration is below all <see cref="MethodClassTable.Concentration"/> for this class table id,
/// the TextBelowConcentration text is used as patient test class result text
/// </summary>
public class MethodClassTableBelow : BaseAuditableEntity
{
    /// <summary>
    /// The ID of the method that this method class table below belongs to
    /// </summary>
    public int MethodId { get; set; }

    /// <summary>
    /// Class table id (1-20)
    /// </summary>
    public int ClassTableId { get; set; }

    /// <summary>
    /// Class result text to use when patient test concentration is below all <see cref="MethodClassTable.Concentration"/>
    /// for current method and class table id
    /// </summary>
    public string? TextBelowConcentration => UserTextBelowConcentration ?? DefaultTextBelowConcentration;

    /// <summary>
    /// The default value for the <see cref="TextBelowConcentration"/> property provided in the method update file.
    /// </summary>
    [MaxLength(MethodReporting.ClassResultTextLength)]
    public string? DefaultTextBelowConcentration { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="TextBelowConcentration"/> property.
    /// </summary>
    [MaxLength(MethodReporting.ClassResultTextLength)]
    public string? UserTextBelowConcentration { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the method this class table below belongs to
    /// </summary>
    public virtual Method Method { get; set; }

    #endregion
}
using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Parameters.Methods;

#pragma warning disable CS8618

namespace Pharos.Server.Domain.Parameters;

/// <summary>
/// A test panel is the short way of selecting a number of <see cref="MethodTest"/>
/// </summary>
public class Panel : BaseAuditableEntity
{
    public const int PanelNameLength = 16;
    public const int PanelLongNameLength = 30;

    /// <summary>
    /// Name of the panel, used for input / selecting the panel.
    /// </summary>
    /// <remarks>
    /// For example: "pcd"
    /// </remarks>
    [MaxLength(Panel.PanelNameLength)]
    public string PanelName { get; set; }

    /// <summary>
    /// Long name of the panel, includes a more describing text about the panel.
    /// </summary>
    /// <remarks>
    /// For example: "Celiac Disease"
    /// </remarks>
    [MaxLength(Panel.PanelLongNameLength)]
    public string PanelLongName { get; set; }

    /// <summary>
    /// A grouped panel is almost treated as a method on reports.
    /// For example on Laboratory report:
    /// - One chapter with header "Celiac Disease" and a list of the results for the tests included in the panel
    /// - One chapter per "real" method with header, f.ex. "ImmunoCap Specific IgE 0-100", with list of results from the method but not included in the panel
    /// </summary>
    public bool GroupedPanel { get; set; }

    /// <summary>
    /// The value indicating whether this panel is enabled for use in the lab or not.
    /// </summary>
    /// <remarks>
    /// The value is not provided in the method update file
    /// </remarks>
    public bool Enabled { get; set; } = true;

    #region Navigation properties

    /// <summary>
    /// The navigation property for the panel's tests
    /// </summary>
    public virtual IList<PanelTest> Tests { get; set; }

    #endregion
}
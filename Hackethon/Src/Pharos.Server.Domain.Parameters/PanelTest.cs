#pragma warning disable CS8618

using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Domain.Parameters;

/// <summary>
/// One test included in a test panel <see cref="Panel"/>
/// A test panel is the short way of selecting a number of <see cref="MethodTest"/>
/// </summary>
public class PanelTest : BaseAuditableEntity
{
    /// <summary>
    /// The ID for the panel test belongs to
    /// </summary>
    public int PanelId { get; set; }

    /// <summary>
    /// The ID of the method test to use
    /// </summary>
    public int MethodTestId { get; set; }


    /// <summary>
    /// When testing this one, repeat it a number of times
    /// </summary>
    public int Replicates { get; set; }

    /// <summary>
    /// The instrument dilution factor to use when this panel test is added to a request.
    /// </summary>
    public int InstrumentDilutionFactor { get; set; }

    /// <summary>
    /// Listing order of the panel tests
    /// </summary>
    public int? SortOrder { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the panel this panel test belongs to
    /// </summary>
    public virtual Panel Panel { get; set; }

    /// <summary>
    /// The navigation property for the method test this panel test uses
    /// </summary>
    public virtual MethodTest MethodTest { get; set; }

    #endregion
}

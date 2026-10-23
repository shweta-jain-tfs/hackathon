using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

#pragma warning disable CS8618

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// Each method test may be called something else in the laboratory
/// </summary>
public class MethodTestLabName : BaseAuditableEntity
{
    /// <summary>
    /// The ID for the method test this laboratory test belongs to
    /// </summary>
    public int MethodTestId { get; set; }

    /// <summary>
    /// The laboratory's name of our method test
    /// </summary>
    [MaxLength(Constants.LabTestNameLength)]
    public string TestName { get; set; }

    /// <summary>
    /// Listing order of the laboratory test name (in case there are multiple ones for one MethodTest)
    /// </summary>
    public int SortOrder { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the method test this laboratory test belongs to
    /// </summary>
    public virtual MethodTest MethodTest { get; set; }

    #endregion

    /// <summary>
    /// Gets the natural sort key for the test, used to enable human-friendly sorting of tests.
    /// </summary>
    [MaxLength(4000)] //Length is set by the NaturalSort function
    public string? TestNameNaturalSortKey { get; }
}
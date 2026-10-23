using Pharos.Server.Domain.Common;

#pragma warning disable CS8618

namespace Pharos.Server.Domain.Parameters;

/// <summary>
/// 
/// </summary>
public class PackageItem : BaseAuditableEntity
{
    /// <summary>
    /// The ID of the package this package content belongs to
    /// </summary>
    public int PackageId { get; set; }

    /// <summary>
    /// The ID of the article this package content consist of
    /// </summary>
    public int ArticleId { get; set; }

    /// <summary>
    /// Number of articles
    /// </summary>
    public int ArticleAmount { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property to the referenced article
    /// </summary>
    public virtual Article Article { get; set; }

    /// <summary>
    /// The navigation property to the package
    /// </summary>
    public virtual Package Package { get; set; }

    #endregion

}
using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Parameters.Methods;

#pragma warning disable CS8618
public abstract class BaseQualityControl : BaseAuditableEntity
{
    /// <summary>
    /// Name of the quality control, i.e. the QC liquid
    /// </summary>
    [MaxLength(Constants.QualityControlNameLength)]
    public string Name { get; set; }

    public int TubeTypeId { get; set; }

    public QualityControlSystemType SystemType { get; set; }

    public virtual IList<BaseQualityControlTest> Tests { get; set; }
}

using System.Text.Json.Serialization;
using Pharos.Server.Domain;

#pragma warning disable CS8618

namespace Pharos.Server.Result.Domain;

public class CurrentCurve : BaseAuditableEntity
{
    public int CurveId { get; set; }

    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; }

    [MaxLength(Constants.LotNumberLength)]
    public string? ConjugateLotNumber { get; set; }

    [MaxLength(Constants.LotNumberLength)]
    public string? ImmunoCapLotNumber { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the curve that is selected as current
    /// </summary>
    [JsonIgnore]
    public virtual Curve Curve { get; set; }

    #endregion
}

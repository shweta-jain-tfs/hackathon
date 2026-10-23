namespace Pharos.Server.Result.Domain;

public class BlankReplicate : BaseEntity
{
    public int BlankId { get; set; }

    public DateTimeOffset MeasurementTime { get; set; }

    /// <summary>
    /// The fluorometer measurements for this replicate.
    /// </summary>
    public double FluorometerValue { get; set; }

    #region Navigation properties

    public virtual IList<BlankReplicateError> BlankReplicateErrors { get; set; } = default!;

    #endregion

}

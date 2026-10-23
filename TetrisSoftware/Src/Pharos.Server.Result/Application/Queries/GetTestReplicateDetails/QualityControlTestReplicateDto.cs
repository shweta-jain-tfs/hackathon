namespace Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;

public class QualityControlTestReplicateDto
{
    public int Id { get; set; }

    public int ReplicateNumber { get; set; }

    /// <summary>
    /// The formatted version of the Response value
    /// </summary>
    public string? ResponseAsText { get; set; }

    /// <summary>
    /// The formatted version of the Concentration value.
    /// Also depends on ConcentrationStatus
    /// </summary>
    public string? ConcentrationAsText { get; set; }

    public bool IsAborted { get; set; }
    
    public DateTimeOffset MeasurementDateTime { get; set; }
    
    public bool HasError { get; set; }
}
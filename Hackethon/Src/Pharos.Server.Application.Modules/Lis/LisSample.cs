namespace Pharos.Server.Application.Lis;

public class LisSample
{
    /// <summary>
    /// The sample ID set by the lab.
    /// </summary>
    public string SampleId { get; set; } = string.Empty;

    /// <summary>
    /// The processing number assigned to one or more test orders (for the same patient)
    /// by the laboratory. Pharos returns this field unchanged to LIS.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// The date when the request to run a test was ordered by a lab.
    /// </summary>
    public DateTimeOffset? RequestDate { get; set; }

    /// <summary>
    /// The date when the sample was collected from a patient.
    /// </summary>
    public DateTimeOffset? SampleCollectionDate { get; set; }

    /// <summary>
    /// The information about the person that has ordered the tests for this sample.
    /// </summary>
    public LisRequestor? Requestor { get; set; }

    /// <summary>
    /// The type of the tube used for the sample.
    /// </summary>
    public string TubeType { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the sample is created locally in Pharos
    /// </summary>
    public bool IsSampleLocallyCreated { get; set; }

    /// <summary>
    /// The list of test orders for the sample.
    /// </summary>
    public List<LisTestOrder> TestOrders { get; set; } = [];
}
namespace Pharos.Server.Application.Lis;

/// <summary>
/// Represents the result for a patient sample test order.
/// </summary>
public record LisSampleTestResult : LisTestResult
{
    /// <summary>
    /// The unique ID of the test order in Pharos (included here for synchronization purposes).
    /// </summary>
    public Guid TestOrderId { get; set; }

    /// <summary>
    /// The final result is still not calculated or not approved.
    /// </summary>
    public bool Pending { get; set; }

    /// <summary>
    /// Indicates that the result has an error.
    /// </summary>
    public bool HasError { get; set; }

    public string? Class { get; set; }

    public string? CutOff { get; set; }
    public string? Quotient { get; set; }

    /// <summary>
    /// The user that has approved the test result.
    /// </summary>
    public string? ApprovedByUser { get; set; }
}

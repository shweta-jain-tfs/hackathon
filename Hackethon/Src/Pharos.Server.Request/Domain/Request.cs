using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Entities;

#pragma warning disable CS8618
namespace Pharos.Server.Request.Domain;

/// <summary>
/// Represents a request
/// </summary>
internal class Request : BaseAuditableEntity
{
    public const int RequestIdLength = 32;

    /// <summary>
    /// The sample ID set by the lab or entered by the user manually.
    /// It can be non-unique (<see cref="BaseAuditableEntity.Created"/> property must be used for unique sample identification).
    /// </summary>
    [MaxLength(Constants.SampleIdMaxLength)]
    public string SampleId { get; set; }

    /// <summary>
    /// The ID of the patient to whom the request belongs.
    /// </summary>
    public int? PatientId { get; set; }

    /// <summary>
    /// The tube type of the sample.
    /// </summary>
    public int? SampleTubeType { get; set; }

    /// <summary>
    /// Number of times (integer) the sample was diluted by the lab before testing.
    /// The number less than 2 means that the sample was not diluted.
    /// </summary>
    public int PreDilutionFactor { get; set; } = 1;

    /// <summary>
    /// The processing number assigned to one or more test orders (for the same patient)
    /// by the laboratory. Pharos returns this field unchanged to LIS.
    /// </summary>
    [MaxLength(RequestIdLength)]
    public string? RequestId { get; set; }

    /// <summary>
    /// The date when the request to run a test was ordered by a lab.
    /// </summary>
    public DateTimeOffset? RequestDate { get; set; }

    /// <summary>
    /// The high priority flag for a sample.
    /// </summary>
    public bool HighPriority { get; set; }

    /// <summary>
    /// The ID of the person or a lab that has ordered the test.
    /// </summary>
    public int? RequestorId { get; set; }

    /// <summary>
    /// The patient's age at the moment where the sample was taken.
    /// </summary>
    [ProtectedData]
    public int? PatientAge { get; set; }

    /// <summary>
    /// The date when the sample was collected from a patient.
    /// </summary>
    public DateTimeOffset? SampleCollectionDate { get; set; }

    /// <summary>
    /// This sample has an error (either the sample itself or at least one of the test orders)
    /// </summary>
    public bool ErrorFlag { get; set; }

    /// <summary>
    /// Display state for the sample's test orders (used in UI)
    /// </summary>
    public RequestState RequestState { get; set; } = RequestState.NotStarted;

    /// <summary>
    /// Volume Check behavior for a sample.
    /// </summary>
    public SampleVolumeCheck SampleVolumeCheck { get; set; }

    /// <summary>
    /// Indicates whether the sample has been counted for the requestors reported samples count.
    /// </summary>
    public bool IsCountedInRequestor { get; set; }

    /// <summary>
    /// The source from which the request was created (manual UI entry, LIS or file import).
    /// </summary>
    public RequestSource Source { get; set; }

    public RequestUid GetRequestUid()
    {
        return new RequestUid
        {
            SampleId = SampleId,
            RequestCreatedDate = Created
        };
    }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the related patient.
    /// </summary>
    public virtual Patient? Patient { get; set; }

    /// <summary>
    /// The navigation property for the related test orders.
    /// </summary>
    public virtual IList<TestOrder> TestOrders { get; set; } = new List<TestOrder>();

    /// <summary>
    /// The navigation property for the related requestor.
    /// </summary>
    public virtual Requestor? Requestor { get; set; }

    public virtual IList<RequestIssue> Issues { get; set; } = new List<RequestIssue>();

    #endregion

    /// <summary>
    /// Calculates the aggregate request state from its test orders and updates the <see cref="RequestState"/> property.
    /// </summary>
    /// <returns></returns>
    public void UpdateRequestState()
    {
        RequestState = RequestStateCalculator.CalculateState(this);
    }

    /// <summary>
    /// Checks if this request has an unfinished test order identified by the specified method and test names.
    /// </summary>
    public bool HasUnfinishedTestOrder(string methodName, string testName)
    {
        return TestOrders.Any(to => to.TestName == testName
                                     && to.MethodName == methodName
                                     && to.State is TestOrderState.NotStarted or TestOrderState.Scheduled or TestOrderState.UnableToStart or TestOrderState.Started or TestOrderState.Pipetted);
    }

    /// <summary>
    /// Checks if this request has any test order identified by the specified method and test names.
    /// </summary>
    public bool HasTestOrder(string methodName, string testName)
    {
        return TestOrders.Any(to => to.TestName == testName && to.MethodName == methodName);
    }

    /// <summary>
    /// Gets the first test order matching the specified method and test names that is in NotStarted state.
    /// Returns null if no matching NotStarted test is found.
    /// </summary>
    /// <param name="methodName">The name of the method to match.</param>
    /// <param name="testName">The name of the test to match.</param>
    /// <returns>A NotStarted test order matching the criteria, or null if none exists.</returns>
    public TestOrder? GetNotStartedTest(string methodName, string testName)
    {
        return TestOrders.FirstOrDefault(to => to.TestName == testName && to.MethodName == methodName && to.State == TestOrderState.NotStarted);
    }
}
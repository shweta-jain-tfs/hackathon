using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;
using EntityState = Pharos.Server.Application.Request.EntityState;

#pragma warning disable CS8618

namespace Pharos.Server.Request.Domain;

/// <summary>
/// Represent what test to run on a specific sample (collected from a patient).
/// </summary>
internal class TestOrder : BaseAuditableEntity, ITestOrder
{
    /// <summary>
    /// The globally unique test order ID which is shared with the instrument.
    /// </summary>
    public Guid Uid { get; set; }

    /// <summary>
    /// The processing status of the order.
    /// </summary>
    public TestOrderState State { get; set; } = TestOrderState.NotStarted;

    /// <summary>
    /// Indicates whether the test order is active. Inactive test orders are not sent by instruments.
    /// By default, the test order is active when created. The lab personnel can set it to false if they
    /// want to temporarily suspend the processing of this test.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The name of an assay method that must be used for the test run.
    /// </summary>
    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; }

    /// <summary>
    /// The name of the test (within the assay method) that must be used for the run.
    /// </summary>
    [MaxLength(Constants.TestNameLength)]
    public string TestName { get; set; }

    /// <summary>
    /// The requested number of replicates for the test. The result for the test order
    /// will the mean response value across all replicates.
    /// </summary>
    public int NumberOfReplicates { get; set; } = 1;

    /// <summary>
    /// Number of times (integer) the instrument should dilute for this test.
    /// A value of 1 represents “no dilution”.
    /// </summary>
    public int InstrumentDilutionFactor { get; set; } = 1;

    /// <summary>
    /// The serial number of the instrument  where the order has been processed.
    /// </summary>
    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public virtual string? InstrumentNumber { get; set; }

    /// <summary>
    /// The ID of the request to run the test on. !!!This is not the SampleId but the ID for the request
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// The custom test name (within a method) defined by a laboratory.
    /// </summary>
    /// <remarks>
    /// Labs can define their own names (more than one) for the test. We need to
    /// remember the exact test name sent by LIS so that we can send it back.
    /// </remarks>
    [MaxLength(Constants.LabTestNameLength)]
    public string? LabTestName { get; set; }

    /// <summary>
    /// The order in which the test must be executed on a particular sample
    /// (if there is more than one for a sample).
    /// </summary>
    /// <remarks>
    /// If the patient sample may not
    /// have enough liquid to perform all prescribed tests, the doctor can set
    /// the test order so that more important tests are executed first.
    /// </remarks>
    public int TestSortOrder { get; set; }

    /// <summary>
    /// The ID of the LIS channel from which the order had come.
    /// </summary>
    public int? LisChannelId { get; set; }

    /// <summary>
    /// Contains the Ids of the panels that this test order is connected to.
    /// </summary>
    public List<int> TestPanelIds { get; set; } = [];

    /// <summary>
    /// The tube id that this test order is assigned to.
    /// </summary>
    [MaxLength(Constants.TubeIdMaxLength)]
    public string? TubeId { get; set; }

    /// <summary>
    /// Shows if the test has been added through a reflex condition.
    /// </summary>
    public bool IsReflexTest { get; set; }

    public bool HasReflexTests => ReflexDefinitions is { Count: > 0 };

    [NotMapped]
    public EntityState EntityState { get; set; }

    #region Navigation properties

    /// <summary>
    /// The request navigation property
    /// </summary>
    [JsonIgnore]
    public virtual Request Request { get; set; }

    /// <summary>
    /// The test order custom parameter navigation property
    /// </summary>
    public virtual TestOrderCustomParameter? TestOrderCustomParameters { get; set; }

    /// <summary>
    /// The navigation property for reflex definitions associated with the test.
    /// </summary>
    public virtual IList<ReflexDefinition>? ReflexDefinitions { get; set; }

    #endregion
}
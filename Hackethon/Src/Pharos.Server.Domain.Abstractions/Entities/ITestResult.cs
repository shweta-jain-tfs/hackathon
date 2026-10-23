using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Domain.Entities;

/// <summary>
/// An abstraction for a result of the test on a patient sample.
/// </summary>
public interface ITestResult
{
    /// <summary>
    /// The database identifier of the test result.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// The ID of the test order to which this result belongs.
    /// </summary>
    Guid TestOrderId { get; set; }

    /// <summary>
    /// The name of the test (within the assay method) that must be used for the run.
    /// </summary>
    string TestName { get; set; }

    /// <summary>
    /// The name of an assay method that must be used for the test run.
    /// </summary>
    string MethodName { get; set; }

    /// <summary>
    /// The test result status.
    /// </summary>
    public TestResultStatus Status { get; set; }

    /// <summary>
    /// Average concentration value for the result's replicates
    /// </summary>
    double? MeanResponse { get; set; }

    /// <summary>
    /// Average concentration value for the result's replicates
    /// </summary>
    double? MeanConcentration { get; set; }

    /// <summary>
    /// Average concentration value without dilution factors for the result's replicates
    /// </summary>
    double? MeanConcentrationRaw { get; }

    string? MeanClass { get; set; }

    string? MeanCutOff { get; set; }

    public double? MeanQuotient { get; set; }

    /// <summary>
    /// Is the result approved or not...
    /// </summary>
    public ResultApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// The sample identifier for sample results. Non-sample results return null.
    /// </summary>
    public string? SampleId => null;
}

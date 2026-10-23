using MediatR;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// Use this command to send test results to LIS.
/// </summary>
/// <param name="LisChannelId">
/// The ID of the LIS channel from which the test orders were received.
/// </param>
/// <param name="Patients">
/// The list of patients with samples and test results. See remarks for more information.
/// </param>
/// <param name="TestOrderIds">
/// The list of test order IDs associated with this message. Used for tracking and event publishing.
/// </param>
/// <param name="IsManualExport">
/// Indicates if this is a manual export triggered by user action (true) or automatic export (false).
/// </param>
/// <remarks>
/// Every <see cref="LisPatient"/> object must represent a unique (for this request) patient.
/// Every patient must have be linked with, at least, one sample and one test result.
/// Test results that do not have patient information are combined under s single patient object.
/// </remarks>
public sealed record SendSampleTestResultsToLisCommand(
    int? LisChannelId,
    IReadOnlyList<LisPatient> Patients,
    IReadOnlyList<Guid> TestOrderIds,
    bool IsManualExport = false) : IRequest;

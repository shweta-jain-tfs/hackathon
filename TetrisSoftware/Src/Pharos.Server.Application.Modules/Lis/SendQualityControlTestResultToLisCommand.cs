using MediatR;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// Sends quality control test result to LIS.
/// </summary>
/// <param name="LisChannelId">The LIS channel ID the result is sent to.</param>
/// <param name="QcTestResult">The quality control test result.</param>
/// <param name="IsManualExport">True when triggered by a user action.</param>
public sealed record SendQualityControlTestResultToLisCommand(
    int? LisChannelId,
    LisQualityControlTestResult QcTestResult,
    bool IsManualExport = false) : IRequest;

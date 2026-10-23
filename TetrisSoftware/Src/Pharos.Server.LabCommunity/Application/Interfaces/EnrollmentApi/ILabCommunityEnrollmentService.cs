using Pharos.Server.Application.Settings.LabCommunity;

namespace Pharos.Server.LabCommunity.Application.Interfaces.EnrollmentApi;

internal interface ILabCommunityEnrollmentService
{
    /// <summary>
    /// Calls the LabCommunity enrollment API to exchange credentials for an API key.
    /// </summary>
    Task<LabCommunityEnrollmentResult> EnrollPharos(LabCommunitySettings settings, string deviceId, CancellationToken cancellationToken = default);
}

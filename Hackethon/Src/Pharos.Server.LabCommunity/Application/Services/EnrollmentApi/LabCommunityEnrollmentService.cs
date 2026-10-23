using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces.EnrollmentApi;

namespace Pharos.Server.LabCommunity.Application.Services.EnrollmentApi;

internal sealed class LabCommunityEnrollmentService(
    ILabCommunityApiClient client,
    ILogger<LabCommunityEnrollmentService> logger) : ILabCommunityEnrollmentService
{
    public async Task<LabCommunityEnrollmentResult> EnrollPharos(
        LabCommunitySettings settings,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var request = new EnrollmentRequest
        {
            Product = OperatorSoftwareProduct.Pharos,
            LaboratoryCode = settings.LaboratoryCode,
            Username = settings.Username,
            Password = settings.Password,
            DeviceId = deviceId
        };

        try
        {
            var response = await client.EnrollAsync(request, cancellationToken);

            return string.IsNullOrEmpty(response.ApiKey) ? LabCommunityEnrollmentResult.Failed() : LabCommunityEnrollmentResult.Enrolled(response.ApiKey);
        }
        catch (ApiException ex)
        {
            var result = LabCommunityEnrollmentResult.Failed();
            if (ex.StatusCode == 401)
            {
                result = LabCommunityEnrollmentResult.InvalidCredentials();
            }
            else if (ex.StatusCode == 409)
            {
                result = LabCommunityEnrollmentResult.AlreadyEnrolled();
            }

            logger.LogWarning(
                "LabCommunity enrollment failed with status {StatusCode}: {Error}",
                ex.StatusCode, ex.Response);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while calling the LabCommunity enrollment API.");
            return LabCommunityEnrollmentResult.Failed();
        }
    }
}

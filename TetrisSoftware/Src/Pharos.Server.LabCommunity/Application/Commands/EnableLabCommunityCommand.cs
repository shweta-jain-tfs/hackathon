using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.EnrollmentApi;
using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Commands;

internal sealed record EnableLabCommunityCommand : IRequest<bool>;  // Returns true if LabCommunity was enabled, false otherwise.

internal sealed class EnableLabCommunityCommandHandler(
    ILabCommunityDbContext dbContext,
    ILabCommunityEnrollmentService enrollmentService,
    IOptionsSnapshot<LabCommunitySettings> settingsSnapshot,
    ILabCommunityConnectionHub connectionHub,
    ILogger<EnableLabCommunityCommandHandler> logger) : IRequestHandler<EnableLabCommunityCommand, bool>
{
    public async Task<bool> Handle(EnableLabCommunityCommand request, CancellationToken cancellationToken)
    {
        var settings = settingsSnapshot.Value;

        var existingEnrollment = await dbContext.GetActiveEnrollmentAsync(settings.LaboratoryCode, cancellationToken);

        var deviceId = existingEnrollment?.DeviceId ?? Guid.NewGuid().ToString("D");
        var enrollmentResult = await enrollmentService.EnrollPharos(settings, deviceId, cancellationToken);

        if (enrollmentResult.Status == LabCommunityEnrollmentStatus.Enrolled && string.IsNullOrWhiteSpace(enrollmentResult.ApiKey))
        {
            enrollmentResult = LabCommunityEnrollmentResult.Failed();
        }

        var labCommunityEnabled = false;

        switch (enrollmentResult.Status)
        {
            case LabCommunityEnrollmentStatus.InvalidCredentials:
                logger.LogWarning("Enrollment failed. Configured credentials are invalid.");

                if (existingEnrollment is not null)
                {
                    existingEnrollment.ValidCredentials = false;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                break;
            case LabCommunityEnrollmentStatus.AlreadyEnrolled:
                logger.LogWarning("Enrollment failed. Device is already enrolled.");

                if (existingEnrollment is not null)
                {
                    existingEnrollment.ValidCredentials = true;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                labCommunityEnabled = true;

                break;
            case LabCommunityEnrollmentStatus.Failed:
                logger.LogWarning("Enrollment failed. No API key was returned.");

                break;
            case LabCommunityEnrollmentStatus.Enrolled:
                var enrollment = new Enrollment
                {
                    LaboratoryCode = settings.LaboratoryCode,
                    DeviceId = deviceId,
                    ApiKey = enrollmentResult.ApiKey ?? string.Empty,
                    ValidCredentials = true
                };

                dbContext.Enrollments.Add(enrollment);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Successfully enrolled device {DeviceId} in laboratory {LaboratoryCode}.", deviceId, settings.LaboratoryCode);

                labCommunityEnabled = true;

                break;
            default:
                throw new ArgumentOutOfRangeException(enrollmentResult.Status.ToString(), enrollmentResult.Status, "Unexpected enrollment status.");
        }

        await connectionHub.LabCommunityStatusChanged();

        return labCommunityEnabled;
    }
}

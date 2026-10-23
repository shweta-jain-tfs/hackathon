using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.CheckIn;

namespace Pharos.Server.LabCommunity.Application.Commands;

internal sealed record CheckInToLabCommunityCommand : IRequest;

internal sealed class CheckInToLabCommunityCommandHandler(
    ILabCommunityDbContext dbContext,
    ILabCommunityCheckInService checkInService,
    ILabCommunityCheckInState checkInState,
    IOptionsSnapshot<LabCommunitySettings> settingsSnapshot,
    IDateTime dateTime,
    ILabCommunityConnectionHub connectionHub,
    ILogger<CheckInToLabCommunityCommandHandler> logger) : IRequestHandler<CheckInToLabCommunityCommand>
{
    public async Task Handle(CheckInToLabCommunityCommand request, CancellationToken cancellationToken)
    {
        var settings = settingsSnapshot.Value;
        var existingEnrollment = await dbContext.GetActiveEnrollmentAsync(settings.LaboratoryCode, cancellationToken);
        if (existingEnrollment is null)
        {
            logger.LogWarning("Cannot check-in to LabCommunity. No active enrollment found.");

            await FailCheckIn();

            return;
        }

        var checkedIn = await checkInService.CheckIn(cancellationToken);
        if (checkedIn)
        {
            checkInState.MarkSucceeded(dateTime.UtcNow);
            await connectionHub.LabCommunityStatusChanged();
        }
        else
        {
            await FailCheckIn();
        }
    }

    private async Task FailCheckIn()
    {
        checkInState.MarkFailed(dateTime.UtcNow);
        await connectionHub.LabCommunityStatusChanged();
    }
}
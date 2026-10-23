using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.PharosStatus;

namespace Pharos.Server.LabCommunity.Application.Commands;

internal sealed record SendPharosStatusToLabCommunityCommand : IRequest;

internal sealed class SendPharosStatusToLabCommunityCommandHandler(
    ILabCommunityDbContext dbContext,
    ILabCommunityPharosStatusService statusService,
    IOptionsSnapshot<LabCommunitySettings> settingsSnapshot,
    ILogger<SendPharosStatusToLabCommunityCommandHandler> logger) : IRequestHandler<SendPharosStatusToLabCommunityCommand>
{
    public async Task Handle(SendPharosStatusToLabCommunityCommand request, CancellationToken cancellationToken)
    {
        var laboratoryCode = settingsSnapshot.Value.LaboratoryCode;
        var existingEnrollment = await dbContext.GetActiveEnrollmentAsync(laboratoryCode, cancellationToken);
        if (existingEnrollment is null)
        {
            logger.LogWarning("Cannot send Pharos status to LabCommunity. No active enrollment found.");
            return;
        }

        await statusService.SendStatusAsync(cancellationToken);
    }
}
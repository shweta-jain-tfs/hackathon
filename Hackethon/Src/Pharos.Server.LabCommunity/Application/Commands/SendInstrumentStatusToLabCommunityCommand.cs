using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;

namespace Pharos.Server.LabCommunity.Application.Commands;

internal sealed record SendInstrumentStatusToLabCommunityCommand : IRequest;

internal sealed class SendInstrumentStatusToLabCommunityCommandHandler(
    ILabCommunityDbContext dbContext,
    ILabCommunityInstrumentStatusService statusService,
    IOptionsSnapshot<LabCommunitySettings> settingsSnapshot,
    ILogger<SendInstrumentStatusToLabCommunityCommandHandler> logger) : IRequestHandler<SendInstrumentStatusToLabCommunityCommand>
{
    public async Task Handle(SendInstrumentStatusToLabCommunityCommand request, CancellationToken cancellationToken)
    {
        var laboratoryCode = settingsSnapshot.Value.LaboratoryCode;
        var existingEnrollment = await dbContext.GetActiveEnrollmentAsync(laboratoryCode, cancellationToken);
        if (existingEnrollment is null)
        {
            logger.LogWarning("Cannot send instrument status to LabCommunity. No active enrollment found.");    
            return;
        }

        await statusService.SendStatusAsync(cancellationToken);
    }
}
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.LabCommunity;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Commands;

internal sealed class SendQualityControlResultToLabCommunityCommandHandler(
    ILabCommunityExportQueue exportQueue,
    ILabCommunityDbContext dbContext,
    IOptionsSnapshot<LabCommunitySettings> settingsSnapshot) : IRequestHandler<SendQualityControlResultToLabCommunityCommand>
{
    public async Task Handle(SendQualityControlResultToLabCommunityCommand command, CancellationToken cancellationToken)
    {
        var settings = settingsSnapshot.Value;
        if (!settings.Enable)
        {
            return;
        }

        var configuration = await dbContext.GetConfigurationAsync(settings.LaboratoryCode, cancellationToken);
        if (configuration?.ReportCalibrationAndQualityResultsEnabled != true)
        {
            return;
        }

        await exportQueue.EnqueueAsync(
            ExportItemType.QualityControlResult,
            $"QualityControlResult:{command.GroupId}:{command.QualityControlName}",
            JsonSerializer.Serialize(command),
            1,
            cancellationToken);
    }
}

using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.LabCommunity;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Commands;

internal sealed class SendCurveControlResultToLabCommunityCommandHandler(
    ILabCommunityExportQueue exportQueue,
    ILabCommunityDbContext dbContext,
    IOptionsSnapshot<LabCommunitySettings> settingsSnapshot) : IRequestHandler<SendCurveControlResultToLabCommunityCommand>
{
    public async Task Handle(SendCurveControlResultToLabCommunityCommand command, CancellationToken cancellationToken)
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
            ExportItemType.CurveControlResult,
            $"CurveControlResult:{command.GroupId}:{command.CurveControlName}",
            JsonSerializer.Serialize(command),
            1,
            cancellationToken);
    }
}
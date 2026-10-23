using Pharos.Server.Sample.Application.Commands.UpdateSampleTrayPosition;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Commands.RemoveTubeFromUnloadedTray;

internal record RemoveTubeFromUnloadedTrayCommand(string TubeId) : IRequest;

internal sealed class RemoveTubeFromUnloadedTrayCommandHandler(
    ISampleDbContext dbContext,
    ISender sender)
    : IRequestHandler<RemoveTubeFromUnloadedTrayCommand>
{
    public async Task Handle(RemoveTubeFromUnloadedTrayCommand request, CancellationToken cancellationToken)
    {
        var tubeInUnloadedTray = await dbContext.SampleTubes
            .Where(t => t.TubeId == request.TubeId
                     && !string.IsNullOrEmpty(t.TrayId)
                     && t.TubePosition != null
                     && dbContext.SampleTrays.Any(tray => tray.Id == t.TrayId && tray.InstrumentNumber == null))
            .FirstOrDefaultAsync(cancellationToken);

        if (tubeInUnloadedTray != null)
        {
            await sender.Send(new UpdateSampleTrayPositionCommand
            {
                TrayId = tubeInUnloadedTray.TrayId!,
                TubePosition = tubeInUnloadedTray.TubePosition!.Value,
                TubeBarcode = string.Empty,
                HighPriority = false
            }, cancellationToken);
        }
    }
}

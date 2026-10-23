using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Events;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Commands;

internal class SetSampleInSampleTrayPositionCommandHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext dbContext,
    ISampleQcIdProvider sampleQcIdProvider,
    ISender sender) : IRequestHandler<SetSampleInSampleTrayPositionCommand>
{
    public async Task Handle(SetSampleInSampleTrayPositionCommand request, CancellationToken cancellationToken)
    {
        var tubeKey = sampleQcIdProvider.GetSampleTubeKeyNoBarcode(request.SampleId);
        var tube = sampleStorage.GetTube(tubeKey.TubeId);

        if (tube != null)
        {
            // TODO: add a scary message to the user
            throw new FluentValidation.ValidationException($"The tube for the sample {request.SampleId} is already in the instrument. It is not allowed set the position for this tube.");
        }

        var sampleTray = await dbContext.SampleTrays
            .Include(x => x.SampleTubes)
            .FirstOrDefaultAsync(x => x.Id == request.TrayId, cancellationToken);

        if (sampleTray == null)
        {
            sampleTray = new SampleTray
            {
                Id = request.TrayId,
            };
            sampleTray.AddDomainEvent(new SampleTrayCreatedEvent(sampleTray));
            dbContext.SampleTrays.Add(sampleTray);
        }
        else if (!string.IsNullOrEmpty(sampleTray.InstrumentNumber))
        {
            // TODO: add a scary message to the user
            throw new FluentValidation.ValidationException($"The tray {request.TrayId} is already in the instrument. It is not allowed define positions for the tray.");
        }

        var existingTubeInTargetPosition = sampleTray.SampleTubes.FirstOrDefault(x => x.TubePosition == request.TubePosition);
        if (existingTubeInTargetPosition != null && existingTubeInTargetPosition.TubeId != tubeKey.TubeId)
        {
            await RemoveOrUnmapTubeFromTrayPosition(existingTubeInTargetPosition, sampleTray, cancellationToken);
        }

        tube = await dbContext.SampleTubes.FirstOrDefaultAsync(t => t.TubeId == tubeKey.TubeId, cancellationToken);
        if (tube == null)
        {
            tube = new SampleTube
            {
                TubeId = tubeKey.TubeId,
                SampleId = request.SampleId,
                Status = SampleTubeStatus.InProgress,
            };
            dbContext.SampleTubes.Add(tube);
        }

        tube.IdentitySource = SampleTubeIdentitySource.External;
        tube.TubePosition = request.TubePosition;

        if (tube.TrayId == null || tube.TrayId != sampleTray.Id)
        {
            sampleTray.SampleTubes.Add(tube);
            tube.TrayId = sampleTray.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveOrUnmapTubeFromTrayPosition(SampleTube tube, SampleTray sampleTray, CancellationToken cancellationToken)
    {
        var requestIdResponse = await sender.Send(new GetRequestIdQuery(tube.SampleId!, tube.RequestCreatedDate), cancellationToken);

        if (requestIdResponse.RequestExists)
        {
            tube.TrayId = null;
            tube.TubePosition = null;
            tube.InstrumentNumber = null;
            sampleTray.SampleTubes.Remove(tube);
            return;
        }

        sampleTray.SampleTubes.Remove(tube);
        dbContext.SampleTubes.Remove(tube);
    }
}

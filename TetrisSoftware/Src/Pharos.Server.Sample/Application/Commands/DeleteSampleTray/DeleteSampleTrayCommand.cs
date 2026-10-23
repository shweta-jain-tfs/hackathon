using Pharos.Server.Application.Request;
using Pharos.Server.Sample.Application.Events;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Commands.DeleteSampleTray;

internal record DeleteSampleTrayCommand(string[] SampleTrayIds) : IRequest<List<string>>;

internal sealed class DeleteSampleTrayCommandHandler(ISampleDbContext dbContext, IInstrumentSampleStorage sampleStorage, ISender sender) : IRequestHandler<DeleteSampleTrayCommand, List<string>>
{
    public async Task<List<string>> Handle(DeleteSampleTrayCommand request, CancellationToken cancellationToken)
    {
        var sampleTrays = await dbContext.SampleTrays
            .Where(x => request.SampleTrayIds.Contains(x.Id))
            .Include(x => x.SampleTubes)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sampleTrays.Count == 0)
        {
            return new List<string>();
        }

        var deletedTrayIds = new HashSet<string>();

        foreach (var tray in sampleTrays)
        {
            // Skip deletion if tray is present in the instrument  
            if (sampleStorage.GetTray(tray.Id) != null)
            {
                continue;
            }

            var tubes = tray.SampleTubes.ToList();
            foreach (var tube in tubes)
            {
                var requestIdResponse = await sender.Send(new GetRequestIdQuery(tube.SampleId!, tube.RequestCreatedDate), cancellationToken);
                if (requestIdResponse.RequestExists)
                {
                    tube.TrayId = null;
                    tube.TubePosition = null;
                    tube.InstrumentNumber = null;
                }
                else
                {
                    tray.SampleTubes.Remove(tube);
                    dbContext.SampleTubes.Remove(tube);
                }
            }

            tray.AddDomainEvent(new SampleTrayDeletedEvent(tray));
            dbContext.SampleTrays.Remove(tray);
            deletedTrayIds.Add(tray.Id);
        }

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return deletedTrayIds.ToList();
    }
}
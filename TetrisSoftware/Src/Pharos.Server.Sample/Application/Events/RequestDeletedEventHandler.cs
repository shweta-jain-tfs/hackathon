using Pharos.Server.Application.Request;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Events;

internal class RequestDeletedEventHandler(ISampleDbContext sampleDbContext) : INotificationHandler<RequestDeletedEvent>
{
    public async Task Handle(RequestDeletedEvent notification, CancellationToken cancellationToken)
    {
        var tubesToBeRemoved = await sampleDbContext.SampleTubes
            .Where(x => x.SampleId == notification.RequestUid.SampleId
                     && x.RequestCreatedDate == notification.RequestUid.RequestCreatedDate
                     && x.TrayId == null
                     && x.TubePosition == null
                     && x.InstrumentNumber == null)
            .ToListAsync(cancellationToken);

        if (tubesToBeRemoved.Count > 0)
        {
            sampleDbContext.SampleTubes.RemoveRange(tubesToBeRemoved);
            await sampleDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

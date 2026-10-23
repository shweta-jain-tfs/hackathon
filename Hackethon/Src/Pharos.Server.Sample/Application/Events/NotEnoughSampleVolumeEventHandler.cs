using Pharos.Server.Application.Request;

namespace Pharos.Server.Sample.Application.Events;

internal class NotEnoughSampleVolumeEventHandler : INotificationHandler<NotEnoughSampleVolumeEvent>
{
    public Task Handle(NotEnoughSampleVolumeEvent notification, CancellationToken cancellationToken)
    {
        //TODO: Handle the event to set the sample tube error in the future.
        //throw new NotImplementedException();
        return Task.CompletedTask;
    }
}

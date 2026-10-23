using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Commands;

namespace Pharos.Server.Events;

internal class ApplicationStartedEventHandler(ISender sender) : INotificationHandler<ApplicationStartedEvent>
{
    public Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        return sender.Send(new SetupAutomaticDeleteJobCommand(), cancellationToken);
    }
}
using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Maintenance.Application.Commands;

namespace Pharos.Server.Maintenance.Application.Events;

internal class ApplicationStartedEventHandler(ISender sender) : INotificationHandler<ApplicationStartedEvent>
{
    public Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        return sender.Send(new CreateMaintenanceTaskCardsCommand(), cancellationToken);
    }
}

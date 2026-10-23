using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Application.Result;

namespace Pharos.Server.Result.Application.Events;

internal class ApplicationStartedEventHandler(ISender sender) : INotificationHandler<ApplicationStartedEvent>
{
    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        await sender.Send(new CleanupCurrentCurvesCommand(), cancellationToken);
    }
}
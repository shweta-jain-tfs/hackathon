using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Request.Application.Commands.RequestIssue;

namespace Pharos.Server.Request.Application.Events;

internal sealed class NotReportedRequestIssuesSettingsChangedEventHandler(ISender sender) :
    INotificationHandler<SettingsChangedEvent<LisSettings>>,
    INotificationHandler<MethodsUpdatedEvent>
{
    public async Task Handle(SettingsChangedEvent<LisSettings> notification, CancellationToken cancellationToken)
    {
        await sender.Send(new SyncNotReportedRequestIssuesCommand(), cancellationToken);
    }

    public async Task Handle(MethodsUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await sender.Send(new SyncNotReportedRequestIssuesCommand(), cancellationToken);
    }
}

using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces;

namespace Pharos.Server.LabCommunity.Application.Events;

internal sealed class ApplicationStartedEventHandler(
    IOptionsSnapshot<LabCommunitySettings> settings,
    ILabCommunitySchedulerService schedulerService) : INotificationHandler<ApplicationStartedEvent>
{
    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        if (settings.Value.Enable)
        {
            await schedulerService.ConfigureAllFromCurrentConfiguration(
                settings.Value.LaboratoryCode,
                cancellationToken);
        }
    }
}

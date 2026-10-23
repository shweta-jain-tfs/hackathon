using MediatR;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;

namespace Pharos.Server.LabCommunity.Application.Events;

internal class LabCommunitySettingsChangedEventHandler(
    ISender sender,
    ILabCommunitySchedulerService schedulerService) : INotificationHandler<SettingsChangedEvent<LabCommunitySettings>>
{
    public async Task Handle(SettingsChangedEvent<LabCommunitySettings> notification, CancellationToken cancellationToken)
    {
        if (notification.NewValue.Enable)
        {
            var labCommunityEnabled = await sender.Send(new EnableLabCommunityCommand(), cancellationToken);

            if (labCommunityEnabled)
            {
                await schedulerService.ConfigureAllFromCurrentConfiguration(
                    notification.NewValue.LaboratoryCode,
                    cancellationToken);

                return;
            }
        }

        await schedulerService.StopAll(cancellationToken);
    }
}
using MediatR;
using Pharos.Server.Application.Identity;
using Pharos.Server.Instrument.Application.Services;

namespace Pharos.Server.Instrument.Application.Events;

internal sealed class UserLoggedOutEventHandler(IAutomaticPowerSaveScheduler automaticPowerSaveScheduler)
    : INotificationHandler<UserLoggedOutEvent>
{
    public async Task Handle(UserLoggedOutEvent notification, CancellationToken cancellationToken)
    {
        if (notification.InstrumentId.HasValue)
        {
            await automaticPowerSaveScheduler.MarkLoggedOutAsync(notification.InstrumentId.Value, cancellationToken);
        }
    }
}

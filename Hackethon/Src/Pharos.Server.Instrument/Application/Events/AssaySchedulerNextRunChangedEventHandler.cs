using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Contracts.Notifications;
using Pharos.Server.Instrument.Application.Helpers;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Queries.GetInstruments;
using ICurrentInstrumentServerHub = Pharos.Server.Application.Instrument.ICurrentInstrumentServerHub;

namespace Pharos.Server.Instrument.Application.Events;

internal record AssaySchedulerNextRunChangedEvent(int InstrumentId) : INotification;

internal class AssaySchedulerNextRunChangedEventHandler(
    IInstrumentDbContext dbContext,
    IInstrumentHub instrumentHub,
    ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<AssaySchedulerNextRunChangedEvent>
{
    public async Task Handle(AssaySchedulerNextRunChangedEvent notification, CancellationToken cancellationToken)
    {
        var scheduler = await dbContext.AssaySchedulers
            .Include(x => x.SchedulerDays)
            .FirstOrDefaultAsync(x => x.InstrumentId == notification.InstrumentId, cancellationToken);

        var nextRunType = AssaySchedulerNextRunType.None;
        DateTimeOffset? nextEventDateTime = null;

        if (scheduler is { IsActive: true } && (scheduler.PausedUntilDate is null || scheduler.PausedUntilDate <= DateTimeOffset.Now))
        {
            (nextEventDateTime, nextRunType) = AssaySchedulerHelper.GetNextRunAssayScheduler(scheduler.SchedulerDays);
        }

        await instrumentHub.AssaySchedulerNextRunChanged(notification.InstrumentId, nextEventDateTime, (int)nextRunType);
        await currentInstrumentHub.AssaySchedulerNextRunChanged(notification.InstrumentId, (int)nextRunType, nextEventDateTime);
    }
}
using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Instrument.Application.Mapping;
using Pharos.Server.Instrument.Domain;
namespace Pharos.Server.Instrument.Application.Events;

internal record AssaySchedulerDayChangedEvent(int InstrumentId, AssaySchedulerDay AssaySchedulerDay, bool AnyDayHasTime) : INotification;

internal class AssaySchedulerDayChangedEventHandler(IInstrumentHub instrumentHub) : INotificationHandler<AssaySchedulerDayChangedEvent>
{
    public async Task Handle(AssaySchedulerDayChangedEvent notification, CancellationToken cancellationToken)
    {
        var dayDto = AssaySchedulerMapper.ToAssaySchedulerDayDto(notification.AssaySchedulerDay);

        await instrumentHub.AssaySchedulerDayChanged(
            notification.AssaySchedulerDay.Scheduler.InstrumentId,
            notification.AssaySchedulerDay.SchedulerId,
            dayDto,
            notification.AnyDayHasTime
            );
    }
}
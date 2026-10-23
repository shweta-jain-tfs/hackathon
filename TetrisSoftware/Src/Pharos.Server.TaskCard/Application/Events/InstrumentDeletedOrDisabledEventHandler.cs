using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.TaskCard.Application.Events;

internal class InstrumentDeletedOrDisabledEventHandler : INotificationHandler<InstrumentDeletedEvent>, INotificationHandler<InstrumentEnabledChangedEvent>
{
    private readonly ITaskCardService _taskCardService;

    public InstrumentDeletedOrDisabledEventHandler(ITaskCardService taskCardService)
    {
        _taskCardService = taskCardService;
    }

    public Task Handle(InstrumentDeletedEvent notification, CancellationToken cancellationToken)
    {
        _taskCardService.RemoveTaskCardsForInstrument(notification.InstrumentNumber);
        return Task.CompletedTask;
    }

    public Task Handle(InstrumentEnabledChangedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Enabled)
        {
            _taskCardService.RemoveTaskCardsForInstrument(notification.InstrumentNumber);
        }
        return Task.CompletedTask;
    }
}
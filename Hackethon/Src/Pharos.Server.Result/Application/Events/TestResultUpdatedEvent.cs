using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

internal class TestResultUpdatedEvent(TestResult entity) : EntityUpdatedEvent<TestResult>(entity);

internal sealed class TestResultUpdatedEventHandler(IResultHub hub) : INotificationHandler<TestResultUpdatedEvent>
{
    public async Task Handle(TestResultUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await hub.Updated(notification.UpdatedEntity.Id);
    }
}
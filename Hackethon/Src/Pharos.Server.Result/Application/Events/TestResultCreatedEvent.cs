using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

internal class TestResultCreatedEvent(TestResult entity) : EntityCreatedEvent<TestResult>(entity);

internal sealed class TestResultCreatedEventHandler(IResultHub hub) : INotificationHandler<TestResultCreatedEvent>
{
    public async Task Handle(TestResultCreatedEvent notification, CancellationToken cancellationToken)
    {
        await hub.Created(notification.NewEntity.Id);
    }
}
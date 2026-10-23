using MediatR;
using Pharos.Server.Domain.Common;
using Pharos.Server.SystemErrors.Domain;
using Pharos.Server.SystemErrors.Infrastructure.Hubs;

namespace Pharos.Server.SystemErrors.Application.Events;

internal class SystemErrorCreated : EntityCreatedEvent<SystemError>
{
    public SystemErrorCreated(SystemError newEntity) : base(newEntity)
    {
    }
}

internal class SystemErrorUpdated : EntityUpdatedEvent<SystemError>
{
    public SystemErrorUpdated(SystemError updatedEntity) : base(updatedEntity)
    {
    }
}

internal class SystemErrorNotifyClient : INotificationHandler<SystemErrorCreated>, INotificationHandler<SystemErrorUpdated>
{
    private readonly SystemErrorHub _hub;

    public SystemErrorNotifyClient(SystemErrorHub hub)
    {
        _hub = hub;
    }
    
    public async Task Handle(SystemErrorCreated notification, CancellationToken cancellationToken)
    {
        await _hub.Created(notification.NewEntity.Id);
    }

    public async Task Handle(SystemErrorUpdated notification, CancellationToken cancellationToken)
    {
        await _hub.Updated(notification.UpdatedEntity.Id);
    }
}
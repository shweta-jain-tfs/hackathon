using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure;
using Pharos.Server.SystemUpdate.Application.Interfaces;

namespace Pharos.Server.SystemUpdate.Application.Commands;

internal sealed record AcknowledgeUpdateCommand : IRequest;

internal sealed class AcknowledgeUpdateCommandHandler : IRequestHandler<AcknowledgeUpdateCommand>
{
    private readonly ISystemUpdateDbContext _dbContext;
    private readonly ISystemUpdateHub _updateHub;
    private readonly IDateTime _dateTime;

    public AcknowledgeUpdateCommandHandler(ISystemUpdateDbContext dbContext, ISystemUpdateHub updateHub, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _updateHub = updateHub;
        _dateTime = dateTime;
    }

    public async Task Handle(AcknowledgeUpdateCommand request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.GetLatestUnacknowledgedAsync(cancellationToken);

        if (session == null) { return; }

        session.AcknowledgedAtUtc = _dateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _updateHub.UpdateOutcomeAcknowledged();
    }
}

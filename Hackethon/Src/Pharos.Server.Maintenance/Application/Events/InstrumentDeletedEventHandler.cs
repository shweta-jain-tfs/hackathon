using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Maintenance.Application.Interfaces;

namespace Pharos.Server.Maintenance.Application.Events;

internal class InstrumentDeletedEventHandler : INotificationHandler<InstrumentDeletedEvent>
{
    private readonly IMaintenanceDbContext _dbContext;

    public InstrumentDeletedEventHandler(IMaintenanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(InstrumentDeletedEvent notification, CancellationToken cancellationToken)
    {
        await _dbContext.Activities.Where(a => a.InstrumentNumber == notification.InstrumentNumber).ExecuteDeleteAsync(cancellationToken);
    }
}
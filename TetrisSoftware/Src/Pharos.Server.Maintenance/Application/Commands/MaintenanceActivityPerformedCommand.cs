using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Maintenance.Application.Events;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Domain;

namespace Pharos.Server.Maintenance.Application.Commands;

internal record MaintenanceActivityPerformedCommand(MaintenanceActivityType ActivityType, string InstrumentNumber) : IRequest;

internal class MaintenanceActivityPerformedCommandHandler : IRequestHandler<MaintenanceActivityPerformedCommand>
{
    private readonly IMaintenanceDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly IMaintenanceDefinitionProvider _definitionProvider;

    public MaintenanceActivityPerformedCommandHandler(IMaintenanceDbContext dbContext, IDateTime dateTime, IMaintenanceDefinitionProvider definitionProvider)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _definitionProvider = definitionProvider;
    }

    public async Task Handle(MaintenanceActivityPerformedCommand request, CancellationToken cancellationToken)
    {
        var maintenanceActivity = await _dbContext.Activities.FirstOrDefaultAsync(x => x.ActivityType == request.ActivityType && x.InstrumentNumber == request.InstrumentNumber, cancellationToken);
        if (maintenanceActivity == null)
        {
            throw new NotFoundException(nameof(MaintenanceActivity));
        }

        if (_definitionProvider.IsAllowedToBeMarkedAsPerformedFromClient(maintenanceActivity.ActivityType))
        {
            maintenanceActivity.LastCompleted = _dateTime.Now;
            maintenanceActivity.AddDomainEvent(new MaintenanceActivityUpdated(maintenanceActivity));

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"Maintenance activity {maintenanceActivity.ActivityType} is not allowed to update last completed time.");
        }
    }
}
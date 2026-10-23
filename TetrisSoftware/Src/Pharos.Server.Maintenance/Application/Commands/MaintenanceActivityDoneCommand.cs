using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Domain;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Maintenance.Application.Commands;

internal record MaintenanceActivityDoneCommand(int MaintenanceActivityId) : IRequest;

internal class MaintenanceActivityDoneCommandHandler : IRequestHandler<MaintenanceActivityDoneCommand>
{
    private readonly IMaintenanceDbContext _dbContext;
    private readonly IMaintenanceDefinitionProvider _definitionProvider;
    private readonly ISender _sender;

    public MaintenanceActivityDoneCommandHandler(IMaintenanceDbContext dbContext, IMaintenanceDefinitionProvider definitionProvider, ISender sender)
    {
        _dbContext = dbContext;
        _definitionProvider = definitionProvider;
        _sender = sender;
    }

    public async Task Handle(MaintenanceActivityDoneCommand request, CancellationToken cancellationToken)
    {
        var maintenanceActivity = await _dbContext.Activities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.MaintenanceActivityId, cancellationToken);
        if (maintenanceActivity == null)
        {
            throw new NotFoundException(nameof(MaintenanceActivity), request.MaintenanceActivityId);
        }

        if (_definitionProvider.IsAllowedToSendDone(maintenanceActivity.ActivityType))
        {
            await _sender.Send(new ControlMaintenanceCommand(maintenanceActivity.InstrumentNumber, new ControlMaintenanceRequest
            {
                OrderedAction = EnumMaintenanceAction.Done,
                OrderedMaintenance = _definitionProvider.GetMaintenanceType(maintenanceActivity.ActivityType),
                MaintenanceId = maintenanceActivity.MaintenanceId.ToString()
            }), cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"Maintenance activity {maintenanceActivity.ActivityType} is not allowed to be marked as done.");
        }
    }
}

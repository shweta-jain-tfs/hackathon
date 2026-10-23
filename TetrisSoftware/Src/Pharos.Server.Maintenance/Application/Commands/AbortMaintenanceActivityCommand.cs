using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Domain;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Maintenance.Application.Commands;

internal record AbortMaintenanceActivityCommand(int MaintenanceActivityId) : IRequest;

internal class AbortMaintenanceActivityCommandHandler : IRequestHandler<AbortMaintenanceActivityCommand>
{
    private readonly IMaintenanceDbContext _dbContext;
    private readonly IMaintenanceDefinitionProvider _definitionProvider;
    private readonly ISender _sender;

    public AbortMaintenanceActivityCommandHandler(IMaintenanceDbContext dbContext, IMaintenanceDefinitionProvider definitionProvider, ISender sender)
    {
        _dbContext = dbContext;
        _definitionProvider = definitionProvider;
        _sender = sender;
    }

    public async Task Handle(AbortMaintenanceActivityCommand request, CancellationToken cancellationToken)
    {
        var maintenanceActivity = await _dbContext.Activities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.MaintenanceActivityId, cancellationToken);
        if (maintenanceActivity == null)
        {
            throw new NotFoundException(nameof(MaintenanceActivity), request.MaintenanceActivityId);
        }

        await _sender.Send(new ControlMaintenanceCommand(maintenanceActivity.InstrumentNumber, new ControlMaintenanceRequest
        {
            OrderedAction = EnumMaintenanceAction.Abort,
            OrderedMaintenance = _definitionProvider.GetMaintenanceType(maintenanceActivity.ActivityType),
            MaintenanceId = maintenanceActivity.MaintenanceId.ToString()
        }), cancellationToken);
    }
}
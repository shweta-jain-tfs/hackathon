using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Domain;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Maintenance.Application.Commands;

public record StartMaintenanceActivityResponse(StartMaintenance StartMaintenance);
public enum StartMaintenance
{
    MaintenanceStartOk,
    CannotStartWrongInstrumentState
}

internal record StartMaintenanceActivityCommand(int MaintenanceActivityId) : IRequest<StartMaintenanceActivityResponse>;

internal class StartMaintenanceActivityCommandHandler : IRequestHandler<StartMaintenanceActivityCommand, StartMaintenanceActivityResponse>
{
    private readonly IMaintenanceDbContext _dbContext;
    private readonly IMaintenanceDefinitionProvider _definitionProvider;
    private readonly ISender _sender;

    public StartMaintenanceActivityCommandHandler(IMaintenanceDbContext dbContext, IMaintenanceDefinitionProvider definitionProvider, ISender sender)
    {
        _dbContext = dbContext;
        _definitionProvider = definitionProvider;
        _sender = sender;
    }

    public async Task<StartMaintenanceActivityResponse> Handle(StartMaintenanceActivityCommand request, CancellationToken cancellationToken)
    {
        var maintenanceActivity = await _dbContext.Activities.FirstOrDefaultAsync(x => x.Id == request.MaintenanceActivityId, cancellationToken);
        if (maintenanceActivity == null)
        {
            throw new NotFoundException(nameof(MaintenanceActivity), request.MaintenanceActivityId);
        }

        var result = await _sender.Send(new ControlMaintenanceCommand(maintenanceActivity.InstrumentNumber, new ControlMaintenanceRequest
        {
            OrderedAction = EnumMaintenanceAction.Start,
            OrderedMaintenance = _definitionProvider.GetMaintenanceType(maintenanceActivity.ActivityType),
            MaintenanceId = maintenanceActivity.MaintenanceId.ToString()
        }), cancellationToken);

        return new StartMaintenanceActivityResponse(Map(result.Response));
    }

    private static StartMaintenance Map(EnumMaintenanceResponse resultResponse)
    {
        return resultResponse switch
        {
            EnumMaintenanceResponse.MaintenanceStartOk => StartMaintenance.MaintenanceStartOk,
            EnumMaintenanceResponse.CannotStartWrongInstrumentState => StartMaintenance.CannotStartWrongInstrumentState,
            _ => throw new ArgumentOutOfRangeException(nameof(resultResponse), resultResponse, null)
        };
    }
}
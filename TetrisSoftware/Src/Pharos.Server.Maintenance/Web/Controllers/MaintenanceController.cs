using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Maintenance.Application.Commands;
using Pharos.Server.Maintenance.Application.Queries;
using Pharos.Server.Maintenance.Domain;

namespace Pharos.Server.Maintenance.Web.Controllers;

[ApiController]
[Route("api/maintenance")]
public class MaintenanceController(
    ISender sender,
    ICurrentInstrumentService currentInstrument,
    IInstrumentManager instrumentManager) : ControllerBase
{
    [HttpGet]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<List<MaintenanceActivityDto>> GetMaintenanceActivities(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetMaintenanceActivitiesQuery(InstrumentNumber), cancellationToken);
    }

    [HttpPost("api/startmaintenanceactivity/{id}")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<StartMaintenanceActivityResponse> StartMaintenanceActivity(int id, CancellationToken cancellationToken)
    {
        return await sender.Send(new StartMaintenanceActivityCommand(id), cancellationToken);
    }

    [HttpPost("api/maintenanceactivitydone/{id}")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task MaintenanceActivityDone(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new MaintenanceActivityDoneCommand(id), cancellationToken);
    }

    [HttpPost("api/maintenanceactivityperformed/{activityType}")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task MaintenanceActivityPerformed(MaintenanceActivityType activityType, CancellationToken cancellationToken)
    {
        await sender.Send(new MaintenanceActivityPerformedCommand(activityType, InstrumentNumber), cancellationToken);
    }

    [HttpPost("api/abortmaintenanceactivity/{id}")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task AbortMaintenanceActivity(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new AbortMaintenanceActivityCommand(id), cancellationToken);
    }

    [HttpGet("api/getmaintenancebottlestate")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<BottleStateDto> GetMaintenanceBottleState(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetMaintenanceBottleStateQuery(InstrumentNumber), cancellationToken);
    }

    [HttpGet("api/canstartmaintenance")]
    [Authorize(Policies.InstrumentOnly)]
    public MaintenanceInfo CanStartMaintenance()
    {
        var instrument = instrumentManager.GetInstrument(InstrumentNumber);
        return new MaintenanceInfo
        {
            CanStartMaintenance = instrument.CanStartMaintenance,
            IsInMaintenanceMode = instrument.IsInMaintenanceMode
        };
    }

    private string InstrumentNumber => currentInstrument.InstrumentNumber ?? throw new NotFoundException();
}

public class MaintenanceInfo
{
    public bool CanStartMaintenance { get; set; }
    public bool IsInMaintenanceMode { get; set; }
}
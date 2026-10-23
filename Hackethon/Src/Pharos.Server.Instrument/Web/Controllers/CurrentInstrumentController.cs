using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;
using Pharos.Server.Instrument.Application.Commands.ControlLight;
using Pharos.Server.Instrument.Application.Commands.ControlSignalTower;
using Pharos.Server.Instrument.Application.Commands.SkipNextScheduledAssayStart;
using Pharos.Server.Instrument.Application.Commands.UnlockCompartment;
using Pharos.Server.Instrument.Application.Commands.UpdateInstrumentMethod;
using Pharos.Server.Instrument.Application.Queries.GetBarcodeDetails;
using Pharos.Server.Instrument.Application.Queries.GetInstrumentEnvironment;
using Pharos.Server.Instrument.Application.Queries.GetInstrumentMethods;
using Pharos.Server.Instrument.Application.Queries.GetInstrumentModuleInformation;
using Pharos.Server.Instrument.Application.Queries.GetInstrumentProcessList;
using Pharos.Server.Instrument.Application.Queries.GetInstruments;
using Pharos.Server.Instrument.Application.Queries.GetProcessStatusSummary;
using Pharos.Server.Instrument.Application.Services;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;
using IAuthorizationService = Pharos.Server.Application.Security.IAuthorizationService;

namespace Pharos.Server.Instrument.Web.Controllers;

[ApiController]
[Route("api/instruments/current")]
public class CurrentInstrumentController : ControllerBase
{
    private readonly ICurrentInstrumentService _currentInstrument;
    private readonly ISender _sender;
    private readonly IAutomaticPowerSaveScheduler _automaticPowerSaveScheduler;
    private readonly IAuthorizationService _authorizationService;

    public CurrentInstrumentController(
        ICurrentInstrumentService currentInstrument,
        ISender sender,
        IAutomaticPowerSaveScheduler automaticPowerSaveScheduler,
        IAuthorizationService authorizationService)
    {
        _currentInstrument = currentInstrument;
        _sender = sender;
        _automaticPowerSaveScheduler = automaticPowerSaveScheduler;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public async Task<InstrumentDto> GetInstrument(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            await _automaticPowerSaveScheduler.MarkLoggedInAsync(CurrentInstrumentId, cancellationToken);
        }

        return await _sender.Send(new GetCurrentInstrumentQuery(), cancellationToken);
    }

    [HttpGet("environment")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<InstrumentEnvironmentDto> GetEnvironment(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetInstrumentEnvironmentQuery(CurrentInstrumentId), cancellationToken);
    }

    [HttpGet("methods")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<List<InstrumentMethodDto>> GetMethods(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetInstrumentMethodsQuery(CurrentInstrumentId), cancellationToken);
    }

    [HttpPut("methods")]
    [Authorize(Policies.InstrumentOnly)]
    [Authorize(Permissions.ManageInstruments)]
    public async Task UpdateAvailableMethod([FromBody] UpdateInstrumentMethodCommand command, CancellationToken cancellationToken)
    {
        command = command with
        {
            InstrumentId = CurrentInstrumentId
        };

        await _sender.Send(command, cancellationToken);
    }

    [HttpGet("processlist")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<PartialQueryResult<ProcessListItemDto>> GetProcessList([FromQuery] GetInstrumentProcessListQuery query, CancellationToken cancellationToken)
    {
        query.InstrumentId = CurrentInstrumentId;

        return await _sender.Send<PartialQueryResult<ProcessListItemDto>>(query, cancellationToken);
    }

    [HttpGet("process-status")]
    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public async Task<ProcessStatusSummaryDto> GetProcessStatusSummary(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetProcessStatusSummaryQuery(CurrentInstrumentId), cancellationToken);
    }

    [HttpPost("startassay")]
    [Authorize(Policies.InstrumentOnly)]
    [Authorize(Permissions.ManageInstruments)]
    public Task StartAssay(CancellationToken cancellationToken)
    {
        return _sender.Send(new StartAssayCommand(CurrentInstrumentNumber), cancellationToken);
    }

    [HttpPost("startcmrassay")]
    [Authorize(Policies.InstrumentOnly)]
    [Authorize(Permissions.RunServiceTools)]
    public Task StartCmrAssay(CancellationToken cancellationToken)
    {
        return _sender.Send(new StartAssayCommand(CurrentInstrumentNumber, CustomMadeRun: true), cancellationToken);
    }

    [HttpPost("endassay")]
    [Authorize(Policies.InstrumentOnly)]
    [Authorize(Permissions.ManageInstruments)]
    public Task EndAssay([FromQuery] EndAssayFinishOptions finishOption, CancellationToken cancellationToken)
    {
        return _sender.Send(new EndAssayCommand(CurrentInstrumentNumber, finishOption), cancellationToken);
    }

    [HttpPost("abortassay")]
    [Authorize(Policies.InstrumentOnly)]
    [Authorize(Permissions.ManageInstruments)]
    public Task AbortAssay(CancellationToken cancellationToken)
    {
        return _sender.Send(new AbortAssayCommand(CurrentInstrumentNumber), cancellationToken);
    }

    [HttpPost("powersave")]
    [Authorize(Policies.InstrumentOnly)]
    [Authorize(Permissions.ManageInstruments)]
    public Task PowerSave(CancellationToken cancellationToken)
    {
        return _sender.Send(new PowerSaveCommand(CurrentInstrumentNumber), cancellationToken);
    }

    [HttpPost("poweroff")]
    [Authorize(Policies.InstrumentOnly)]
    [Authorize(Permissions.ManageInstruments)]
    public Task PowerOff(CancellationToken cancellationToken)
    {
        return _sender.Send(new PowerOffCommand(CurrentInstrumentNumber), cancellationToken);
    }

    [HttpPost("standby")]
    [Authorize(Policies.InstrumentOnly)]
    [Authorize(Permissions.ManageInstruments)]
    public Task Standby(CancellationToken cancellationToken)
    {
        return _sender.Send(new StandbyCommand(CurrentInstrumentNumber), cancellationToken);
    }

    [HttpPost("controllight")]
    [Authorize(Policies.InstrumentOnly)]
    public Task ControlLight(CancellationToken cancellationToken)
    {
        return _sender.Send(new ControlLightCommand(CurrentInstrumentNumber), cancellationToken);
    }

    [HttpPost("assayscheduler/skip-next-start")]
    [Authorize(Policies.InstrumentOnly)]
    [Authorize(Permissions.ManageInstruments)]
    public Task SkipNextScheduledAssayStart(CancellationToken cancellationToken)
    {
        return _sender.Send(new SkipNextScheduledAssayStartCommand(CurrentInstrumentId), cancellationToken);
    }

    [HttpPost("unlockleftsampledrawer")]
    [Authorize(Policies.InstrumentOnly)]
    public Task UnlockLeftSampleDrawer(CancellationToken cancellationToken)
    {
        return _sender.Send(new UnlockCompartmentCommand(CurrentInstrumentNumber, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1), cancellationToken);
    }

    [HttpPost("unlockrightsampledrawer")]
    [Authorize(Policies.InstrumentOnly)]
    public Task UnlockRightSampleDrawer(CancellationToken cancellationToken)
    {
        return _sender.Send(new UnlockCompartmentCommand(CurrentInstrumentNumber, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer2), cancellationToken);
    }

    [HttpPost("unlockreagentdrawer")]
    [Authorize(Policies.InstrumentOnly)]
    public Task UnlockReagentDrawer(CancellationToken cancellationToken)
    {
        return _sender.Send(new UnlockCompartmentCommand(CurrentInstrumentNumber, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer), cancellationToken);
    }

    [HttpPost("unlocksolidwastedrawer")]
    [Authorize(Policies.InstrumentOnly)]
    public Task UnlockSolidWasteDrawer(CancellationToken cancellationToken)
    {
        return _sender.Send(new UnlockCompartmentCommand(CurrentInstrumentNumber, EnumInstrumentCompartmentWithLock.CompartmentSolidWasteDrawer), cancellationToken);
    }

    [HttpPost("unlockhood")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task UnlockHood(CancellationToken cancellationToken)
    {
        var canManageInstruments = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ManageInstruments);
        var canRunServiceTools = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.RunServiceTools);

        if (!canManageInstruments && !canRunServiceTools)
        {
            await HttpContext.ForbidAsync();
            return;
        }

        await _sender.Send(new UnlockCompartmentCommand(CurrentInstrumentNumber, EnumInstrumentCompartmentWithLock.CompartmentHood), cancellationToken);
    }

    [HttpPost("mutesignaltower")]
    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public async Task MuteSignalTower(CancellationToken cancellationToken)
    {
        // Handle the signal tower direct. 
        await _sender.Send(
            new ControlSignalTowerCommand(
                CurrentInstrumentNumber,
                new ControlSignalTowerRequest
                {
                    SetSound = EnumSignalTowerControlSoundState.SoundOff
                }),
            cancellationToken);
    }

    [HttpGet("moduleinformation")]
    [Authorize(Policies.InstrumentOnly)]
    public Task<ModuleInformationDto> GetInstrumentModuleInformation(CancellationToken cancellationToken)
    {
        var query = new GetInstrumentModuleInformationQuery(CurrentInstrumentId);
        return _sender.Send(query, cancellationToken);
    }

    [HttpGet("barcodedetails")]
    [Authorize(Policies.InstrumentOnly)]
    public Task<ScannedBarcodeDto> GetBarcodeDetails([FromQuery] string barcode, CancellationToken cancellationToken)
    {
        var query = new GetBarcodeDetailsQuery(CurrentInstrumentNumber, barcode);
        return _sender.Send(query, cancellationToken);
    }

    private int CurrentInstrumentId => _currentInstrument.InstrumentId ?? throw new NotFoundException();
    private string CurrentInstrumentNumber => _currentInstrument.InstrumentNumber ?? throw new NotFoundException();
}

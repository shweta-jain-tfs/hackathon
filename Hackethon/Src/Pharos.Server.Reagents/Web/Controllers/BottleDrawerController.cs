using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Reagents.Application.Queries;

namespace Pharos.Server.Reagents.Web.Controllers;

[ApiController]
[Route("api/bottledrawer")]
public class BottleDrawerController : ControllerBase
{
    private readonly ICurrentInstrumentService _currentInstrumentService;
    private readonly ISender _sender;

    public BottleDrawerController(ICurrentInstrumentService currentInstrumentService, ISender sender)
    {
        _currentInstrumentService = currentInstrumentService;
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<BottleDrawerDetailsDto> GetBottleDrawerDetails(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetBottleDrawerDetailsQuery(CurrentInstrumentNumber), cancellationToken);
    }

    [HttpGet("loading-position")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<LoadingPositionDto> GetLoadingPosition([FromQuery] string barcode, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetLoadingPositionQuery(CurrentInstrumentNumber, barcode), cancellationToken);
    }

    private string CurrentInstrumentNumber => _currentInstrumentService.InstrumentNumber ?? throw new NotFoundException();
}

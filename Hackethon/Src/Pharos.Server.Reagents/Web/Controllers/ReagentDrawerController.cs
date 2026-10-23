using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Reagents.Application.Queries;

namespace Pharos.Server.Reagents.Web.Controllers;

[ApiController]
[Route("api/reagentdrawer")]
public class ReagentDrawerController : ControllerBase
{
    private readonly ICurrentInstrumentService _currentInstrumentService;
    private readonly ISender _sender;

    public ReagentDrawerController(ICurrentInstrumentService currentInstrumentService, ISender sender)
    {
        _currentInstrumentService = currentInstrumentService;
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<ReagentDrawerDto> GetReagentDrawerDetails(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetReagentDrawerDetailsQuery(CurrentInstrumentNumber), cancellationToken);
    }

    [HttpGet("reagent-content-detail")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<ReagentContentDetailDto> GetReagentContentDetail([FromQuery] string? barcode, [FromQuery] string? reagentUdi, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetReagentContentDetailQuery(barcode, reagentUdi, CurrentInstrumentNumber), cancellationToken);
    }

    private string CurrentInstrumentNumber => _currentInstrumentService.InstrumentNumber ?? throw new NotFoundException();
}

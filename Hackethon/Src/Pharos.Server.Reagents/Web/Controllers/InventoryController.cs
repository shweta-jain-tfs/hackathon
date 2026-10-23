using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Reagents.Application.Commands;
using Pharos.Server.Reagents.Application.Queries.Inventory;

namespace Pharos.Server.Reagents.Web.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly ISender _sender;

    public InventoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("carriers")]
    [Authorize]
    public async Task<PartialQueryResult<CarrierListResultDto>> GetCarriers([FromQuery] GetCarriersQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<CarrierListResultDto>>(query, cancellationToken);
    }

    [HttpGet("cartridges")]
    [Authorize]
    public async Task<PartialQueryResult<CartridgeListResultDto>> GetCartridges([FromQuery] GetCartridgesQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<CartridgeListResultDto>>(query, cancellationToken);
    }

    [HttpGet("bottles")]
    [Authorize]
    public async Task<PartialQueryResult<BottleListResultDto>> GetBottles([FromQuery] GetBottlesQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<BottleListResultDto>>(query, cancellationToken);
    }

    [HttpPost("unload-reagent")]
    [Authorize(Permissions.ManageReagents)]
    public async Task<IActionResult> UnloadReagent([FromBody] List<UnloadReagentRequestItemDto> items)
    {
        await _sender.Send(new UnloadReagentCommand(items));
        return Ok();
    }

    [HttpGet("drawer-dilution-plates")]
    [Authorize]
    public async Task<PartialQueryResult<DrawerDilutionPlatesResultDto>> GetDrawerDilutionPlates([FromQuery] GetDrawerDilutionPlatesQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<DrawerDilutionPlatesResultDto>>(query, cancellationToken);
    }

    [HttpGet("internal-dilution-plates")]
    [Authorize]
    public async Task<PartialQueryResult<InternalDilutionPlatesResultDto>> GetInternalDilutionPlates([FromQuery] GetInternalDilutionPlatesQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<InternalDilutionPlatesResultDto>>(query, cancellationToken);
    }
}

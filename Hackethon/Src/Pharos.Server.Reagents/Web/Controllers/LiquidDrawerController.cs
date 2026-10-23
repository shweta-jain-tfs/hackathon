using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Reagents.Application.Queries;

namespace Pharos.Server.Reagents.Web.Controllers;

/// <summary>
/// Controller for liquid drawer operations
/// </summary>
[ApiController]
[Route("api/liquiddrawer")]
public class LiquidDrawerController : ControllerBase
{
    private readonly ICurrentInstrumentService _currentInstrumentService;
    private readonly ISender _sender;

    public LiquidDrawerController(ICurrentInstrumentService currentInstrumentService, ISender sender)
    {
        _currentInstrumentService = currentInstrumentService;
        _sender = sender;
    }

    /// <summary>
    /// Gets liquid drawer details with all container positions and status information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Liquid drawer details with 4 predefined containers (Waste 2,1 and Rinse 2,1)</returns>
    [HttpGet("details")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<LiquidDrawerDetailsDto> GetLiquidDrawerDetails(CancellationToken cancellationToken)
    {
        var query = new GetLiquidDrawerDetailsQuery
        {
            InstrumentNumber = CurrentInstrumentNumber
        };

        return await _sender.Send(query, cancellationToken);
    }

    private string CurrentInstrumentNumber => _currentInstrumentService.InstrumentNumber ?? throw new NotFoundException();
}
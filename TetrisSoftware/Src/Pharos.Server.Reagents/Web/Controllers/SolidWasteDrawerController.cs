using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Reagents.Application.Queries;

namespace Pharos.Server.Reagents.Web.Controllers;

/// <summary>
/// Controller for solid waste drawer operations
/// </summary>
[ApiController]
[Route("api/wastedrawer")]
public class SolidWasteDrawerController : ControllerBase
{
    private readonly ICurrentInstrumentService _currentInstrumentService;
    private readonly ISender _sender;

    public SolidWasteDrawerController(ICurrentInstrumentService currentInstrumentService, ISender sender)
    {
        _currentInstrumentService = currentInstrumentService;
        _sender = sender;
    }

    /// <summary>
    /// Gets solid waste drawer details with container status information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Solid waste drawer details with 2 containers (Biohazard and Recyclable)</returns>
    [HttpGet("details")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<SolidWasteDrawerDetailsDto> GetWasteDrawerDetails(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetSolidWasteDrawerDetailsQuery(CurrentInstrumentNumber), cancellationToken);
    }

    private string CurrentInstrumentNumber => _currentInstrumentService.InstrumentNumber ?? throw new NotFoundException();
}

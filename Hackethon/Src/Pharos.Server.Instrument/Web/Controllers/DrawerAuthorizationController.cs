using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Instrument.Application.Services;

namespace Pharos.Server.Instrument.Web.Controllers;

[ApiController]
[Route("api/drawerauth")]
public class DrawerAuthorizationController : ControllerBase
{
    private readonly ICurrentInstrumentService _currentInstrumentService;
    private readonly IDrawerAuthorizationService _drawerAuthorizationService;
    private readonly ICurrentUserService _userService;

    public DrawerAuthorizationController(ICurrentInstrumentService currentInstrumentService, IDrawerAuthorizationService drawerAuthorizationService, ICurrentUserService userService)
    {
        _drawerAuthorizationService = drawerAuthorizationService;
        _currentInstrumentService = currentInstrumentService;
        _userService = userService;
    }

    [HttpPost("unlockdrawerauthenticate")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task UnlockDrawerAuthorizationResponse()
    {
        var userId = _userService.UserId!.Value;
        var instrumentNumber = _currentInstrumentService.InstrumentNumber;
        await _drawerAuthorizationService.SendUnlockIfWaiting(userId, instrumentNumber!);
    }
}
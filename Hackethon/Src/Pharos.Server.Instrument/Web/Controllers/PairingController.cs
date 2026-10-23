using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Web.Controllers;

[ApiController]
[Route("api/instruments")]
[Authorize(Permissions.ManageInstruments)]
[Authorize(Policies.ServiceEngineerOrRndMode)]
public class PairingController(IInstrumentPairingService pairingService) : ControllerBase
{
    [HttpGet("pairing")]
    public bool IsPairingEnabled()
    {
        return pairingService.PairingEnabled;
    }

    [HttpPost("pairing")]
    public async Task EnablePairing(bool enable)
    {
        if (enable)
        {
            await pairingService.EnablePairing();
        }
        else
        {
            await pairingService.DisablePairing();
        }
    }

    [HttpPost("pairing/approval")]
    public async Task HandleApproval(string instrumentNumber, bool approveParing)
    {
        await pairingService.HandleApproval(instrumentNumber, approveParing);
    }
}

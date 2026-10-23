using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Queries.GetPatient;

namespace Pharos.Server.Request.Web.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController(ISender sender, IOptionsSnapshot<RequestSettings> requestSettings) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ViewPersonalAndHealthData)]
    public async Task<PartialQueryResult<PatientDto>> GetPatient([FromQuery] GetPatientQuery query, CancellationToken cancellationToken)
    {
        if (!requestSettings.Value.IsPatientInformationEnabled)
        {
            // Return an empty result if patient info is disabled.
            return new PartialQueryResult<PatientDto>([], 0, 0);
        }

        return await sender.Send<PartialQueryResult<PatientDto>>(query, cancellationToken);
    }
}

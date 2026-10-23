using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Quality;
using Pharos.Server.Application.Result;
using Pharos.Server.Quality.Application.Queries;

namespace Pharos.Server.Quality.Web.Controllers;

[ApiController]
[Route("api/quality")]
[Authorize(Permissions.EditQualityControlSettings)]
public class QualityController(ISender sender) : ControllerBase
{
    [HttpGet("methods")]
    public Task<List<MethodQualityDto>> GetMethodQuality([FromQuery] GetMethodQualityQuery query, CancellationToken cancellationToken)
    {
        return sender.Send<List<MethodQualityDto>>(query, cancellationToken);
    }

    [HttpPost("qc-cartridge-info")]
    public Task<QcCartridgeInformationDto?> GetQcCartridgeInfo([FromQuery] string dataMatrix, CancellationToken cancellationToken)
    {
        return sender.Send(new GetQcCartridgeInfoQuery(dataMatrix), cancellationToken);
    }
}

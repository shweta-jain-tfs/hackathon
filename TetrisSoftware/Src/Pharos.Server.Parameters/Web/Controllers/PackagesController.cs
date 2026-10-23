using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Server.Application.Queries;
using Pharos.Server.Parameters.Application.Queries.GetPackages;

namespace Pharos.Server.Parameters.Web.Controllers;

[ApiController]
[Route("api/packages")]
public class PackagesController : ControllerBase
{
    private readonly ISender _sender;

    public PackagesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("package")]
    [Authorize]
    public Task<PartialQueryResult<PackageDto>> GetPackages([FromQuery] GetPackagesQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send<PartialQueryResult<PackageDto>>(query, cancellationToken);
    }

    [HttpGet("details/{id}")]
    [Authorize]
    public Task<PackageDetailsDto> GetPackageDetails(int id, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetPackageDetailsQuery(id), cancellationToken);
    }

    [HttpGet("intended-markets")]
    [Authorize]
    public Task<List<IntendedMarketOptionDto>> GetIntendedMarkets(CancellationToken cancellationToken)
    {
        return _sender.Send(new GetIntendedMarketsQuery(), cancellationToken);
    }
}
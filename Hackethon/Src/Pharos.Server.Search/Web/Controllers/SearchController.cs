using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Server.Application.Queries;
using Pharos.Server.Search.Application.Queries;

namespace Pharos.Server.Search.Web.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(ISender sender) : ControllerBase
{
    [HttpGet("searchResult")]
    [Authorize]
    public async Task<IEnumerable<SearchResultDto>> GetSearchResult([FromQuery] GetSearchResultQuery query, CancellationToken cancellationToken)
    {
        return await sender.Send(query, cancellationToken);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.LoadList.Application.Commands.CopyLoadListLevelsCommand;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Application.Commands.UpdateConfiguration;
using Pharos.Server.LoadList.Application.Queries;
using Pharos.Server.LoadList.Application.Queries.GetConfigurations;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Web.Controllers;


[ApiController]
[Route("api/loadlist")]
public class LoadListController
{
    private readonly ISender _sender;

    public LoadListController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("bottles")]
    [Authorize]
    public async Task<List<LoadListBottleDto>> GetLoadListBottles()
    {
        return await _sender.Send(new GetLoadListBottlesQuery());
    }

    [HttpGet("carriers")]
    [Authorize]
    public async Task<List<LoadListCarrierDto>> GetLoadListCarriers()
    {
        return await _sender.Send(new GetLoadListCarriersQuery());
    }

    [HttpGet("cartridges")]
    [Authorize]
    public async Task<List<LoadListCartridgeDto>> GetLoadListCartridges()
    {
        return await _sender.Send(new GetLoadListCartridgesQuery());
    }

    [HttpGet("dilutionPlates")]
    [Authorize]
    public async Task<List<LoadListDilutionPlateDto>> GetLoadListDilutionPlates()
    {
        return await _sender.Send(new GetLoadListDilutionPlatesQuery());
    }

    [HttpGet("sampleTubes")]
    [Authorize]
    public async Task<List<LoadListSampleTubeDto>> GetLoadListSampleTubes()
    {
        return await _sender.Send(new GetLoadListSampleTubesQuery());
    }

    [HttpGet("configurations")]
    [Authorize]
    public async Task<PartialQueryResult<LoadListConfigurationDto>> GetLoadListConfigurations([FromQuery] GetLoadListConfigurationQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<LoadListConfigurationDto>>(query, cancellationToken);
    }

    [HttpGet("totalRefillLevel")]
    [Authorize]
    public async Task<RefillLevelInfoDto> GetTotalRefillLevel(LoadListItemType loadListItemType, string selectedInstrument)
    {
        return await _sender.Send(new GetTotalRefillLevelQuery(loadListItemType, selectedInstrument));
    }

    [HttpGet("reagentLoadersCount")]
    [Authorize]
    public async Task<int> GetReagentLoadersCount(
        [FromQuery] IEnumerable<string>? instrumentNumbers = null,
        [FromQuery] IEnumerable<LoadListStatus>? statuses = null)
    {
        return await _sender.Send(new GetReagentLoadersCountQuery(instrumentNumbers, statuses));
    }

    [HttpPost("updateConfigurations")]
    [Authorize(Permissions.ManageReagents)]
    public async Task UpdateLoadListLevelConfigurations([FromBody] UpdateLoadListConfigurationLevelsCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }

    [HttpPost("copyLevels")]
    [Authorize(Permissions.ManageReagents)]
    public async Task CopyLoadListLevels([FromBody] CopyLoadListLevelsCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }
}

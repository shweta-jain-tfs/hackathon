using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Reflex.Application.Commands.CreateReflexDefinitionCommand;
using Pharos.Server.Reflex.Application.Commands.DeleteReflexDefinitionCommand;
using Pharos.Server.Reflex.Application.Commands.SetActiveReflexDefinitionCommand;
using Pharos.Server.Reflex.Application.Commands.UpdateReflexDefinitionCommand;
using Pharos.Server.Reflex.Application.Mapping;
using Pharos.Server.Reflex.Application.Queries;
using Pharos.Server.Reflex.Application.Queries.GetReflexDefinition;
using Pharos.Server.Reflex.Application.Queries.GetReflexDefinitions;
using Pharos.Server.Reflex.Application.Queries.ValidateReflexDefinitionName;
using Pharos.Server.Reflex.Domain;

namespace Pharos.Server.Reflex.Web.Controllers;

[ApiController]
[Route("api/reflex")]
public class ReflexController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<PartialQueryResult<ReflexDefinitionSummaryDto>> GetReflexDefinitions([FromQuery] GetReflexDefinitionsQuery query, CancellationToken cancellationToken)
    {
        return await sender.Send<PartialQueryResult<ReflexDefinitionSummaryDto>>(query, cancellationToken);
    }

    [HttpGet("{id}")]
    [Authorize]
    public Task<ReflexDefinitionDto> GetReflexDefinition(int id, CancellationToken cancellationToken)
    {
        return sender.Send(new GetReflexDefinitionQuery(id), cancellationToken);
    }

    [HttpGet("forTest")]
    [Authorize]
    public async Task<List<ReflexDefinitionDto>> GetReflexDefinitionsForTest(
        [FromQuery] string testName,
        [FromQuery] string methodName,
        CancellationToken cancellationToken)
    {
        var reflexDefinitions = await sender.Send(
            new GetReflexDefinitionsForTestQuery(testName, methodName), cancellationToken).ConfigureAwait(false);

        var reflexDefinitionsToReturn = reflexDefinitions
            .Cast<ReflexDefinition>() // Ensure concrete type
            .Select(x => x.ToReflexDefinitionDto())
            .ToList();
        return reflexDefinitionsToReturn;
    }

    [HttpGet("validateName")]
    [Authorize]
    public Task<bool> ValidateDefinitionName([FromQuery] string name, CancellationToken cancellationToken)
    {
        return sender.Send(new ValidateReflexDefinitionNameQuery(name), cancellationToken);
    }

    [HttpPost]
    [Authorize(Permissions.EditTestSettings)]
    public Task<int> CreateReflexDefinition(CreateReflexDefinitionCommand command, CancellationToken cancellationToken)
    {
        return sender.Send(command, cancellationToken);
    }

    [HttpPut("{id}")]
    [Authorize(Permissions.EditTestSettings)]
    public Task UpdateReflexDefinition(int id, [FromBody] UpdateReflexDefinitionCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return sender.Send(command, cancellationToken);
    }

    [HttpDelete]
    [Authorize(Permissions.EditTestSettings)]
    public Task DeleteReflexDefinition(int[] ids, CancellationToken cancellationToken)
    {
        var command = new DeleteReflexDefinitionCommand(ids);
        return sender.Send(command, cancellationToken);
    }

    [HttpPut("{id}/activate")]
    [Authorize(Permissions.EditTestSettings)]
    public Task SetActiveReflexDefinition(int id, bool active, CancellationToken cancellationToken)
    {
        var command = new SetActiveReflexDefinitionCommand(id, active);
        return sender.Send(command, cancellationToken);
    }

    [HttpGet("hasManual")]
    [Authorize]
    public Task<bool> HasManualReflexDefinitionsAsync(CancellationToken cancellationToken)
    {
        return sender.Send(new HasManualReflexQuery(), cancellationToken);
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.AuditTrail.Events;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Queries.GetTubeDefinitions;

namespace Pharos.Server.Request.Web.Controllers;

[ApiController]
[Route("api/tubedefinitions")]
public class TubeDefinitionsController(
    ISender sender,
    IRequestsService temporaryRequestsService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IList<TubeDefinitionDto>> GetAll(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetTubeDefinitionsQuery(), cancellationToken);
    }

    /// <summary>
    /// Temporary "injected" here until decided where the Request Import functions should be placed...
    /// </summary>
    [HttpPost]
    [Route("requestImport")]
    [Authorize(Policies.RnD)]
    [Authorize(Permissions.ManageSamples)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RequestImport(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(file), ["No file was provided for request import."] }
            })
            {
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var bytes = await GetBytes(file);
            await temporaryRequestsService.ImportRequestsFile(bytes, file.FileName);

            await sender.Send(RequestEvents.RequestsImportedEvent.Create().ToCommand(), cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            var parameterName = ex.ParamName ?? "RequestImport";
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { parameterName, [ex.Message] }
            })
            {
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static async Task<byte[]> GetBytes(IFormFile file)
    {
        await using var memStream = new MemoryStream();
        await file.CopyToAsync(memStream);
        return memStream.ToArray();
    }
}

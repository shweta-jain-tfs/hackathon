using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Request.Application.Commands.RequestGenerator;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Jobs;
using Pharos.Server.Request.Application.Queries.RequestGenerator;
using Quartz;

namespace Pharos.Server.Request.Web.Controllers;

[ApiController]
[Route("api/request-generator")]
[Authorize(Permissions.RunServiceTools)]
public class RequestGeneratorController(
    ISender sender,
    IRequestGeneratorValidationService validationService,
    ICurrentUserService currentUserService,
    ISchedulerFactory schedulerFactory) : ControllerBase
{
    [HttpGet]
    public async Task<bool> IsRequestGenerationInProgress()
    {
        return await schedulerFactory.IsRequestGenerationJobInProgress();
    }

    [HttpPost("preview")]
    public async Task<List<RequestPreviewDto>> Preview([FromBody] GenerateRequestsDto requestDetails, CancellationToken cancellationToken)
    {
        return await sender.Send(new GenerateRequestsPreviewQuery(requestDetails), cancellationToken);
    }

    [HttpPost("generate")]
    public async Task Generate([FromBody] GenerateRequestsDto requestDetails, CancellationToken cancellationToken)
    {
        if (await schedulerFactory.IsRequestGenerationJobInProgress())
        {
            throw new NotAllowedException(Translations.RequestGenerator_GenerationAlreadyInProgress);
        }

        await validationService.ValidateAsync(requestDetails, cancellationToken);
        await schedulerFactory.CreateRequestGenerationJob(currentUserService.UserId!.Value, requestDetails);
        await schedulerFactory.TriggerRequestGenerationJob();
    }
}
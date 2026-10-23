using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Parameters.Application.Jobs;
using Pharos.Server.Parameters.Application.Queries;
using Pharos.Server.Parameters.Application.Queries.GetVersion;
using Quartz;

namespace Pharos.Server.Parameters.Web.Controllers;

[ApiController]
[Route("api/parameters")]
public class ParametersController(
    ICurrentUserService currentUser,
    ISender sender,
    ITempFileStorage tempFileStorage,
    ISchedulerFactory schedulerFactory) : ControllerBase
{
    [HttpPost]
    [Route("import/upload")]
    [Authorize(Permissions.EditTestSettings)]
    public async Task<Guid> UploadMethodUpdatePackageFile([Required] IFormFile file)
    {
        if (await schedulerFactory.ImportParametersJobExists())
        {
            throw new NotAllowedException("Method update has already been started. You need to cancel the operation before you upload another file.");
        }

        await using var stream = file.OpenReadStream();
        var fileId = await tempFileStorage.SaveFile(file.FileName, stream);
        await schedulerFactory.CreateImportParametersJob(currentUser.UserId!.Value, fileId);

        return fileId;
    }

    [HttpGet]
    [Route("import/status")]
    [Authorize(Permissions.EditTestSettings)]
    public async Task<bool> IsParameterJobInProgress()
    {
        return await schedulerFactory.ImportParametersJobExists();
    }

    [HttpGet]
    [Route("import/package-description")]
    [Authorize(Permissions.EditTestSettings)]
    public async Task<string> GetMethodUpdatePackageDescription(Guid packageFileId)
    {
        return await sender.Send(new GetMethodUpdatePackageDescription(packageFileId));
    }

    [HttpPost]
    [Route("import/start-update")]
    [Authorize(Permissions.EditTestSettings)]
    public async Task StartMethodUpdate()
    {
        await schedulerFactory.TriggerImportParametersJob();
    }

    [HttpPost]
    [Route("import/cancel")]
    [Authorize(Permissions.EditTestSettings)]
    public async Task CancelMethodUpdate()
    {
        var fileId = await schedulerFactory.CancelImportParametersJob();
        if (fileId != Guid.Empty)
        {
            tempFileStorage.DeleteFile(fileId);
        }
    }

    [HttpGet]
    [Route("version")]
    [Authorize]
    public async Task<string?> GetParameterUpdatePackageVersion()
    {
        return await sender.Send(new GetParameterUpdatePackageQuery());
    }
}
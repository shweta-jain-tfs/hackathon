using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Flr.Application.Jobs;
using Quartz;

namespace Pharos.Server.Result.Flr.Web.Controllers;

[ApiController]
[Route("api/flrImport")]
[Authorize(Permissions.RunServiceTools)]
public class FlrController(
    ITempFileStorage tempFileStorage,
    ICurrentUserService currentUser,
    ISchedulerFactory schedulerFactory) : ControllerBase
{
    [HttpPost]
    public async Task StartFlrImport([Required] IFormFile file)
    {
        if (await schedulerFactory.IsFlrImportJobInProgress())
        {
            throw new NotAllowedException("FLR import has already been started. Wait for the operation to complete.");
        }

        await using var stream = file.OpenReadStream();
        var fileId = await tempFileStorage.SaveFile(file.FileName, stream);
        try
        {
            await schedulerFactory.CreateFlrImportJob(currentUser.UserId!.Value, fileId);
            await schedulerFactory.TriggerFlrImportJob();
        }
        catch
        {
            // failed to start the job
            tempFileStorage.DeleteFile(fileId);
            throw;
        }
    }

    [HttpGet]
    public Task<bool> IsFlrImportInProgress()
    {
        return schedulerFactory.IsFlrImportJobInProgress();
    }
}
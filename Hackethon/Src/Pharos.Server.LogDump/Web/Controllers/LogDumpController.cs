using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.LogDump;
using Pharos.Server.Infrastructure;
using Pharos.Server.LogDump.Application.Commands;
using Pharos.Server.LogDump.Application.Jobs;
using Pharos.Server.LogDump.Application.Queries.GetLogDumpCollectionResult;
using Quartz;

namespace Pharos.Server.LogDump.Web.Controllers;

[ApiController]
[Route("api/logDump")]
[Authorize(Permissions.RunServiceTools)]
public class LogDumpController(
    ISender sender,
    ISchedulerFactory schedulerFactory,
    ITempFileStorage tempFileStorage) : ControllerBase
{
    [HttpGet("status")]
    public async Task<bool> IsLogDumpCollectionInProgress(CancellationToken cancellationToken)
    {
        return await schedulerFactory.IsLogDumpJobInProgress(cancellationToken);
    }

    [HttpGet("result")]
    public async Task<LogDumpCollectionResultDto> GetLogDumpCollectionResult()
    {
        return await sender.Send(new GetLogDumpCollectionResultQuery());
    }

    [HttpPost("start")]
    public Task<bool> StartLogDumpCollection([FromBody] LogDumpCollectionRequest request, CancellationToken cancellationToken)
    {
        var command = new StartLogDumpCollectionCommand(request, LogDumpRequestSource.Pharos);
        return sender.Send(command, cancellationToken);
    }

    [HttpPost("cancel")]
    public Task CancelLogDumpCollection(CancellationToken cancellationToken)
    {
        return sender.Send(new CancelLogDumpCollectionCommand(), cancellationToken);
    }

    [HttpGet("export/{fileId:guid}")]
    public IActionResult ExportLogDump(Guid fileId)
    {
        var filePath = tempFileStorage.GetFilePath(fileId);
        if (filePath is null)
        {
            return NotFound();
        }

        var fileName = tempFileStorage.GetFileName(fileId);
        var mimeType = "application/zip";
        return PhysicalFile(filePath, mimeType, fileName, enableRangeProcessing: true);
    }
}
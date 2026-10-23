using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.AuditTrail.Application.Queries;

namespace Pharos.Server.AuditTrail.Web.Controllers;

[ApiController]
[Route("api/audit-trail")]
[Authorize(Permissions.ViewAuditTrail)]
public class AuditTrailController(ISender sender) : ControllerBase
{
#if DEBUG
    [HttpPost]
    public async Task<IActionResult> CreateAuditTrailRecord([FromBody] CreateAuditTrailRecordCommand command)
    {
        var id = await sender.Send(command);
        return Ok(new { Message = "Audit record added", Id = id });
    }
#endif

    [HttpGet("getAll")]
    public async Task<PartialQueryResult<AuditTrailRecordDto>?> GetAuditTrailRecords([FromQuery] GetAuditTrailRecordsQuery query, CancellationToken cancellationToken)
    {
        var res = await sender.Send<PartialQueryResult<AuditTrailRecordDto>>(query, cancellationToken);
        return res;
    }

    [HttpGet("getOperators")]
    public Task<IReadOnlyList<string>> GetOperators(CancellationToken ct)
    {
        return sender.Send(new GetDistinctAuditOperatorsQuery(), ct);
    }
    
    [HttpPost("export")]
    public async Task<string> ExportAuditTrailRecords([FromBody] GetAuditTrailExportQuery query, CancellationToken cancellationToken)
    {
        return await sender.Send(query, cancellationToken);
    }
}
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Parameters.Application.Commands.MethodQualityControls;
using Pharos.Server.Parameters.Application.Queries.GetQualityControls;

namespace Pharos.Server.Parameters.Web.Controllers;

[ApiController]
[Authorize(Permissions.EditQualityControlSettings)]
[Route("api/qualitycontrols")]
public class QualityControlsController : ControllerBase
{
    private readonly ISender _sender;

    public QualityControlsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("phadia")]
    public async Task<PartialQueryResult<PhadiaQualityControlDto>> GetPhadiaQualityControls([FromQuery] GetPhadiaQualityControlsQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<PhadiaQualityControlDto>>(query, cancellationToken);
    }

    [HttpPut("phadia/enable")]
    public async Task UpdatePhadiaQualityControlEnabled(int id, bool enabled, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdatePhadiaQualityControlEnabledCommand(id, enabled), cancellationToken);
    }

    [HttpGet("phadia/{id}")]
    public async Task<PhadiaQualityControlDetailDto> GetPhadiaQualityControlDetails(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetPhadiaQualityControlDetailsQuery(id), cancellationToken);
    }

    [HttpPut("phadia/{id}")]
    public async Task UpdatePhadiaQualityControlDetails(int id, UpdatePhadiaQualityControlCommand command, CancellationToken cancellationToken)
    {
        command = command with
        {
            Id = id
        };
        await _sender.Send(command, cancellationToken);
    }

    [HttpGet("lab")]
    public async Task<PartialQueryResult<LabQualityControlDto>> GetLabQualityControls([FromQuery] GetLabQualityControlsQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<LabQualityControlDto>>(query, cancellationToken);
    }

    [HttpPut("lab/enable")]
    public async Task UpdateLabQualityControlEnabled(int id, bool enabled, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateLabQualityControlEnabledCommand(id, enabled), cancellationToken);
    }

    [HttpGet("lab/{id}")]
    public async Task<LabQualityControlDetailDto> GetLabQualityControlDetails(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetLabQualityControlDetailsQuery(id), cancellationToken);
    }

    [HttpDelete("lab")]
    public async Task DeleteLabQualityControls(HashSet<int> ids, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteLabQualityControlsCommand(ids), cancellationToken);
    }

    [HttpPost("lab")]
    public async Task<int> CreateLabQualityControl(CreateLabQualityControlCommand command, CancellationToken cancellationToken)
    {
        return await _sender.Send(command, cancellationToken);
    }

    [HttpPut("lab/{id}")]
    public async Task UpdateLabQualityControl(int id, UpdateLabQualityControlCommand command, CancellationToken cancellationToken)
    {
        command = command with
        {
            Id = id
        };
        await _sender.Send(command, cancellationToken);
    }

    [HttpGet("barcodeformats")]
    public async Task<List<QualityControlBarcodeFormatDetailDto>> GetQualityControlBarcodeFormats(CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetQualityControlBarcodeFormatsQuery(), cancellationToken);
    }

    [HttpGet("barcodeformats/{id}")]
    public async Task<QualityControlBarcodeFormatDetailDto> GetQualityControlBarcodeFormatDetails(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetQualityControlBarcodeFormatDetailsQuery(id), cancellationToken);
    }

    [HttpPost("barcodeformats")]
    public async Task CreateQualityControlBarcodeFormat(CreateQualityControlBarcodeFormatCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }

    [HttpPut("barcodeformats/{id}")]
    public async Task UpdateQualityControlBarcodeFormat(int id, UpdateQualityControlBarcodeFormatCommand command, CancellationToken cancellationToken)
    {
        command = command with
        {
            Id = id
        };
        await _sender.Send(command, cancellationToken);
    }

    [HttpDelete("barcodeformats/{id}")]
    public async Task DeleteQualityControlBarcodeFormat(int id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteQualityControlBarcodeFormatCommand(id), cancellationToken);
    }

    [HttpGet("schedules")]
    public async Task<PartialQueryResult<QualityControlScheduleDto>> GetQualityControlSchedules([FromQuery] GetQualityControlSchedulesQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<QualityControlScheduleDto>>(query, cancellationToken);
    }

    [HttpPut("schedules/{id}")]
    public async Task UpdateQualityControlSchedule(int id, UpdateQualityControlScheduleCommand command, CancellationToken cancellationToken)
    {
        command = command with
        {
            Id = id
        };
        await _sender.Send(command, cancellationToken);
    }
}
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Queries;
using Pharos.Server.Parameters.Application.Commands;
using Pharos.Server.Parameters.Application.Commands.MethodTestDetails;
using Pharos.Server.Parameters.Application.Commands.QualityControl;
using Pharos.Server.Parameters.Application.Commands.SpecializedMethodsAndTests;
using Pharos.Server.Parameters.Application.Queries;
using Pharos.Server.Parameters.Application.Queries.GetMethodCurveControls;
using Pharos.Server.Parameters.Application.Queries.GetMethodNames;
using Pharos.Server.Parameters.Application.Queries.GetMethods;
using Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;
using Pharos.Server.Parameters.Application.Queries.GetSpecializedMethodsAndTests;
using Pharos.Server.Parameters.Application.Queries.GetParameterExport;
using Pharos.Server.Parameters.Application.Queries.QualityControl;

namespace Pharos.Server.Parameters.Web.Controllers;

[ApiController]
[Route("api/methods")]
public class MethodsController : ControllerBase
{
    private readonly ISender _sender;

    public MethodsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize]
    public Task<List<MethodDto>> GetMethods(CancellationToken cancellationToken)
    {
        return _sender.Send(new GetMethodsQuery(), cancellationToken);
    }

    [HttpGet("specialized")]
    [Authorize(Permissions.EditServiceSettings)]
    public Task<PartialQueryResult<SpecializedMethodOrTestDto>> GetSpecializedMethodsAndTests([FromQuery] GetSpecializedMethodsAndTestsQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send<PartialQueryResult<SpecializedMethodOrTestDto>>(query, cancellationToken);
    }

    [HttpPut("specialized")]
    [Authorize(Permissions.EditServiceSettings)]
    public Task SaveSpecializedMethodsAndTests([FromBody] SaveSpecializedMethodsAndTestsCommand command, CancellationToken cancellationToken)
    {
        return _sender.Send(command, cancellationToken);
    }

    [HttpGet("names")]
    [Authorize]
    public Task<List<string>> GetMethodNames(CancellationToken cancellationToken)
    {
        return _sender.Send(new GetMethodNamesQuery(), cancellationToken);
    }

    [HttpGet("requesttests")]
    [Authorize]
    public Task<PartialQueryResult<MethodTestDto>> GetRequestTests([FromQuery] GetRequestTestsQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send<PartialQueryResult<MethodTestDto>>(query, cancellationToken);
    }

    [HttpGet("reflextests")]
    [Authorize]
    public Task<PartialQueryResult<MethodTestDto>> GetReflexTests([FromQuery] GetReflexTestsQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send<PartialQueryResult<MethodTestDto>>(query, cancellationToken);
    }

    [HttpGet("parameterinfo")]
    [Authorize]
    public Task<ParameterInfo> GetParameterInfo(string testName, string methodName, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetTestResultInformationQuery(testName, methodName), cancellationToken);
    }

    [HttpPut("{id}")]
    [Authorize(Permissions.EditSystemSettings)]
    public Task UpdateMethod(int id, [FromBody] UpdateMethodCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return _sender.Send(command, cancellationToken);
    }

    [HttpGet("details/{id}")]
    [Authorize]
    public Task<MethodDetailsDto> GetMethodDetails(int id, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetMethodDetailsQuery(id), cancellationToken);
    }

    [HttpPost("export")]
    [Authorize(Permissions.EditTestSettings)]
    public async Task<FileStreamResult> GetParametersExport([FromBody] GetParameterExportToClientQuery query, CancellationToken cancellationToken)
    {
        var exportedFile = await _sender.Send(query, cancellationToken);
        var mimeType = "application/zip";
        return File(exportedFile, mimeType);
    }

    [HttpGet("test/details/{testId}")]
    [Authorize]
    public Task<MethodTestDetailsDto> GetMethodTestDetails(int testId, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetMethodTestDetailsQuery(testId), cancellationToken);
    }

    [HttpGet("curveControlNames")]
    [Authorize]
    public Task<List<string>> GetMethodCurveControlNames([FromQuery] GetMethodCurveControlNamesQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send(query, cancellationToken);
    }

    [HttpPut("test/details/{testId}")]
    [Authorize(Permissions.EditSystemSettings)]
    public Task UpdateMethodTestDetails(int testId, [FromBody] UpdateMethodTestDetailsCommand command, CancellationToken cancellationToken)
    {
        command.Id = testId;
        return _sender.Send(command, cancellationToken);
    }

    [HttpGet("method/qualityControlsOverview")]
    [Authorize(Permissions.EditQualityControlSettings)]
    public Task<List<MethodQualityControlsOverviewDto>> GetMethodQualityControlsOverview([FromQuery] List<string> qualityControlMethodNameList, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetMethodQualityControlsOverviewQuery(qualityControlMethodNameList), cancellationToken);
    }

    [HttpGet("method/qualityControl/{id}")]
    [Authorize(Permissions.EditQualityControlSettings)]
    public Task<QualityControlDto> GetMethodQualityControlsById(int id, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetMethodQualityControlsByIdQuery(id), cancellationToken);
    }

    [HttpPost("method/qualityControl/createLabLot")]
    [Authorize(Permissions.EditQualityControlSettings)]
    public Task<int> CreateLabQualityControlLot(CreateLabQualityControlLotCommand command, CancellationToken cancellationToken)
    {
        return _sender.Send(command, cancellationToken);
    }

    [HttpDelete("method/qualityControl/deleteLot")]
    [Authorize(Permissions.EditQualityControlSettings)]
    public Task<bool> DeleteQualityControlLot(DeleteMethodQualityControlLotsCommand command, CancellationToken cancellationToken)
    {
        return _sender.Send(command, cancellationToken);
    }

    [HttpPost("method/qualityControl/updateLot")]
    [Authorize(Permissions.EditQualityControlSettings)]
    public Task<int> UpdateQualityControlLot(UpdateQualityControlLotCommand command, CancellationToken cancellationToken)
    {
        return _sender.Send(command, cancellationToken);
    }

    [HttpPost("method/qualityControl/methodNames")]
    [Authorize(Permissions.EditQualityControlSettings)]
    public Task<List<string>> GetQualityControlsMethodNames(CancellationToken cancellationToken)
    {
        return _sender.Send(new GetQualityControlsMethodNamesQuery(), cancellationToken);
    }

    [HttpGet("getInstrumentDilutions")]
    [Authorize]
    public async Task<List<InstrumentDilutionDto>> GetInstrumentDilutionFactors([FromQuery] GetInstrumentDilutionsQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send(query, cancellationToken);
    }

    [HttpGet("tests")]
    [Authorize]
    public Task<PartialQueryResult<MethodTestDto>> GetMethodTests([FromQuery] GetMethodTestsQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send<PartialQueryResult<MethodTestDto>>(query, cancellationToken);
    }
}

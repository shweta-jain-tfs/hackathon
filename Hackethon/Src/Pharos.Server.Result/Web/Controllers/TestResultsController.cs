using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Enums;
using Pharos.Server.Result.Application.Queries;
using Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;
using Pharos.Server.Result.Application.Queries.GetResultExport;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;
using Pharos.Server.Result.Application.Queries.GetTestResults;

namespace Pharos.Server.Result.Web.Controllers;

[ApiController]
[Route("api/results")]
public class TestResultsController : ControllerBase
{
    private readonly ISender _sender;

    public TestResultsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize]
    public Task<PartialQueryResult<AnalyticalRunTestResultDto>> GetTestResults([FromQuery] GetTestResultsQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send<PartialQueryResult<AnalyticalRunTestResultDto>>(query, cancellationToken);
    }

    [HttpGet("index")]
    [Authorize]
    public Task<int> GetTestResultIndex([FromQuery] GetTestResultIndexQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send(query, cancellationToken);
    }

    [HttpGet("count")]
    [Authorize]
    public Task<int> GetTestResultsCount(string serialNumber, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetTestResultsCountQuery(serialNumber), cancellationToken);
    }

    [HttpGet("pendingReflex")]
    [Authorize]
    public Task<bool> GetTestResultsPendingReflex([FromQuery] AnyTestResultsInPendingReflexQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send(query, cancellationToken);
    }

    [HttpPost("approve")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task<int> ApproveTestResults(ApproveTestResultsCommand command, CancellationToken cancellationToken)
    {
        return await _sender.Send(command, cancellationToken);
    }

    [HttpPost("reset")]
    [Authorize]
    public async Task<int> ResetTestResults(ResetTestResultsCommand command, CancellationToken cancellationToken)
    {
        return await _sender.Send(command, cancellationToken);
    }

    [HttpGet("analyticalrun/{id}")]
    [Authorize]
    public async Task<AnalyticalRunDto> GetAnalyticalRun(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetAnalyticalRunQuery(id), cancellationToken);
    }

    [HttpGet("cc/{id}")]
    [Authorize]
    public async Task<List<CurveControlTestResultDto>> GetCurveControlByAr(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetCurveControlQuery(id), cancellationToken);
    }

    [HttpGet("qc/{id}")]
    [Authorize]
    public async Task<List<QualityControlTestResultDto>> GetQualityControlByAr(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetQualityControlQuery(id), cancellationToken);
    }

    [HttpGet("qcdetails/{id}")]
    [Authorize]
    public async Task<QualityControlTestResultDto> GetQualityControlById(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetQualityControlTestResultQuery(id), cancellationToken);
    }

    [HttpGet("methodNameByAr/{id}")]
    [Authorize]
    public async Task<MethodAndInstrumentNameDto> GetMethodNameByRun(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetMethodAndInstrumentNameByAnalyticalRunQuery(id), cancellationToken);
    }

    [HttpGet("analyticalRuns")]
    [Authorize]
    public Task<PartialQueryResult<AnalyticalRunDto>> GetAnalyticalRuns([FromQuery] GetAnalyticalRunsQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send<PartialQueryResult<AnalyticalRunDto>>(query, cancellationToken);
    }

    [HttpPost("export")]
    [Authorize(Permissions.ExportTestResults)]
    public async Task<FileStreamResult> GetResultExport([FromBody] GetResultExportQuery query, CancellationToken cancellationToken)
    {
        var exportedFile = await _sender.Send(query, cancellationToken);
        var mimeType = "text/csv";
        return File(exportedFile.Stream, mimeType, exportedFile.FileName);
    }

    [HttpGet("curveControls")]
    [Authorize]
    public async Task<CurveControlChartDto> GetCurveControlForCharts([FromQuery] GetCurveControlsForChartsQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send(query, cancellationToken);
    }

    [HttpGet("curveControlsForQuality")]
    [Authorize]
    public async Task<CurveControlChartDto> GetCurveControlsForQuality([FromQuery] GetCurveControlsForQualityQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send(query, cancellationToken);
    }

    [HttpGet("qualityControls")]
    [Authorize]
    public async Task<QualityControlChartDto> GetQualityControlForCharts([FromQuery] GetQualityControlsForChartsQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send(query, cancellationToken);
    }

    [HttpGet("qualityControlsForQuality")]
    [Authorize]
    public async Task<QualityControlChartDto> GetQualityControlForQuality([FromQuery] GetQualityControlsForQualityQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send(query, cancellationToken);
    }

    [HttpGet("qualityControlsInformation")]
    [Authorize]
    public async Task<List<QualityControlInformationDto>> GetQualityControlInformation([FromQuery] GetQualityControlInformationQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send(query, cancellationToken);
    }

    [HttpGet("testResultStatus")]
    [Authorize]
    public async Task<TestResultSummaryStatus> GetOverallStatusForTestResults(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetOverallStatusForTestResultsQuery(id), cancellationToken);
    }

    [HttpPost("updateTestResult")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task UpdateTestResult(UpdateSampleTestResultCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }

    [HttpPost("updateCurveControlComment")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task UpdateCurveControlComment(UpdateCurveControlCommentCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }

    [HttpPost("updateQualityControlComment")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task UpdateQualityControlComment(UpdateQualityControlCommentCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }

    [HttpPost("rerunCurveControl")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task RerunCurveControl([FromBody] RerunCurveControlCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }

    [HttpGet("replicateDetails")]
    [Authorize]
    public async Task<List<TestReplicateDetailDto>> GetTestReplicateDetails([FromQuery] GetTestReplicateDetailsQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send(query, cancellationToken);
    }

    [HttpDelete("analyticalRuns")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task DeleteAnalyticalRuns([FromBody] DeleteAnalyticalRunsCommand command, CancellationToken cancellation)
    {
        await _sender.Send(command, cancellation);
    }

    [HttpGet("sampletestresult")]
    [Authorize]
    public async Task<SampleTestResultDto> GetSampleTestResult([FromQuery] GetSampleTestResultQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send(query, cancellationToken);
    }
}

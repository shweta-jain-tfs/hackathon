using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

namespace Pharos.Server.Result.Web.Controllers;

[ApiController]
[Route("api/curves")]
public class CurvesController : ControllerBase
{
    private readonly ISender _sender;

    public CurvesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<CurvePresentationDto> GetCurve(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetCalibrationCurvePointsQuery(id), cancellationToken);
    }

    [HttpGet("ar/{id}")]
    [Authorize]
    public async Task<CurvePresentationDto?> GetCurveByAR(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetCalibrationCurveFromAnalyticalRuns(id), cancellationToken);
    }

    [HttpPut("update")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task UpdateCurve(UpdateTestResultCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }

    [HttpGet("active/{serialNumber}/{methodName}")]
    [Authorize]
    public async Task<List<CurvePresentationDto>> GetCurrentCurves(string serialNumber, string methodName, CancellationToken cancellationToken)
    {
        var curveInfo = await _sender.Send(new GetCurrentCalibrationCurvesQuery(serialNumber, methodName), cancellationToken);
        var list = new List<CurvePresentationDto>();

        foreach (var curve in curveInfo)
        {
            list.Add(await _sender.Send(new GetCalibrationCurvePointsQuery(curve.CurveId), cancellationToken));
        }

        return list;
    }

    [HttpGet("{serialNumber}/{methodName}")]
    [Authorize]
    public async Task<List<CurvePresentationDto>> GetCurves(string serialNumber, string methodName, CancellationToken cancellationToken)
    {
        var curveIds = await _sender.Send(new GetCalibrationCurvesQuery(serialNumber, methodName), cancellationToken);
        var list = new List<CurvePresentationDto>();

        foreach (var curveId in curveIds)
        {
            list.Add(await _sender.Send(new GetCalibrationCurvePointsQuery(curveId), cancellationToken));
        }

        return list;
    }

    [HttpGet("activeConjugateLots/{serialNumber}/{methodName}")]
    [Authorize]
    public async Task<List<string>> GetActiveConjugateLots(string serialNumber, string methodName, CancellationToken cancellationToken)
    {
        var curveInfo = await _sender.Send(new GetCurrentCalibrationCurvesQuery(serialNumber, methodName), cancellationToken);
        var list = curveInfo.Select(x => x.ConjugateLotNumber).ToList();

        return list;
    }

    [HttpPut("re-run")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task ReRunCurve(RerunCalibratorCurveCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }

    [HttpGet("availableCurves")]
    [Authorize]
    public async Task<List<AvailableCurveDto>> GetAvailableCurves(int analyticalRunId, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetAvailableCalibrationCurvesQuery(analyticalRunId), cancellationToken);
    }

    [HttpPost("updateCurveId")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task ChangeCurveForAnalyticalRun(ChangeCurveForAnalyticalRunCommand forAnalyticalRunCommand, CancellationToken cancellationToken)
    {
        await _sender.Send(forAnalyticalRunCommand, cancellationToken);
    }

    [HttpPost("AcceptCurve")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task AcceptCurveFromFlaggedCurveControl(AcceptCurveFromFlaggedCurveControlCommand fromFlaggedCurveControlCommand, CancellationToken cancellationToken)
    {
        await _sender.Send(fromFlaggedCurveControlCommand, cancellationToken);
    }

}
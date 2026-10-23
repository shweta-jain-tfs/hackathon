using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Mapping;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

internal record GetCalibrationCurvePointsQuery(int CurveId) : IRequest<CurvePresentationDto>;

internal sealed class GetCalibrationCurvePointsQueryHandler(
    IResultDbContext context,
    IMethodsService methodsService,
    IResultCalculationService resultCalculationService) : IRequestHandler<GetCalibrationCurvePointsQuery, CurvePresentationDto>
{
    public async Task<CurvePresentationDto> Handle(GetCalibrationCurvePointsQuery request, CancellationToken cancellationToken)
    {
        var curve = await context.Curves
            .Where(x => x.Id == request.CurveId)
            .Include(x => x.CurrentCurves)
            .Include(x => x.TestResults!)
            .ThenInclude(tr => tr.Replicates!)
            .ThenInclude(r => r.TestReplicateErrors)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (curve == null)
        {
            throw new NotFoundException("curve id: " + request.CurveId);
        }

        var methodPresentation = await methodsService.GetMethod(curve.MethodName, m => m.Presentation);
        var calibratorTestResults = curve.TestResults!;
        var isAccepted = curve.Judgment == Domain.CurveJudgment.Flagged &&
                         await context.AnalyticalRuns.AnyAsync(ar => ar.CurveId == curve.Id && ar.CurveState == CurveState.Frozen, cancellationToken);

        IList<CalculatedPointDto>? calculatedCurvePoints = null;

        if (curve.CurveFittedFlag)
        {
            //Extract parameters for calculation
            var maxConcentration = calibratorTestResults.MaxBy(x => x.ExpectedConcentration)!.ExpectedConcentration;
            var minConcentration = calibratorTestResults.MinBy(x => x.ExpectedConcentration)!.ExpectedConcentration;
            var isLogScaleX = methodPresentation is { XAxisScale: AxisScale.Log };

            calculatedCurvePoints = resultCalculationService.CalculateCurvePoints(curve, maxConcentration, minConcentration, isLogScaleX);
        }

        var curvePresentationDto = new CurvePresentationDto
        {
            Id = curve.Id,
            Judgment = curve.Judgment,
            ApprovalStatus = curve.ApprovalStatus,
            MeasurementDateTime = curve.MeasurementDateTime,
            LastDayToUse = curve.LastDayToUse,
            CurveFittedFlag = curve.CurveFittedFlag,
            ConjugateLot = curve.ConjugateLotNumber,
            CarrierLot = curve.ImmunoCapLotNumber,
            CalculatedPoints = calculatedCurvePoints,
            MethodPresentation = ResultMapper.ToMethodPresentationDto(methodPresentation),
            CalibratorTestResults = calibratorTestResults.Select(ResultMapper.ToCalibratorTestResultDto).ToList(),
            ActiveCurve = curve.CurrentCurves!.Count > 0,
            SelectedCurve = true,
            CurveControlDisabledFlag = curve.CurveControlDisabledFlag ?? false,
            IsAccepted = isAccepted
        };

        foreach (var item in curvePresentationDto.CalibratorTestResults)
        {
            //get concentration unit
            item.Unit = await methodsService.GetMethodTestConcentrationUnit(item.MethodName, item.TestName) ?? string.Empty;
        }

        return curvePresentationDto;
    }
}
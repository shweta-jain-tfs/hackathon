using Pharos.Server.Application.Reagents;
using Pharos.Server.Result.Application.Queries.GetCalibrationCurve;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Interfaces;

public interface IResultCalculationService
{
    public Task<Curve> CheckAndFitCurve(string instrumentNumber, Guid groupId, List<CalibratorTestResult> calibratorTestOrders, CancellationToken cancellationToken);
    public Task CalculateReplicateConcentration(TestReplicate replicate, int curveId, int preDilutionFactor, int instrumentDilutionFactor, string methodName, string testName, TestResultType testOrderType, CancellationToken cancellationToken);
    public Task CalculateSummaryResult(TestResult result);
    public IList<CalculatedPointDto> CalculateCurvePoints(Curve curveId, double maxConcentration, double minConcentration, bool isLogScaleX);
    public double AdjustResponseAccordingToCarrierLotAndConcentration(double response, EliaCalibratorWellLotInformation eliaCalibratorLot, double concentration);
}

using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Interfaces;

public interface IResponseCalculationService
{
    Task<Blank?> GetValidReagentBlank(string instrumentNumber, string devSolutionBarcode, int devSolutionCellPosition, BlankType blankType);
    Task CalculateResponseForTestReplicate(TestReplicate replicate, Blank blank, TestResultType testOrderType, string methodName, string calibratorCurveControlName);
    void CalculateMeanResponse(TestResult result);
}

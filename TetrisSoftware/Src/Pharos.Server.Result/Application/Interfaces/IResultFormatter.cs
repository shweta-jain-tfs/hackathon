using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Interfaces;

public interface IResultFormatter
{
    public Task<string> ConcentrationAsText(
        double concentration,
        ConcentrationStatus concentrationStatus,
        string methodName, string testName,
        int totalDilutionFactor,
        string? carrierBarcode,
        TestResultType resultType);

    public string ResponseAsText(double response);
    public string NoDataAvailable();
    public string LowRU();
    public string QuotientAsText(double quotient, bool asPercent);
}

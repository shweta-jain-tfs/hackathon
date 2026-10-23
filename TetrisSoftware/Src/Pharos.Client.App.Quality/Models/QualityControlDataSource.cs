namespace Pharos.Client.App.Quality.Models;

public interface IQualityControlDataSource
{
    public Task<QualityControlChart> GetQualityControls(string instrumentNumber, string methodName, string qualityControlName, string testName, int lastDays);
    public Task<ICollection<QualityControlInformation>> GetQualityControlInformation(string instrumentNumber, string methodName, int lastDays);
}

internal sealed class QualityControlDataSource(HttpClient httpClient) : IQualityControlDataSource
{
    public async Task<QualityControlChart> GetQualityControls(string instrumentNumber, string methodName, string qualityControlName, string testName, int lastDays)
    {
        return await new TestResultsClient(httpClient).GetQualityControlForQualityAsync(instrumentNumber, methodName, qualityControlName, testName, lastDays);
    }

    public async Task<ICollection<QualityControlInformation>> GetQualityControlInformation(string instrumentNumber, string methodName, int lastDays)
    {
        return await new TestResultsClient(httpClient).GetQualityControlInformationAsync(instrumentNumber, methodName, lastDays);
    }
}
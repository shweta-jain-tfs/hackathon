using Pharos.Client.Common.Models;

namespace Pharos.Client.App.InstrumentManagement.Models.Instruments;

internal sealed class TestResultDataService : ITestResultDataService
{
    private readonly HttpClient _httpClient;
    public int? AnalyticalRunId { get; set; }
    public string? InstrumentNumber { get; set; }

    public TestResultDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<int> GetTestResultsCount(string serialNumber)
    {
        var client = new TestResultsClient(_httpClient);
        return await client.GetTestResultsCountAsync(serialNumber);
    }

}
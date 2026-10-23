using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.Common.App.Services;

public class InstrumentDilutionService : IInstrumentDilutionService
{
    private readonly HttpClient _httpClient;

    public InstrumentDilutionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<InstrumentDilution>> GetDilutionFactors(string methodName, string testName, int predilutionFactor)
    {
        var client = new MethodsClient(_httpClient);
        var result = await client.GetInstrumentDilutionFactorsAsync(predilutionFactor, methodName, testName);
        return result.ToList();
    }
}
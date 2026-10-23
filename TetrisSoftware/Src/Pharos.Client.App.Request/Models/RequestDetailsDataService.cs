namespace Pharos.Client.App.Request.Models;

public interface IRequestDetailsDataService : IDetailsPageDataService<RequestDetails>
{
    Task QueryLisAsync(string[] sampleIds);
    Task ResetTestOrdersAsync(int[] requestIds, bool includeAbortedTests);
    Task<int> GetResettableTestsCountAsync(int[] requestIds, bool includeAbortedTests);
    Task QueryRefreshExistingRequestAsync(string sampleId);
    Task ExportToLisAsync(int[] requestIds);
}

public interface ISampleTubeDataSource
{
    Task<SampleTubeImage> GetSampleTubeImage(string instrumentNumber, string imageId);
    Task UnloadSampleTubes(int requestId);
    Task<List<SampleTubeHistory>> GetSampleTubes(int requestId);
}

public interface IVolumeCalculationService
{
    Task<int?> GetNeededSampleVolume(ICollection<RequestTest> testOrders);
}

internal class RequestDetailsDataService : IRequestDetailsDataService, ISampleTubeDataSource, IVolumeCalculationService
{
    private readonly HttpClient _httpClient;

    public RequestDetailsDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RequestDetails> GetByIdAsync(int entityId)
    {
        var client = new RequestsClient(_httpClient);
        return await client.GetRequestDetailsAsync(entityId);
    }

    public async Task<int> CreateAsync(RequestDetails value)
    {
        var client = new RequestsClient(_httpClient);
        var command = value.ToCreateRequestCommand();
        return await client.CreateRequestAsync(command);
    }

    public async Task UpdateAsync(RequestDetails value)
    {
        var client = new RequestsClient(_httpClient);
        var command = value.ToUpdateRequestCommand();
        await client.UpdateRequestAsync(value.Id, command);
    }

    public async Task QueryLisAsync(string[] sampleIds)
    {
        var client = new LisClient(_httpClient);
        await client.QueryTestOrdersForSamplesAsync(sampleIds);
    }

    public async Task ExportToLisAsync(int[] requestIds)
    {
        var client = new RequestsClient(_httpClient);
        await client.ExportRequestsToLisAsync(requestIds);
    }

    public async Task QueryRefreshExistingRequestAsync(string sampleId)
    {
        var client = new LisClient(_httpClient);
        await client.QueryRefreshExistingRequestAsync(sampleId);
    }

    public async Task<SampleTubeImage> GetSampleTubeImage(string instrumentNumber, string imageId)
    {
        var client = new RequestsClient(_httpClient);
        return await client.GetSampleTubeImageAsync(instrumentNumber, imageId);
    }

    public async Task UnloadSampleTubes(int requestId)
    {
        var client = new RequestsClient(_httpClient);
        await client.UnloadSampleTubesAsync(requestId);
    }

    public async Task<List<SampleTubeHistory>> GetSampleTubes(int requestId)
    {
        var client = new RequestsClient(_httpClient);
        var items = await client.GetSampleTubeHistoryByIdAsync(requestId);
        return items.ToList();
    }

    public async Task ResetTestOrdersAsync(int[] requestIds, bool includeAbortedTests)
    {
        var client = new RequestsClient(_httpClient);
        await client.ResetTestOrdersAsync(new ResetTestOrdersCommand
        {
            RequestIds = requestIds,
            IncludeAbortedTests = includeAbortedTests
        });
    }

    public async Task<int> GetResettableTestsCountAsync(int[] requestIds, bool includeAbortedTests)
    {
        var client = new RequestsClient(_httpClient);
        return await client.GetResettableTestsCountAsync(new GetResettableTestsCountQuery
        {
            RequestIds = requestIds,
            IncludeAbortedTests = includeAbortedTests
        });
    }

    public async Task<int?> GetNeededSampleVolume(ICollection<RequestTest> testOrders)
    {
        var items = testOrders.Where(x => x is { IsActive: true, State: TestOrderState.NotStarted or TestOrderState.UnableToStart }).ToVolumeCalculationTests();

        if (items.Count == 0)
        {
            return null;
        }

        var client = new RequestsClient(_httpClient);
        var volumeNeeded = await client.GetNeededSampleVolumeAsync(items);
        return volumeNeeded.AccumulatedVolumeNeeded;
    }
}
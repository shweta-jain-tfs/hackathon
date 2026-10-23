namespace Pharos.Client.App.Sample.Models;

public interface IRequestDataSource
{
    Task<RequestIdResponse> GetRequestIdAsync(string sampleId);
}

internal class RequestDataSource : IRequestDataSource
{
    private readonly RequestsClient _client;

    public RequestDataSource(HttpClient httpClient)
    {
        _client = new RequestsClient(httpClient);
    }

    public async Task<RequestIdResponse> GetRequestIdAsync(string sampleId)
    {
        var requestDetails = await _client.GetRequestIdAsync(sampleId);
        return requestDetails;
    }
}
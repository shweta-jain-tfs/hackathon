namespace Pharos.Client.App.Parameters.Reflex.Models;

public interface IRequestorDataSource
{
    Task<RequestorDetails> GetRequestorDetails(int id);
    Task<Tuple<IEnumerable<Requestor>, int>> GetRequestors(string searchCriteria);
    Task<Requestor?> GetRequestorByRequestorId(string requestorId);
}

public class RequestorDataSource : IRequestorDataSource
{
    private readonly HttpClient _httpClient;

    public RequestorDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Tuple<IEnumerable<Requestor>, int>> GetRequestors(string searchCriteria)
    {
        if (string.IsNullOrWhiteSpace(searchCriteria))
        {
            return new Tuple<IEnumerable<Requestor>, int>(Array.Empty<Requestor>(), 0);
        }

        var client = new RequestorsClient(_httpClient);
        var result = await client.GetRequestorAsync(searchCriteria, searchCriteria, 0, 40, null, null);
        return new Tuple<IEnumerable<Requestor>, int>(result.Items, result.TotalCount);
    }

    public async Task<Requestor?> GetRequestorByRequestorId(string requestorId)
    {
        var client = new RequestorsClient(_httpClient);
        return await client.GetRequestorByRequestIdAsync(requestorId);
    }

    public async Task<RequestorDetails> GetRequestorDetails(int id)
    {
        var client = new RequestorsClient(_httpClient);
        return await client.GetRequestorDetailsAsync(id);
    }
}
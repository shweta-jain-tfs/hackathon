namespace Pharos.Client.App.Parameters.Requestors.Models;

public interface IRequestorDetailsDataService : IDetailsPageDataService<RequestorDetails>
{
    Task ResetCountersAsync(int requestorId);
}

internal sealed class RequestorDetailsDataService : IRequestorDetailsDataService
{
    private readonly HttpClient _httpClient;

    public RequestorDetailsDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RequestorDetails> GetByIdAsync(int id)
    {
        var client = new RequestorsClient(_httpClient);
        var dto = await client.GetRequestorDetailsAsync(id);

        if (dto == null)
        {
            throw new InvalidOperationException($"Requestor with ID {id} not found.");
        }

        return dto;
    }

    public async Task<int> CreateAsync(RequestorDetails model)
    {
        var client = new RequestorsClient(_httpClient);
        var command = model.ToCreateRequestorCommand();
        return await client.CreateRequestorAsync(command);
    }

    public async Task UpdateAsync(RequestorDetails model)
    {
        var client = new RequestorsClient(_httpClient);
        var command = model.ToUpdateRequestorCommand();
        await client.UpdateRequestorAsync(model.Id, command);
    }

    public async Task ResetCountersAsync(int requestorId)
    {
        var client = new RequestorsClient(_httpClient);
        await client.ResetCountersAsync(requestorId);
    }
}
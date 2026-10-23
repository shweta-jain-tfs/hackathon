namespace Pharos.Client.App.Logs.Models.Errors;

public interface ISystemErrorDetailsDataService : IEntityDataService<SystemErrorDetails>
{
    Task<DateTimeOffset> RespondToError(int id, ResponseAction responseAction);
    Task AcknowledgeSystemErrors(ICollection<int> ids);
}

internal sealed class SystemErrorDetailsDataService : ISystemErrorDetailsDataService
{
    private readonly SystemErrorsClient _client;

    public SystemErrorDetailsDataService(HttpClient httpClient)
    {
        _client = new SystemErrorsClient(httpClient);
    }

    public Task<DataSourceResult<SystemErrorDetails>> GetAsync(DataSourceRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<SystemErrorDetails> GetByIdAsync(int entityId)
    {
        return await _client.GetSystemErrorDetailsAsync(entityId);
    }

    public Task<int> CreateAsync(SystemErrorDetails value)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(SystemErrorDetails value)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public Task<DeleteResponse> DeleteAsync(int[] entityIds)
    {
        throw new NotImplementedException();
    }

    public Task<DateTimeOffset> RespondToError(int id, ResponseAction responseAction)
    {
        return _client.RespondToErrorAsync(new RespondToErrorCommand
        {
            SystemErrorId = id,
            ResponseAction = responseAction
        });
    }

    public async Task AcknowledgeSystemErrors(ICollection<int> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var command = new AcknowledgeSystemErrorsCommand
        {
            SystemErrorIds = ids
        };

        await _client.AcknowledgeSystemErrorsAsync(command);
    }
}

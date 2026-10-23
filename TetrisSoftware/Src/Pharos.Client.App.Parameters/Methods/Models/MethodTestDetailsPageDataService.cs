namespace Pharos.Client.App.Parameters.Methods.Models;

public interface IMethodTestDetailsPageDataService : IDetailsPageDataService<MethodTestDetails>;

public class MethodTestDetailsPageDataService(HttpClient httpClient) : IMethodTestDetailsPageDataService
{
    public async Task<MethodTestDetails> GetByIdAsync(int id)
    {
        var client = new MethodsClient(httpClient);
        return await client.GetMethodTestDetailsAsync(id);
    }

    public Task<int> CreateAsync(MethodTestDetails model)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(MethodTestDetails model)
    {
        var client = new MethodsClient(httpClient);
        var command = model.ToUpdateMethodTestDetailsCommand();
        await client.UpdateMethodTestDetailsAsync(model.Id, command);
    }
}
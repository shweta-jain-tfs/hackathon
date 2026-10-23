namespace Pharos.Client.App.Parameters.Reflex.Models;

public interface IReflexDetailDataService : IDetailsPageDataService<ReflexDefinition>
{
    Task<bool> ValidateDefinitionNameAsync(string name);
}

internal class ReflexDetailDataService : IReflexDetailDataService
{
    private readonly HttpClient _httpClient;

    public ReflexDetailDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ReflexDefinition> GetByIdAsync(int id)
    {
        var client = new ReflexClient(_httpClient);
        return await client.GetReflexDefinitionAsync(id);
    }

    public async Task<int> CreateAsync(ReflexDefinition model)
    {
        var client = new ReflexClient(_httpClient);
        var command = model.ToCreateReflexDefinitionCommand();
        return await client.CreateReflexDefinitionAsync(command);
    }

    public async Task UpdateAsync(ReflexDefinition model)
    {
        var client = new ReflexClient(_httpClient);
        var command = model.ToUpdateReflexDefinitionCommand();
        await client.UpdateReflexDefinitionAsync(model.Id, command);
    }

    public async Task<bool> ValidateDefinitionNameAsync(string name)
    {
        var client = new ReflexClient(_httpClient);
        return await client.ValidateDefinitionNameAsync(name);
    }
}
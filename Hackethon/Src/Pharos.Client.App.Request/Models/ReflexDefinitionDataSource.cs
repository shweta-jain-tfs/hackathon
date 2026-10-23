namespace Pharos.Client.App.Request.Models;

public interface IReflexDefinitionDataSource
{
    Task<List<ReflexDefinition>> GetReflexDefinitionsForTestAsync(string testName, string methodName);
}

internal class ReflexDefinitionDataSource : IReflexDefinitionDataSource
{
    private readonly HttpClient _httpClient;

    public ReflexDefinitionDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ReflexDefinition>> GetReflexDefinitionsForTestAsync(string testName, string methodName)
    {
        var client = new ReflexClient(_httpClient);
        var result = await client.GetReflexDefinitionsForTestAsync(testName, methodName);
        return result as List<ReflexDefinition> ?? result.ToList();
    }
}
namespace Pharos.Client.App.Parameters.QualityControl.Models;

public class TubeDefinitionDataSource : IEntityDataSource<TubeDefinition>
{
    private readonly HttpClient _httpClient;

    public TubeDefinitionDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<TubeDefinition>> GetAsync(DataSourceRequest request)
    {
        var client = new TubeDefinitionsClient(_httpClient);
        var result = await client.GetAllAsync();

        return new DataSourceResult<TubeDefinition>(result.ToList(), result.Count);
    }

    public Task<TubeDefinition> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }
}
namespace Pharos.Client.App.Result.Models;
internal sealed class MethodNameDataSource : IMethodNameDataSource
{
    private readonly HttpClient _httpClient;

    public MethodNameDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<string>> GetMethodNamesAsync()
    {
        var client = new MethodsClient(_httpClient);
        var result = await client.GetMethodNamesAsync();
        return result.ToList();
    }
}

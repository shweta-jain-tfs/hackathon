namespace Pharos.Client.App.ServiceTools.Services;

public interface IRequestGeneratorDataService
{
    Task<List<RequestPreview>> PreviewAsync(GenerateRequests command, CancellationToken cancellationToken = default);

    Task GenerateAsync(GenerateRequests command, CancellationToken cancellationToken = default);

    Task<bool> GetStatusAsync(CancellationToken cancellationToken = default);
}

internal sealed class RequestGeneratorDataService(
    HttpClient httpClient) : IRequestGeneratorDataService
{
    public async Task<List<RequestPreview>> PreviewAsync(
        GenerateRequests command,
        CancellationToken cancellationToken = default)
    {
        var client = new RequestGeneratorClient(httpClient);
        var result = await client.PreviewAsync(command, cancellationToken);
        return result.ToList();
    }

    public async Task GenerateAsync(
        GenerateRequests command,
        CancellationToken cancellationToken = default)
    {
        var client = new RequestGeneratorClient(httpClient);
        await client.GenerateAsync(command, cancellationToken);
    }

    public async Task<bool> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var client = new RequestGeneratorClient(httpClient);
        return await client.IsRequestGenerationInProgressAsync(cancellationToken);
    }
}

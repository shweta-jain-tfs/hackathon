namespace Pharos.Client.App.Quality.Models;

internal sealed class QualityDataSource(HttpClient httpClient) : IQualityDataSource
{
    public async Task<QcCartridgeInformation?> GetQcCartridgeInfo(string dataMatrix)
    {
        var client = new QualityClient(httpClient);
        return await client.GetQcCartridgeInfoAsync(dataMatrix);
    }
}

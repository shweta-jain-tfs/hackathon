using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.Models;

public sealed class UiSettingsDataSource(HttpClient httpClient) : ISingletonDataSource<UiSettings>
{

    public async Task<UiSettings> GetAsync()
    {
        var uiSettingsClient = new UiSettingsClient(httpClient);
        return await uiSettingsClient.GetUiSettingsAsync();
    }
}
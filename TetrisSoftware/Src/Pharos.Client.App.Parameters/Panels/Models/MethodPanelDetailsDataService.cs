namespace Pharos.Client.App.Parameters.Panels.Models;

internal class MethodPanelDetailsDataService(HttpClient httpClient) : IDetailsPageDataService<MethodPanelDetails>
{
    public async Task<int> CreateAsync(MethodPanelDetails model)
    {
        var client = new PanelsClient(httpClient);
        var command = model.ToCreateMethodPanelCommand();
        return await client.CreateMethodPanelAsync(command);
    }

    public async Task<MethodPanelDetails> GetByIdAsync(int entityId)
    {
        var client = new PanelsClient(httpClient);
        return await client.GetMethodPanelDetailsAsync(entityId);
    }

    public async Task UpdateAsync(MethodPanelDetails model)
    {
        var client = new PanelsClient(httpClient);
        var command = model.ToUpdateMethodPanelCommand();
        await client.UpdateMethodPanelAsync(model.Id, command);
    }
}
